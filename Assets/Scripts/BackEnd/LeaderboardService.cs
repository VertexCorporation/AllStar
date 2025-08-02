/***************************************************************************
 *  LeaderboardService.cs (2025-07-05 - FLAWLESS EDITOR-PROOF VERSION)
 *  -----------------------------------------------------------------------
 *  • FLAWLESS EDITOR UX: Now provides MOCK DATA when an App Check failure
 *    is detected in the Unity Editor. This prevents "failed to fetch"
 *    errors and allows for seamless UI testing and development without a
 *    live connection. This logic is safely wrapped in #if UNITY_EDITOR.
 *  • PUBLIC READS: Allows unauthenticated users to view leaderboards.
 *  • SECURE SUBMISSION: Score submission remains protected and requires
 *    user authentication.
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Firebase.Functions;
using System.Linq;

namespace Vertex.Backend
{
    public static class LeaderboardService
    {
        private static readonly Dictionary<string, (List<ScoreEntry> data, DateTime fetchTime)> clientCache = new Dictionary<string, (List<ScoreEntry> data, DateTime fetchTime)>();
        private static readonly TimeSpan cacheDuration = TimeSpan.FromMinutes(5);
        private static FirebaseFunctions _functions;
        private static FirebaseFunctions functions
        {
            get
            {
                if (_functions == null)
                {
                    _functions = FirebaseFunctions.GetInstance(Firebase.FirebaseApp.DefaultInstance, "europe-west1");
                }
                return _functions;
            }
        }
        
        #region DTO Classes
        [Serializable] private class JsonScoreEntry { public string userId; public int score; public string username; public int rank; }
        [Serializable] private class JsonScoreEntryListWrapper { public List<JsonScoreEntry> entries; }
        #endregion

        private static async Task<List<ScoreEntry>> FetchLeaderboardDataViaSignedUrlAsync(string leaderboardType)
        {
            string cacheKey = leaderboardType;
            if (clientCache.TryGetValue(cacheKey, out var cachedItem) && DateTime.UtcNow - cachedItem.fetchTime < cacheDuration)
            {
                Debug.Log($"[LeaderboardService] SUCCESS: Loading '{cacheKey}' from client-side cache.");
                return cachedItem.data;
            }

            Debug.Log($"[LeaderboardService] INFO: Requesting signed URL for '{leaderboardType}' leaderboard from Cloud Functions...");
            var function = functions.GetHttpsCallable("getLeaderboardUrl");
            var data = new Dictionary<string, object> { { "leaderboardType", leaderboardType } };

            HttpsCallableResult result;
            try
            {
                result = await function.CallAsync(data);
            }
            catch (FunctionsException e)
            {
                if (Application.isEditor && e.ErrorCode == FunctionsErrorCode.Unauthenticated)
                {
                    Debug.LogWarning($"[LeaderboardService] INFO (Editor Only): App Check blocked the call as expected. Returning mock data for UI testing. Error: {e.Message}");
                    
                    // *** FLAWLESS EDITOR FIX ***
                    // Instead of returning null and causing an error state in the UI,
                    // return a generated list of mock data. This code will NOT be
                    // included in a real build.
                    #if UNITY_EDITOR
                    return GenerateMockLeaderboardData(leaderboardType);
                    #endif
                }
                else
                {
                    Debug.LogError($"[LeaderboardService] FAILED to get signed URL for '{leaderboardType}'. Firebase Functions Error: [{e.ErrorCode}] {e.Message}");
                }
                return null; // Return null for any other real error
            }
            catch (Exception e)
            {
                Debug.LogError($"[LeaderboardService] FAILED with a generic exception for '{leaderboardType}' while getting signed URL. Error: {e.Message}");
                return null;
            }

            if (result.Data is not IDictionary<string, object> resultData || !resultData.TryGetValue("signedUrl", out object signedUrlObj))
            {
                Debug.LogError($"[LeaderboardService] FATAL: Cloud Function 'getLeaderboardUrl' did not return a 'signedUrl'. Data: {result.Data}");
                return null;
            }

            string signedUrl = signedUrlObj.ToString();
            Debug.Log($"[LeaderboardService] INFO: Received signed URL for '{leaderboardType}'. Downloading data...");
            
            using (var www = UnityWebRequest.Get(signedUrl))
            {
                await www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.Success)
                {
                    var finalEntries = ParseJsonToScoreEntries(www.downloadHandler.text);
                    if (finalEntries != null)
                    {
                        Debug.Log($"[LeaderboardService] SUCCESS: Parsed and mapped {finalEntries.Count} entries for '{leaderboardType}'.");
                        clientCache[cacheKey] = (finalEntries, DateTime.UtcNow);
                        return finalEntries;
                    }
                    else
                    {
                        Debug.LogError($"[LeaderboardService] FATAL: Failed to parse JSON for '{leaderboardType}' leaderboard.");
                        return null;
                    }
                }
                else
                {
                    if (www.responseCode == 404)
                    {
                        Debug.LogWarning($"[LeaderboardService] WARNING: Leaderboard cache not found (404) for '{leaderboardType}'. This can happen if the leaderboard is empty or the cache is being generated for the first time.");
                    }
                    else
                    {
                        Debug.LogError($"[LeaderboardService] FATAL: Could not download leaderboard from signed URL for '{leaderboardType}'. Code: {www.responseCode}, Error: {www.error}.");
                    }
                    return null;
                }
            }
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// Generates a list of fake ScoreEntry objects for testing in the Unity Editor.
        /// This method is compiled out of release builds.
        /// </summary>
        private static List<ScoreEntry> GenerateMockLeaderboardData(string leaderboardType)
        {
            var mockEntries = new List<ScoreEntry>();
            int baseScore = leaderboardType == "seasonal" ? 150000 : 80000;
            for (int i = 0; i < 25; i++)
            {
                mockEntries.Add(new ScoreEntry
                {
                    rank = i + 1,
                    username = $"EditorUser{i + 1}",
                    score = baseScore - (i * 2500) - UnityEngine.Random.Range(0, 1000),
                    userId = $"editor-user-id-{i + 1}"
                });
            }
            return mockEntries;
        }
        #endif

        private static List<ScoreEntry> ParseJsonToScoreEntries(string json)
        {
            // ... (This function remains unchanged)
            if (string.IsNullOrWhiteSpace(json) || json.Trim() == "[]") {
                return new List<ScoreEntry>();
            }
            try {
                var wrapper = JsonUtility.FromJson<JsonScoreEntryListWrapper>("{\"entries\":" + json + "}");
                if (wrapper?.entries != null) {
                    return wrapper.entries.Select(jsonEntry => new ScoreEntry {
                        userId = jsonEntry.userId,
                        score = jsonEntry.score,
                        username = jsonEntry.username,
                        rank = jsonEntry.rank
                    }).ToList();
                }
                Debug.LogError($"[LeaderboardService] FATAL: Failed to parse JSON. Wrapper or entries list is null.");
                return null;
            } catch (Exception e) {
                Debug.LogError($"[LeaderboardService] FATAL: Failed to parse JSON. Error: {e.Message}");
                return null;
            }
        }

        public static Task<List<ScoreEntry>> GetHighscoresAsync() => FetchLeaderboardDataViaSignedUrlAsync("global");
        public static Task<List<ScoreEntry>> GetSeasonScoresAsync() => FetchLeaderboardDataViaSignedUrlAsync("seasonal");
        public static async Task SubmitScoreAsync(int score, string type)
        {
            // ... (This function remains unchanged)
            if (!AuthService.IsSignedIn) {
                Debug.LogWarning($"[LeaderboardService] User is not signed in. Aborting score submission for type '{type}'.");
                return;
            }
            var data = new Dictionary<string, object> { { "score", score }, { "type", type } };
            Debug.Log($"[LeaderboardService] Preparing to submit score. Type: '{type}', Score: {score}, User: {AuthService.UserId}");
            try {
                var function = functions.GetHttpsCallable("submitScore");
                var result = await function.CallAsync(data);
                if (result.Data is IDictionary<string, object> resultData) {
                    var status = resultData.ContainsKey("status") ? resultData["status"].ToString() : "unknown";
                    var message = resultData.ContainsKey("message") ? resultData["message"].ToString() : "no message";
                    Debug.Log($"[LeaderboardService] SUBMISSION RESULT: Type: '{type}', Status: '{status}', Message: '{message}'");
                }
            } catch (FunctionsException e) {
                Debug.LogError($"[LeaderboardService] FAILED to submit score. Error: [{e.ErrorCode}] {e.Message}");
            } catch (Exception e) {
                Debug.LogError($"[LeaderboardService] FAILED with a generic exception while submitting score. Error: {e.Message}");
            }
        }
    }
}