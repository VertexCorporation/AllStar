/***************************************************************************
 *  BaseLeaderboardView.cs (2025-07-08 - PULSING LOADER - FINAL)
 *  -----------------------------------------------------------------------
 *  • DYNAMIC LOADING: The static "Loading..." text now has a continuous
 *    fade-in/fade-out (pulsing) animation via LeanTween. This provides
 *    superior visual feedback to the user, indicating that the system
 *    is actively working.
 *  • ROBUST CANCELLATION: The loading animation is cleanly and reliably
 *    cancelled via a unique tween ID when the refresh cycle completes or
 *    the view is disabled, preventing orphaned tweens.
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Vertex.Backend;

/// <summary>
/// An abstract base class for displaying any leaderboard. It handles the core
/// lifecycle of fetching data, managing loading states, and painting UI rows,
/// while delegating the specifics of data fetching to derived classes.
/// </summary>
public abstract class BaseLeaderboardView : MonoBehaviour
{
    [Header("Base Leaderboard UI")]
    [Tooltip("The parent Transform where leaderboard entry prefabs will be instantiated.")]
    [SerializeField] protected Transform entryDisplayParent;
    [Tooltip("The prefab representing a single row in the leaderboard.")]
    [SerializeField] protected EntryDisplay entryDisplayPrefab;
    [Tooltip("A Text component to display messages when the list is empty or fails to load.")]
    [SerializeField] protected Text emptyListMessageText;
    [Tooltip("A CanvasGroup used as a loading overlay. Should contain a Text child.")]
    [SerializeField] protected CanvasGroup loadingPanel;

    [Header("Base Leaderboard Options")]
    [Tooltip("The maximum number of entries to display from the fetched data.")]
    [SerializeField] protected int entriesToDisplay = 100;
    [Tooltip("The minimum duration the loading animation will be visible to prevent jarring flashes.")]
    [SerializeField] protected float minLoadingDisplayTime = 0.5f;
    [Tooltip("The duration of the loading panel's fade-out animation.")]
    [SerializeField] protected float panelFadeOutDuration = 0.3f;
    [Tooltip("The duration of the entry list's fade-in animation.")]
    [SerializeField] protected float entriesFadeInDuration = 0.4f;

    // --- PULSING LOADER SETTINGS ---
    [Header("Pulsing Loader Animation")]
    [Tooltip("The duration of one full pulse cycle (fade out and in).")]
    [SerializeField] [Range(0.5f, 2f)] private float pulseDuration = 1.0f;
    [Tooltip("The minimum alpha the loading text will fade to during a pulse.")]
    [SerializeField] [Range(0.1f, 1f)] private float pulseMinAlpha = 0.4f;

    // --- Private Fields ---
    private CanvasGroup _entryParentCanvasGroup;
    protected List<ScoreEntry> cachedEntries = new List<ScoreEntry>();
    private bool isRefreshing = false;
    private bool _lastRefreshFailed = false;

    // A unique ID to control the loading animation tween.
    private int _loadingTweenId = -1;

    // --- Properties ---
    /// <summary>
    /// Gets the selected language from PlayerPrefs for localization.
    /// Defaults to 0 (e.g., English).
    /// </summary>
    protected int Lang => PlayerPrefs.GetInt("SelectedLanguage", 0);
    
    #region Unity Lifecycle & Abstract Methods
    
    protected virtual void Awake()
    {
        // Automatically find or add a CanvasGroup to the entry parent for fade animations.
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
        // Ensure any running animations are stopped when the view is disabled.
        if (LeanTween.isTweening(_loadingTweenId))
        {
            LeanTween.cancel(_loadingTweenId);
            Debug.Log($"[{GetType().Name}] Active loading tween ({_loadingTweenId}) was cancelled on disable.");
        }

        ClearRows();
        cachedEntries.Clear();
        isRefreshing = false;
        _lastRefreshFailed = false;
        Debug.Log($"[{GetType().Name}] View disabled – cache, UI rows, and animations have been cleared.");
    }

    /// <summary>
    /// Implemented by derived classes to fetch the specific leaderboard data.
    /// </summary>
    /// <returns>A Task containing a list of ScoreEntry objects, or null on failure.</returns>
    protected abstract Task<List<ScoreEntry>> FetchLeaderboardDataAsync();
    
    /// <summary>
    /// Implemented by derived classes to get the local player's current score for optimistic updates.
    /// </summary>
    /// <returns>The local player's score.</returns>
    protected abstract int GetLocalPlayerScore();
    
    /// <summary>
    /// A virtual property to provide a custom loading message.
    /// </summary>
    protected virtual string LoadingMessage => "Loading...";
    
    #endregion

    #region Core Refresh Lifecycle
    
    /// <summary>
    /// Public entry point to start displaying the leaderboard.
    /// </summary>
    public virtual async Task DisplayLeaderboard()
    {
        if (!gameObject.activeInHierarchy || isRefreshing) return;
        await RefreshFromServer();
    }
    
    /// <summary>
    /// Manages the full data refresh cycle, including UI state changes and data fetching.
    /// </summary>
    public virtual async Task RefreshFromServer()
    {
        if (isRefreshing)
        {
            Debug.Log($"[{GetType().Name}] Refresh request ignored, already in progress.");
            return;
        }
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
                Debug.LogError($"[{GetType().Name}] Data fetch returned null. The view will display a failure message.");
            }
            else
            {
                _lastRefreshFailed = false;
                cachedEntries = fetchedData;
                Debug.Log($"[{GetType().Name}] Successfully fetched {cachedEntries.Count} entries from the server.");
            }
        }
        catch (Exception e)
        {
            _lastRefreshFailed = true;
            cachedEntries.Clear();
            Debug.LogError($"[{GetType().Name}] A critical exception occurred during data fetching: {e.Message}\n{e.StackTrace}");
        }
        finally
        {
            // Enforce a minimum display time for the loading animation.
            float elapsedTime = Time.realtimeSinceStartup - refreshStartTime;
            if (elapsedTime < minLoadingDisplayTime)
            {
                await Task.Delay(TimeSpan.FromSeconds(minLoadingDisplayTime - elapsedTime));
            }

            isRefreshing = false;
            await OnRefreshFinishedAsync();
        }
    }

    /// <summary>
    /// Prepares the UI for a refresh by clearing old data and starting the loading animation.
    /// </summary>
    protected virtual void OnRefreshStarted()
    {
        Debug.Log($"[{GetType().Name}] Starting refresh cycle.");
        ClearRows();
        
        if (_entryParentCanvasGroup)
        {
            _entryParentCanvasGroup.alpha = 0;
            _entryParentCanvasGroup.interactable = false;
        }
        if (emptyListMessageText) emptyListMessageText.gameObject.SetActive(false);
        
        if (loadingPanel)
        {
            // Set the loading message text.
            var loadingTextComponent = loadingPanel.GetComponentInChildren<Text>(true);
            if (loadingTextComponent) loadingTextComponent.text = LoadingMessage;

            // Make the panel visible and block raycasts.
            loadingPanel.alpha = 1f;
            loadingPanel.gameObject.SetActive(true);
            loadingPanel.blocksRaycasts = true;

            // Start the continuous pulsing animation.
            _loadingTweenId = LeanTween.alphaCanvas(loadingPanel, pulseMinAlpha, pulseDuration)
                .setLoopPingPong()
                .setEase(LeanTweenType.easeInOutSine)
                .id; // Store the ID to cancel it later.
            
            Debug.Log($"[{GetType().Name}] Pulsing loader animation started with ID: {_loadingTweenId}.");
        }
    }

    /// <summary>
    /// Finalizes the refresh process by stopping the loading animation and painting the new data.
    /// </summary>
    protected virtual async Task OnRefreshFinishedAsync()
    {
        Debug.Log($"[{GetType().Name}] Finishing refresh cycle.");
        // Stop the loading animation and fade out the panel.
        if (loadingPanel && loadingPanel.gameObject.activeInHierarchy)
        {
            // 1. Cancel the looping pulse animation.
            if (LeanTween.isTweening(_loadingTweenId))
            {
                LeanTween.cancel(_loadingTweenId);
                Debug.Log($"[{GetType().Name}] Cancelled pulsing loader animation (ID: {_loadingTweenId}).");
            }
            
            // 2. Start a new, one-shot fade-out animation.
            LeanTween.alphaCanvas(loadingPanel, 0f, panelFadeOutDuration)
                     .setEase(LeanTweenType.easeOutQuad);
            
            await Task.Delay(TimeSpan.FromSeconds(panelFadeOutDuration));
            loadingPanel.blocksRaycasts = false;
            loadingPanel.gameObject.SetActive(false);
        }

        // Repopulate the UI with the fetched data.
        PaintRows();

        // Fade in the new entry list if data exists.
        if (cachedEntries.Count > 0 && !_lastRefreshFailed && _entryParentCanvasGroup)
        {
            _entryParentCanvasGroup.interactable = true;
            LeanTween.alphaCanvas(_entryParentCanvasGroup, 1f, entriesFadeInDuration);
            Debug.Log($"[{GetType().Name}] Fading in the new leaderboard entries.");
        }
    }
    
    #endregion
    
    #region UI Painting & Optimistic Updates
    
    /// <summary>
    /// Clears and repopulates the UI with the current `cachedEntries`.
    /// </summary>
    protected void PaintRows()
    {
        ClearRows();
        
        // Before painting, try to insert the local player's best score for a responsive feel.
        ApplyOptimisticUpdate();

        if (cachedEntries.Count == 0)
        {
            if (emptyListMessageText)
            {
                emptyListMessageText.text = _lastRefreshFailed
                    ? (Lang == 1 ? "veri çekilemedi :(" : "could not fetch data :(") // Example localization
                    : (Lang == 1 ? "liderlik tablosu boş" : "leaderboard is empty");
                emptyListMessageText.gameObject.SetActive(true);
                Debug.Log($"[{GetType().Name}] Painting empty state: {(_lastRefreshFailed ? "Failure" : "Empty List")}.");
            }
            if (_entryParentCanvasGroup) _entryParentCanvasGroup.alpha = 0;
            return;
        }

        if (emptyListMessageText) emptyListMessageText.gameObject.SetActive(false);

        var entriesToPaint = cachedEntries.Take(entriesToDisplay).ToList();
        int rank = 1;
        foreach (var entry in entriesToPaint)
        {
            entry.rank = rank++; // Assign the final rank just before painting.
            CreateRow(entry);
        }
        Debug.Log($"[{GetType().Name}] Painted {entriesToPaint.Count} rows to the UI.");
    }
    
    /// <summary>
    /// Optimistically updates the cached list with the local player's score
    /// to provide immediate feedback without waiting for the next server refresh.
    /// </summary>
    private void ApplyOptimisticUpdate()
    {
        if (_lastRefreshFailed) return; // Don't update if the base data is known to be bad.

        #if UNITY_EDITOR
        // Skip if we are showing mock data in the editor to avoid mixing real and fake entries.
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
            // If the player is already in the list, just update their score if the local one is higher.
            if (myLocalScore > myEntryInList.score)
            {
                Debug.Log($"[{GetType().Name}] Optimistic Update: Updating user {myUserId.Substring(0, 5)}'s score from {myEntryInList.score} to {myLocalScore}.");
                myEntryInList.score = myLocalScore;
            }
        }
        else
        {
            // If the player is not in the list, check if their score is high enough to be added.
            bool shouldAdd = cachedEntries.Count < entriesToDisplay || myLocalScore > cachedEntries.Min(e => e.score);
            if (shouldAdd)
            {
                Debug.Log($"[{GetType().Name}] Optimistic Update: Adding user {myUserId.Substring(0, 5)} to the list with score {myLocalScore}.");
                cachedEntries.Add(new ScoreEntry
                {
                    userId = myUserId,
                    username = AuthService.Username,
                    score = myLocalScore,
                    rank = -1 // Rank will be recalculated during PaintRows.
                });
            }
        }
        
        // Re-sort the list to reflect the optimistic changes.
        cachedEntries = cachedEntries.OrderByDescending(e => e.score).ToList();
    }
    
    #endregion
    
    #region UI Helpers
    
    /// <summary>
    /// Destroys all child GameObjects under the `entryDisplayParent`.
    /// </summary>
    protected void ClearRows()
    {
        if (entryDisplayParent)
        {
            int childCount = entryDisplayParent.childCount;
            if (childCount > 0)
            {
                foreach (Transform child in entryDisplayParent) Destroy(child.gameObject);
                // Debug.Log($"[{GetType().Name}] Cleared {childCount} existing UI rows.");
            }
        }
    }

    /// <summary>
    /// Instantiates and initializes a new entry row prefab.
    /// </summary>
    private void CreateRow(ScoreEntry entry)
    {
        if (entryDisplayPrefab && entryDisplayParent)
        {
            Instantiate(entryDisplayPrefab, entryDisplayParent, false).SetEntry(entry);
        }
    }
    
    #endregion
}