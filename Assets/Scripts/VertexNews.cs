// FILE: VertexNews.cs
// FINAL & PRODUCTION-READY VERSION
// This version implements a robust, time-based local caching system to optimize server load and enable offline viewing.

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Firebase;
using Firebase.Functions;

/// <summary>
/// Manages fetching, caching, and displaying news articles from a remote source.
/// Implements a 3-day local cache using PlayerPrefs to minimize server requests and enable offline functionality.
/// Relies on AppInitializer to prepare all necessary Firebase instances before it runs.
/// </summary>
[AddComponentMenu("Vertex/Vertex News System")]
public sealed class VertexNews : MonoBehaviour
{
    #region Constants

    private const string NewsCacheKey = "VertexNews_JsonCache";
    private const string NewsTimestampKey = "VertexNews_TimestampCache";
    private const double CacheStaleThresholdDays = 3.0;

    #endregion

    #region Inspector Configuration

    [Header("API Configuration")]
    [Tooltip("The region where your Cloud Functions are deployed (e.g., 'europe-west1').")]
    [SerializeField]
    private string functionsRegion = "europe-west1";

    [Header("Scene UI References")]
    [Tooltip("The UI Text element used to display loading and error messages to the user.")]
    [SerializeField]
    private Text statusText;
    [Tooltip("The parent Transform where news item prefabs will be instantiated.")]
    [SerializeField]
    private Transform contentParent;
    [Tooltip("The prefab for a single news article. It must have a NewsItemUI component.")]
    [SerializeField]
    private GameObject newsItemPrefab;

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets a value indicating whether news data has been successfully loaded, either from cache or server.
    /// </summary>
    public bool IsDataLoaded { get; private set; } = false;

    #endregion

    #region Firebase Services

    private FirebaseFunctions _functionsWithoutAppCheck;
    /// <summary>
    /// Gets a memoized Firebase Functions instance that uses a secondary, non-default app without App Check.
    /// </summary>
    private FirebaseFunctions FunctionsWithoutAppCheck
    {
        get
        {
            if (_functionsWithoutAppCheck == null)
            {
                FirebaseApp noAppCheckApp = AppInitializer.NoAppCheckApp;
                if (noAppCheckApp == null)
                {
                    Debug.LogError("[VertexNews] CRITICAL: The NoAppCheckApp instance is null.");
                    return null;
                }
                _functionsWithoutAppCheck = FirebaseFunctions.GetInstance(
                    noAppCheckApp,
                    functionsRegion
                );
            }
            return _functionsWithoutAppCheck;
        }
    }

    #endregion

    #region Private State & Caching

    private static List<Article> s_cachedArticles;
    private static readonly Dictionary<string, Sprite> s_cachedSprites = new Dictionary<
        string,
        Sprite
    >();
    private static int s_cachedLanguage = -1;

    private bool _isCurrentlyWorking = false;
    private Coroutine _loadingAnimationCoroutine;
    private bool IsLanguageTurkish => PlayerPrefs.GetInt("SelectedLanguage", 0) == 1;

    #endregion

    #region JSON Data Structures

    [Serializable]
    public class NewsData
    {
        public List<Article> articles;
    }
    [Serializable]
    public class Article
    {
        public string id;
        public Translations translations;
        public Cover cover;
        public List<string> references;
        public string publishedAt;
    }
    [Serializable]
    public class Translations
    {
        public TranslationItem en;
        public TranslationItem tr;
    }
    [Serializable]
    public class Cover
    {
        public string en;
        public string tr;
    }
    [Serializable]
    public class TranslationItem
    {
        public string title;
        public string summary;
        public string content;
    }

    #endregion

    #region Unity Lifecycle

    private void OnEnable()
    {
        StartCoroutine(OrchestrateDisplayRoutine());
    }

