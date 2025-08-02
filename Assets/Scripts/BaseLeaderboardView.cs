/***************************************************************************
 *  BaseLeaderboardView.cs (2025-07-06 - CONTEXT FIX - FINAL)
 *  -----------------------------------------------------------------------
 *  • CONTEXT FIX: Re-added the 'Lang' property that was inadvertently
 *    omitted. This property is required by derived views for localization.
 *  • FLAWLESS OPTIMISTIC UPDATES: Skips execution on data fetch failures
 *    or when displaying mock data in the Unity Editor.
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Vertex.Backend;

public abstract class BaseLeaderboardView : MonoBehaviour
{
    [Header("Base Leaderboard UI")]
    [SerializeField] protected Transform entryDisplayParent;
    [SerializeField] protected EntryDisplay entryDisplayPrefab;
    [SerializeField] protected Text emptyListMessageText;
    [SerializeField] protected CanvasGroup loadingPanel;

    [Header("Base Leaderboard Options")]
    [SerializeField] protected int entriesToDisplay = 100;
    [SerializeField] protected float minLoadingDisplayTime = 0.5f;
    [SerializeField] protected float panelFadeOutDuration = 0.3f;
    [SerializeField] protected float entriesFadeInDuration = 0.4f;

    private CanvasGroup _entryParentCanvasGroup;
    protected List<ScoreEntry> cachedEntries = new List<ScoreEntry>();
    private bool isRefreshing = false;

    // --- FLAWLESS FIX: Re-added the missing Lang property ---
    protected int Lang => PlayerPrefs.GetInt("SelectedLanguage", 0);
    
    private bool _lastRefreshFailed = false;

    #region Unity Lifecycle & Abstract Methods
    
    protected virtual void Awake()
    {
        if (entryDisplayParent)
        {
            _entryParentCanvasGroup = entryDisplayParent.GetComponent<CanvasGroup>();
            if (_entryParentCanvasGroup == null)
            {
                _entryParentCanvasGroup = entryDisplayParent.gameObject.AddComponent<CanvasGroup>();
                Debug.LogWarning($"[{GetType().Name}] A 'Canvas Group' was auto-added to '{entryDisplayParent.name}' for fade effects. It's recommended to add this manually.", gameObject);
            }
        }
    }

    protected virtual void OnDisable()
    {
        ClearRows();
        cachedEntries.Clear();
        isRefreshing = false;
        _lastRefreshFailed = false;
        Debug.Log($"[{GetType().Name}] View disabled – cache and UI rows cleared.");
    }

    protected abstract Task<List<ScoreEntry>> FetchLeaderboardDataAsync();
    protected abstract int GetLocalPlayerScore();
    protected virtual string LoadingMessage => "Loading...";
    
    #endregion

    #region Core Refresh Lifecycle
    
    public virtual async Task DisplayLeaderboard()
    {
        if (!gameObject.activeInHierarchy || isRefreshing) return;
        await RefreshFromServer();
    }
    
    public virtual async Task RefreshFromServer()
    {
        if (isRefreshing) return;
        isRefreshing = true;
        float refreshStartTime = Time.realtimeSinceStartup;

        OnRefreshStarted();

        try
        {
            var fetchedData = await FetchLeaderboardDataAsync();
            if (fetchedData == null)
            {
                _lastRefreshFailed = true;
                cachedEntries.Clear();
                Debug.LogError($"[{GetType().Name}] Fetch returned null. The view will display a failure message.");
            }
            else
            {
                _lastRefreshFailed = false;
                cachedEntries = fetchedData;
                Debug.Log($"[{GetType().Name}] Successfully fetched {cachedEntries.Count} entries.");
            }
        }
        catch (Exception e)
        {
            _lastRefreshFailed = true;
            cachedEntries.Clear();
            Debug.LogError($"[{GetType().Name}] A critical error occurred during data fetching: {e.Message}\n{e.StackTrace}");
        }
        finally
        {
            float elapsedTime = Time.realtimeSinceStartup - refreshStartTime;
            if (elapsedTime < minLoadingDisplayTime)
            {
                await Task.Delay(TimeSpan.FromSeconds(minLoadingDisplayTime - elapsedTime));
            }

            isRefreshing = false;
            await OnRefreshFinishedAsync();
        }
    }

    protected virtual void OnRefreshStarted()
    {
        ClearRows();
        if (_entryParentCanvasGroup)
        {
            _entryParentCanvasGroup.alpha = 0;
            _entryParentCanvasGroup.interactable = false;
        }
        if (emptyListMessageText) emptyListMessageText.gameObject.SetActive(false);
        if (loadingPanel)
        {
            var t = loadingPanel.GetComponentInChildren<Text>(true);
            if (t) t.text = LoadingMessage;
            loadingPanel.alpha = 1f;
            loadingPanel.gameObject.SetActive(true);
            loadingPanel.blocksRaycasts = true;
        }
    }

    protected virtual async Task OnRefreshFinishedAsync()
    {
        if (loadingPanel && loadingPanel.gameObject.activeInHierarchy)
        {
            LeanTween.alphaCanvas(loadingPanel, 0f, panelFadeOutDuration);
            await Task.Delay(TimeSpan.FromSeconds(panelFadeOutDuration));
            loadingPanel.blocksRaycasts = false;
            loadingPanel.gameObject.SetActive(false);
        }

        PaintRows();

        if (cachedEntries.Count > 0 && !_lastRefreshFailed && _entryParentCanvasGroup)
        {
            _entryParentCanvasGroup.interactable = true;
            LeanTween.alphaCanvas(_entryParentCanvasGroup, 1f, entriesFadeInDuration);
        }
    }
    
    #endregion
    
    #region UI Painting & Optimistic Updates
    
    protected void PaintRows()
    {
        ClearRows();
        ApplyOptimisticUpdate();

        if (cachedEntries.Count == 0)
        {
            if (emptyListMessageText)
            {
                emptyListMessageText.text = _lastRefreshFailed
                    ? (Lang == 1 ? "veri çekilemedi :(" : "could not fetch data :(")
                    : (Lang == 1 ? "liderlik tablosu boş" : "leaderboard is empty");
                emptyListMessageText.gameObject.SetActive(true);
            }
            if (_entryParentCanvasGroup) _entryParentCanvasGroup.alpha = 0;
            return;
        }

        if (emptyListMessageText) emptyListMessageText.gameObject.SetActive(false);

        var entriesToPaint = cachedEntries.Take(entriesToDisplay).ToList();
        int rank = 1;
        foreach (var entry in entriesToPaint)
        {
            entry.rank = rank++;
            CreateRow(entry);
        }
    }
    
    private void ApplyOptimisticUpdate()
    {
        if (_lastRefreshFailed) return;

        #if UNITY_EDITOR
        if (cachedEntries.Any() && cachedEntries[0].userId.StartsWith("editor-user-id-"))
        {
            return;
        }
        #endif
        
        if (!AuthService.IsSignedIn) return;

        int myLocalScore = GetLocalPlayerScore();
        if (myLocalScore <= 0) return;

        string myUserId = AuthService.UserId;
        if (string.IsNullOrEmpty(myUserId)) return;
        
        var myEntryInList = cachedEntries.FirstOrDefault(e => e.userId == myUserId);
        if (myEntryInList != null)
        {
            myEntryInList.score = Mathf.Max(myEntryInList.score, myLocalScore);
        }
        else
        {
            bool shouldAdd = !cachedEntries.Any() || cachedEntries.Count < entriesToDisplay || myLocalScore > cachedEntries.Min(e => e.score);
            if (shouldAdd)
            {
                cachedEntries.Add(new ScoreEntry
                {
                    userId = myUserId,
                    username = AuthService.Username,
                    score = myLocalScore,
                    rank = -1
                });
            }
        }
        
        cachedEntries = cachedEntries.OrderByDescending(e => e.score).ToList();
    }
    
    #endregion
    
    #region UI Helpers
    
    protected void ClearRows()
    {
        if (entryDisplayParent)
        {
            foreach (Transform child in entryDisplayParent) Destroy(child.gameObject);
        }
    }

    private void CreateRow(ScoreEntry entry)
    {
        if (entryDisplayPrefab && entryDisplayParent)
        {
            Instantiate(entryDisplayPrefab, entryDisplayParent, false).SetEntry(entry);
        }
    }
    
    #endregion
}