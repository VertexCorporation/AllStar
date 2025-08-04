/***************************************************************************
 *  LeaderboardService.cs (2025-07-09 - EDITOR REAL DATA - FINAL)
 *  -----------------------------------------------------------------------
 *  • REAL EDITOR DATA: Removed all mock data generation logic. The system
 *    now relies on a correctly configured App Check debug token via
 *    AppInitializer to fetch REAL data in the Unity Editor.
 *  • ENHANCED DEBUGGING: If an App Check error occurs in the Editor,
 *    it now logs a clear, actionable error message guiding the developer
 * to check their debug token configuration.
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
    /// <summary>
    /// A static service class that handles all interactions with the leaderboard backend.
    /// It uses client-side caching and signed URLs for efficient and scalable data retrieval.
    /// </summary>
    public static class LeaderboardService
    {
        // A simple in-memory cache to avoid redundant network calls within a short time frame.
        private static readonly Dictionary<string, (List<ScoreEntry> data, DateTime fetchTime)> clientCache = new Dictionary<string, (List<ScoreEntry> data, DateTime fetchTime)>();
        private static readonly TimeSpan cacheDuration = TimeSpan.FromMinutes(5);

        private static FirebaseFunctions _functions;
        private static FirebaseFunctions functions
        {
            get
            {
                // Lazily initialize the FirebaseFunctions instance to ensure FirebaseApp is ready.
                if (_functions == null)
                {
                    _functions = FirebaseFunctions.GetInstance(Firebase.FirebaseApp.DefaultInstance, "europe-west1");
                }
                return _functions;
            }
        }

        #region DTO Classes (Data Transfer Objects)
        [Serializable] private class JsonScoreEntry { public string userId; public int score; public string username; public int rank; }
        [Serializable] private class JsonScoreEntryListWrapper { public List<JsonScoreEntry> entries; }
        #endregion

        /// <summary>
        /// The core method for fetching any leaderboard. It requests a signed URL from the backend
        /// and then downloads the JSON data from Google Cloud Storage.
        /// </summary>
        /// <param name="leaderboardType">The type of leaderboard to fetch ("global" or "seasonal").</param>
        private static async Task<List<ScoreEntry>> FetchLeaderboardDataViaSignedUrlAsync(string leaderboardType)
        {
            string cacheKey = leaderboardType;
            if (clientCache.TryGetValue(cacheKey, out var cachedItem) && DateTime.UtcNow - cachedItem.fetchTime < cacheDuration)
            {
                Debug.Log($"[LeaderboardService] SUCCESS: Loading '{cacheKey}' from client-side cache.");
                return cachedItem.data;
            }

            Debug.Log($"[LeaderboardService] INFO: Requesting signed URL for '{leaderboardType}' leaderboard...");
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
                    Debug.LogError($"[LeaderboardService] FAILED (Editor): App Check blocked the call. " +
                                   $"Ensure 'FIREBASE_APP_CHECK_DEBUG_TOKEN' environment variable is set correctly. Error: [{e.ErrorCode}] {e.Message}");
                }
                else
                {
                    Debug.LogError($"[LeaderboardService] FAILED to get signed URL for '{leaderboardType}'. Firebase Functions Error: [{e.ErrorCode}] {e.Message}");
                }
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LeaderboardService] FAILED with a generic exception while getting signed URL for '{leaderboardType}'. Error: {e.Message}");
                return null;
            }

            // =================================================================================
            //  --- KURŞUN GEÇİRMEZ (BULLETPROOF) URL ÇIKARMA BÖLÜMÜ ---
            // =================================================================================

            string signedUrl = null; // Başlangıçta null olarak ayarla.

            // 1. Önce gelen verinin null olup olmadığını kontrol et.
            if (result.Data != null)
            {
                // 2. Veriyi en genel sözlük tipine (IDictionary) çevirmeyi dene.
                // Bu, <object, object> veya <string, object> gibi olası tüm varyasyonları yakalar.
                if (result.Data is IDictionary<object, object> rawData)
                {
                    Debug.Log($"[LeaderboardService-DEBUG] Successfully cast result.Data to IDictionary<object, object>. Iterating through keys...");

                    // 3. Sözlüğün içindeki her bir anahtar-değer çiftini manuel olarak dolaş.
                    foreach (var kvp in rawData)
                    {
                        // 4. Anahtarın bir string olup olmadığını ve "signedUrl" ile eşleşip eşleşmediğini kontrol et.
                        // Bu, tip döküm hatalarını önler.
                        if (kvp.Key is string keyAsString && keyAsString == "signedUrl")
                        {
                            // 5. Değerin bir string olup olmadığını kontrol et.
                            if (kvp.Value is string valueAsString)
                            {
                                signedUrl = valueAsString;
                                Debug.Log($"[LeaderboardService-DEBUG] SUCCESS: Found and extracted 'signedUrl' key with value: {signedUrl.Substring(0, 50)}...");
                                break; // Anahtarı bulduk, döngüden çık.
                            }
                            else
                            {
                                Debug.LogWarning($"[LeaderboardService-DEBUG] Found 'signedUrl' key, but its value is not a string. Value Type: {kvp.Value?.GetType().Name}");
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogError($"[LeaderboardService-DEBUG] result.Data was not null, but could not be cast to IDictionary<object, object>. Actual Type: {result.Data.GetType().Name}");
                }
            }

            // 6. Tüm bu çabalardan sonra hala bir URL'imiz yoksa, hata ver ve çık.
            if (string.IsNullOrEmpty(signedUrl))
            {
                // Önceki debug loglamasını buraya taşıyoruz ki daha anlamlı olsun.
                var debugData = result.Data as IDictionary<object, object>;
                string content = "Could not cast to dictionary or it was empty.";
                if (debugData != null)
                {
                    content = string.Join(", ", debugData.Select(kvp => $"'{kvp.Key}': '{kvp.Value}'"));
                }

                Debug.LogError($"[LeaderboardService] FATAL: Could not extract a valid 'signedUrl' string from the function's response. Raw content: {{ {content} }}");
                return null;
            }

            // =================================================================================
            //  --- GÜVENLİ BÖLGE: Artık 'signedUrl' değişkeninin dolu olduğundan eminiz ---
            // =================================================================================

            Debug.Log($"[LeaderboardService] INFO: Received and validated signed URL. Downloading data...");

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
                        Debug.LogWarning($"[LeaderboardService] WARNING: Leaderboard cache not found (404) for '{leaderboardType}'.");
                        return new List<ScoreEntry>();
                    }
                    else
                    {
                        Debug.LogError($"[LeaderboardService] FATAL: Could not download leaderboard from signed URL. Code: {www.responseCode}, Error: {www.error}.");
                    }
                    return null;
                }
            }
        }
        
        /// <summary>
        /// A robust helper method to parse the JSON array from the server into a list of ScoreEntry objects.
        /// </summary>
        private static List<ScoreEntry> ParseJsonToScoreEntries(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Trim() == "[]")
            {
                // An empty array is a valid response for an empty leaderboard.
                return new List<ScoreEntry>();
            }

            try
            {
                // JsonUtility cannot parse a root array directly, so we wrap it in an object.
                var wrapper = JsonUtility.FromJson<JsonScoreEntryListWrapper>("{\"entries\":" + json + "}");
                if (wrapper?.entries != null)
                {
                    // Use LINQ to project the JSON DTOs to our main ScoreEntry model.
                    return wrapper.entries.Select(jsonEntry => new ScoreEntry
                    {
                        userId = jsonEntry.userId,
                        score = jsonEntry.score,
                        username = jsonEntry.username,
                        rank = jsonEntry.rank
                    }).ToList();
                }
                Debug.LogError($"[LeaderboardService] FATAL: Failed to parse JSON. Wrapper or entries list is null after deserialization.");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LeaderboardService] FATAL: Exception during JSON parsing. Ensure the JSON format is correct. Error: {e.Message}");
                return null;
            }
        }

        // --- PUBLIC API ---

        /// <summary>
        /// Fetches the global highscores leaderboard.
        /// </summary>
        public static Task<List<ScoreEntry>> GetHighscoresAsync() => FetchLeaderboardDataViaSignedUrlAsync("global");

        /// <summary>
        /// Fetches the active seasonal leaderboard.
        /// </summary>
        public static Task<List<ScoreEntry>> GetSeasonScoresAsync() => FetchLeaderboardDataViaSignedUrlAsync("seasonal");

        /// <summary>
        /// Submits a player's score to the backend. Requires an authenticated user.
        /// </summary>
        /// <param name="score">The score to submit.</param>
        /// <param name="type">The type of leaderboard ("highscore" or "seasonal").</param>
        public static async Task SubmitScoreAsync(int score, string type)
        {
            if (!AuthService.IsSignedIn)
            {
                Debug.LogWarning($"[LeaderboardService] Aborting score submission for type '{type}': User is not signed in.");
                return;
            }

            var data = new Dictionary<string, object> { { "score", score }, { "type", type } };
            Debug.Log($"[LeaderboardService] Preparing to submit score. Type: '{type}', Score: {score}, User: {AuthService.UserId.Substring(0, 8)}...");

            try
            {
                var function = functions.GetHttpsCallable("submitScore");
                var result = await function.CallAsync(data);

                if (result.Data is IDictionary<string, object> resultData)
                {
                    var status = resultData.ContainsKey("status") ? resultData["status"].ToString() : "unknown";
                    var message = resultData.ContainsKey("message") ? resultData["message"].ToString() : "no message";
                    Debug.Log($"[LeaderboardService] SUBMISSION RESULT: Type: '{type}', Status: '{status}', Message: '{message}'");
                }
            }
            catch (FunctionsException e)
            {
                Debug.Log($"[LeaderboardService] FAILED to submit score. Error: [{e.ErrorCode}] {e.Message}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LeaderboardService] FAILED with a generic exception while submitting score. Error: {e.Message}");
            }
        }
    }
}