    private void OnDisable()
    {
        if (_loadingAnimationCoroutine != null)
            StopCoroutine(_loadingAnimationCoroutine);
        StopAllCoroutines();
        _isCurrentlyWorking = false;
        Debug.Log("[VertexNews] Panel disabled. All coroutines stopped.");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Manually triggers a new data fetch operation, bypassing the time-based cache check.
    /// </summary>
    public void ForceRefresh()
    {
        if (_isCurrentlyWorking)
        {
            Debug.LogWarning(
                "[VertexNews] A refresh is already in progress. Manual trigger ignored."
            );
            return;
        }
        Debug.Log("[VertexNews] A manual refresh has been triggered.");
        s_cachedArticles = null; // Invalidate memory cache
        PlayerPrefs.DeleteKey(NewsTimestampKey); // Invalidate local cache to force fetch
        IsDataLoaded = false;
        StartCoroutine(OrchestrateDisplayRoutine());
    }

    #endregion

    #region Core Logic


    /// <summary>
    /// The main coroutine that orchestrates the entire news display process with local caching.
    /// </summary>
    private IEnumerator OrchestrateDisplayRoutine()
    {
        yield return new WaitUntil(() => AppInitializer.FirebaseReady);
        if (_isCurrentlyWorking) yield break;
        _isCurrentlyWorking = true;

        CleanUpView();
        SetStatusLoading();

        Debug.Log("================ NEW ORCHESTRATION CYCLE ================");

        bool languageChanged = s_cachedLanguage != -1 && s_cachedLanguage != PlayerPrefs.GetInt("SelectedLanguage", 0);
        if (languageChanged)
        {
            Debug.LogWarning("[VertexNews Decision] Language has changed. Invalidating cache.");
        }

        bool localCacheExists = TryLoadArticlesFromLocalCache();
        if (!localCacheExists)
        {
            Debug.LogWarning("[VertexNews Decision] No local cache found in PlayerPrefs.");
        }

        bool isCacheStale = IsLocalCacheStale();
        if (isCacheStale)
        {
            Debug.LogWarning("[VertexNews Decision] Local cache is STALE.");
        }
        else if (localCacheExists)
        {
            Debug.Log("<color=green>[VertexNews Decision] Local cache is FRESH.</color>");
        }

        bool isCacheUsable = localCacheExists && !isCacheStale && !languageChanged;

        if (isCacheUsable)
        {
            Debug.Log($"<color=cyan>[VertexNews Path] Using FRESH local cache. Age: {(DateTime.UtcNow - GetCacheTimestamp()).TotalHours:F1} hours.</color>");
            IsDataLoaded = true;
        }
        else
        {
            string reason = !localCacheExists ? "No local cache" : (languageChanged ? "Language changed" : "Cache is stale");
            Debug.Log($"<color=yellow>[VertexNews Path] Reason: {reason}. Attempting to fetch from server.</color>");
            yield return StartCoroutine(RefreshArticleCacheRoutine());
        }

        if (IsDataLoaded && s_cachedArticles != null && s_cachedArticles.Count > 0)
        {
            yield return StartCoroutine(PopulateViewRoutine(s_cachedArticles));
        }
        else
        {
            if (localCacheExists)
            {
                Debug.LogWarning("[VertexNews Path] Server fetch failed. Displaying STALE local cache as a fallback.");
                IsDataLoaded = true;
                yield return StartCoroutine(PopulateViewRoutine(s_cachedArticles));
            }
            else
            {
                if (statusText != null && statusText.color != Color.red) SetStatusError("No news articles available.");
            }
        }

        _isCurrentlyWorking = false;
    }

    /// <summary>
    /// Handles the full server-side fetch-and-cache process for articles.
    /// </summary>
    private IEnumerator RefreshArticleCacheRoutine()
    {
        string signedNewsJsonUrl = null;
        yield return StartCoroutine(
            GetSignedUrlForNewsCacheRoutine(url => signedNewsJsonUrl = url)
        );
        if (string.IsNullOrEmpty(signedNewsJsonUrl))
        {
            SetStatusError("Could not get news data link from server.");
            yield break;
        }

        // The fetch routine now returns the raw JSON string for caching purposes.
        string fetchedJson = null;
        yield return StartCoroutine(
            FetchNewsJsonFromUrlRoutine(signedNewsJsonUrl, json => fetchedJson = json)
        );

        if (!string.IsNullOrEmpty(fetchedJson))
        {
            // If the fetch was successful, parse it and save to the local cache.
            if (TryParseAndCacheArticles(fetchedJson))
            {
                IsDataLoaded = true;
                Debug.Log(
                    $"[VertexNews] Successfully fetched and cached {s_cachedArticles.Count} new articles."
                );
            }
            else
            {
                SetStatusError("Failed to parse server data.");
            }
        }
        // If fetch fails, IsDataLoaded remains false, and the orchestrator will decide if stale data can be used.
    }

    #endregion

    #region Caching System

    /// <summary>
    /// Tries to load and parse the news JSON from PlayerPrefs into the static cache.
    /// </summary>
    /// <returns>True if loading and parsing were successful, false otherwise.</returns>
    private bool TryLoadArticlesFromLocalCache()
    {
        string cachedJson = PlayerPrefs.GetString(NewsCacheKey, null);
        if (string.IsNullOrEmpty(cachedJson))
        {
            Debug.Log("[Cache System] `PlayerPrefs` check for `NewsCacheKey` returned NULL or EMPTY.");
            return false;
        }

        Debug.Log("<color=lime>[Cache System] Found data in `PlayerPrefs` for `NewsCacheKey`. Attempting to parse...</color>");
        return TryParseAndCacheArticles(cachedJson);
    }

    /// <summary>
    /// Checks if the timestamp stored in PlayerPrefs is older than the defined threshold.
    /// </summary>
    private bool IsLocalCacheStale()
    {
        DateTime cachedTimestamp = GetCacheTimestamp();

        if (cachedTimestamp == DateTime.MinValue)
        {
            Debug.Log("[Cache System] `GetCacheTimestamp` returned MinValue. Cache is considered STALE.");
            return true;
        }

        TimeSpan cacheAge = DateTime.UtcNow - cachedTimestamp;
        bool isStale = cacheAge.TotalDays > CacheStaleThresholdDays;

        Debug.Log($"[Cache System] Current Time (UTC): {DateTime.UtcNow:O}");
        Debug.Log($"[Cache System] Cached Timestamp (UTC): {cachedTimestamp:O}");
        Debug.Log($"[Cache System] Cache Age: {cacheAge.TotalHours:F2} hours. Stale Threshold: {CacheStaleThresholdDays * 24} hours.");
        Debug.Log($"[Cache System] Is Stale? -> {isStale}");

        return isStale;
    }

    /// <summary>
    /// Safely retrieves and parses the cache timestamp from PlayerPrefs.
    /// </summary>
    /// <returns>The stored DateTime in UTC, or DateTime.MinValue if not found or invalid.</returns>
    private DateTime GetCacheTimestamp()
    {
        string timestampIso = PlayerPrefs.GetString(NewsTimestampKey, null);
        if (string.IsNullOrEmpty(timestampIso))
            return DateTime.MinValue;

        if (
            DateTime.TryParse(
                timestampIso,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal,
                out DateTime result
            )
        )
        {
            return result;
        }
        return DateTime.MinValue;
    }

    /// <summary>
    /// Parses a JSON string, populates the static article cache, and saves the data to PlayerPrefs.
    /// Crucially, it saves the CURRENT server time as the cache timestamp.
    /// </summary>
    /// <param name="jsonContent">The raw JSON string of the articles array.</param>
    /// <returns>True if parsing and caching were successful.</returns>
    private bool TryParseAndCacheArticles(string jsonContent)
    {
        try
        {
            string wrappedJson = $"{{\"articles\":{jsonContent}}}";
            NewsData newsData = JsonUtility.FromJson<NewsData>(wrappedJson);

            if (newsData?.articles == null || newsData.articles.Count == 0)
            {
                Debug.LogWarning("[VertexNews] Parsed news data is null or empty. Cache will not be updated.");
                return false;
            }

            s_cachedArticles = newsData.articles;
            s_cachedLanguage = PlayerPrefs.GetInt("SelectedLanguage", 0);

            // --- YENİ VE DOĞRU MANTIK ---
            // Haber içeriğindeki tarihe bakmıyoruz.
            // Başarıyla veri çektiğimiz ŞİMDİKİ anın UTC zamanını alıyoruz.
            // Bu, cihazın zamanını temel alır. Hileye karşı %100 korumalı olmasa da,
            // bu sistem için yeterince güvenilir ve basittir.
            DateTime fetchTimestamp = DateTime.UtcNow;

            // Veriyi ve veriyi çektiğimiz zamanı PlayerPrefs'e kaydediyoruz.
            PlayerPrefs.SetString(NewsCacheKey, jsonContent);
            PlayerPrefs.SetString(NewsTimestampKey, fetchTimestamp.ToString("o", CultureInfo.InvariantCulture));
            PlayerPrefs.Save();

            Debug.Log($"<color=lime>[Cache System] Successfully parsed and saved cache. New cache timestamp (UTC): {fetchTimestamp:O}</color>");

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[VertexNews] Failed to parse and cache JSON. Clearing invalid cache. Error: {ex.Message}");
            // Eğer parse etme sırasında hata olursa, bozuk cache kalmasın diye temizliyoruz.
            PlayerPrefs.DeleteKey(NewsCacheKey);
            PlayerPrefs.DeleteKey(NewsTimestampKey);
            PlayerPrefs.Save();
            return false;
        }
    }

    #endregion

    #region Network Routines

    /// <summary>
    /// Calls a Cloud Function to get a temporary, signed URL for the main news JSON file.
    /// </summary>
    private IEnumerator GetSignedUrlForNewsCacheRoutine(Action<string> onComplete)
    {
        var functionsInstance = FunctionsWithoutAppCheck;
        if (functionsInstance == null)
        {
            onComplete(null);
            yield break;
        }

        var callableTask = functionsInstance.GetHttpsCallable("getNewsCacheUrl").CallAsync();
        yield return new WaitUntil(() => callableTask.IsCompleted);

        if (callableTask.IsFaulted)
        {
            Debug.LogError(
                $"[VertexNews] Functions call to get news cache URL failed: {callableTask.Exception}"
            );
            onComplete(null);
            yield break;
        }

        var result = callableTask.Result;
        var data = result.Data as Dictionary<object, object>;

        if (
            data != null
            && data.TryGetValue("signedUrl", out object urlObject)
            && urlObject is string signedUrl
        )
        {
            onComplete(signedUrl);
        }
        else
        {
            Debug.LogError(
                $"[VertexNews] Server response did not contain a valid 'signedUrl' string."
            );
            onComplete(null);
        }
    }

    /// <summary>
    /// Downloads the raw news JSON string from a given URL.
    /// </summary>
    private IEnumerator FetchNewsJsonFromUrlRoutine(string signedUrl, Action<string> onComplete)
    {
        using (var www = UnityWebRequest.Get(signedUrl))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                onComplete(www.downloadHandler.text);
            }
            else
            {
                Debug.LogError(
                    $"[VertexNews] Failed to download JSON from signed URL. Error: {www.error}"
                );
                onComplete(null);
            }
        }
    }

    #endregion

    #region UI Population
    private IEnumerator PopulateViewRoutine(List<Article> articles)
    {
        if (contentParent.TryGetComponent(out VerticalLayoutGroup layoutGroup))
        {
            layoutGroup.enabled = false;
        }
        var canvasGroups = new List<CanvasGroup>();
        foreach (var article in articles)
        {
            GameObject newsItemGO = Instantiate(newsItemPrefab, contentParent);
            if (newsItemGO.TryGetComponent(out CanvasGroup cg))
            {
                cg.alpha = 0;
                canvasGroups.Add(cg);
            }
            StartCoroutine(PopulateSingleNewsItemRoutine(newsItemGO, article));
        }
        if (layoutGroup != null)
        {
            layoutGroup.enabled = true;
        }
        yield return new WaitForEndOfFrame();
        if (statusText != null)
            statusText.gameObject.SetActive(false);
        yield return StartCoroutine(FadeCanvasGroups(canvasGroups, 0f, 1f, 0.4f));
    }
    private IEnumerator PopulateSingleNewsItemRoutine(GameObject itemInstance, Article articleData)
    {
        if (!itemInstance.TryGetComponent(out NewsItemUI ui))
        {
            Debug.LogError(
                $"[VertexNews] CRITICAL: News Item Prefab is MISSING the NewsItemUI component!",
                itemInstance
            );
            yield break;
        }
        TranslationItem translation = IsLanguageTurkish
            ? articleData.translations.tr
            : articleData.translations.en;
        string coverPath = IsLanguageTurkish ? articleData.cover.tr : articleData.cover.en;
        ui.TitleText.text = translation.title;
        ui.SummaryText.text = translation.summary;
        if (ui.ContentText != null)
        {
            ui.ContentText.text = translation.content;
        }
        if (ui.CoverImage != null)
        {
            if (!string.IsNullOrEmpty(coverPath))
            {
                ui.CoverImage.gameObject.SetActive(true);
                if (s_cachedSprites.TryGetValue(coverPath, out Sprite cachedSprite))
                {
                    ui.CoverImage.sprite = cachedSprite;
                }
                else
                {
                    yield return StartCoroutine(
                        DownloadAndApplyImageRoutine(coverPath, ui.CoverImage)
                    );
                }
            }
            else
            {
                ui.CoverImage.gameObject.SetActive(false);
            }
        }
    }
    private IEnumerator DownloadAndApplyImageRoutine(string imagePath, Image targetImage)
    {
        string signedImageUrl = null;
        yield return StartCoroutine(
            GetSignedUrlForCoverRoutine(imagePath, url => signedImageUrl = url)
        );
        if (string.IsNullOrEmpty(signedImageUrl))
        {
            targetImage.gameObject.SetActive(false);
            yield break;
        }
        using (var www = UnityWebRequestTexture.GetTexture(signedImageUrl))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                Sprite newSprite = Sprite.Create(
                    texture,
                    new Rect(0.0f, 0.0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
                targetImage.sprite = newSprite;
                s_cachedSprites[imagePath] = newSprite;
            }
            else
            {
                targetImage.gameObject.SetActive(false);
            }
        }
    }
    private IEnumerator GetSignedUrlForCoverRoutine(string filePath, Action<string> onComplete)
    {
        var functionsInstance = FunctionsWithoutAppCheck;
        if (functionsInstance == null)
        {
            onComplete(null);
            yield break;
        }
        var callableTask = functionsInstance
            .GetHttpsCallable("getCoverDownloadUrl")
            .CallAsync(new Dictionary<string, object> { { "filePath", filePath } });
        yield return new WaitUntil(() => callableTask.IsCompleted);
        if (callableTask.IsFaulted)
        {
            onComplete(null);
            yield break;
        }
        var result = callableTask.Result;
        var data = result.Data as Dictionary<object, object>;
        if (
            data != null
            && data.TryGetValue("signedUrl", out object urlObject)
            && urlObject is string signedUrl
        )
        {
            onComplete(signedUrl);
        }
        else
        {
            onComplete(null);
        }
    }
    #endregion
    #region UI State Management & Helpers
    private void SetStatusLoading()
    {
        if (statusText == null)
            return;
        statusText.gameObject.SetActive(true);
        statusText.color = Color.white;
        statusText.text = IsLanguageTurkish ? "Yükleniyor..." : "Loading...";
        if (_loadingAnimationCoroutine != null)
            StopCoroutine(_loadingAnimationCoroutine);
        _loadingAnimationCoroutine = StartCoroutine(AnimateLoadingTextRoutine());
    }
    private void SetStatusError(string errorMessage)
    {
        IsDataLoaded = false;
        if (statusText == null)
            return;
        if (_loadingAnimationCoroutine != null)
        {
            StopCoroutine(_loadingAnimationCoroutine);
            _loadingAnimationCoroutine = null;
        }
        statusText.gameObject.SetActive(true);
        statusText.color = Color.red;
        statusText.text = IsLanguageTurkish ? "Bir hata oluştu." : "An error occurred.";
        Debug.LogError($"[VertexNews] Final Error State Set: {errorMessage}");
    }
    private IEnumerator AnimateLoadingTextRoutine()
    {
        statusText.color = new Color(
            statusText.color.r,
            statusText.color.g,
            statusText.color.b,
            1f
        );
        while (true)
        {
            float alpha = 0.6f + Mathf.PingPong(Time.time * 0.8f, 0.4f);
            statusText.color = new Color(
                statusText.color.r,
                statusText.color.g,
                statusText.color.b,
                alpha
            );
            yield return null;
        }
    }
    private void CleanUpView()
    {
        if (contentParent == null)
            return;
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
    }
    private IEnumerator FadeCanvasGroups(
        IEnumerable<CanvasGroup> canvasGroups,
        float from,
        float to,
        float duration
    )
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float alpha = Mathf.Lerp(from, to, elapsedTime / duration);
            foreach (var cg in canvasGroups)
            {
                if (cg != null)
                    cg.alpha = alpha;
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        foreach (var cg in canvasGroups)
        {
            if (cg != null)
                cg.alpha = to;
        }
    }
    #endregion
}