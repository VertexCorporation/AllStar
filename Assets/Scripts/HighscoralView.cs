/***************************************************************************
 *  HighscoralView.cs (2025-07-03 - REFACTORED & DECOUPLED)
 *  -----------------------------------------------------------------------
 *  • ARCHITECTURE FIX: Now a pure 'View' component. All authentication
 *    logic and error handling have been moved to AuthService and UIManager.
 *  • SIMPLIFIED: Contains no complex coroutines or logic, preventing
 *    "inactive GameObject" errors and improving maintainability.
 *  • FOCUSED: Its sole responsibility is to fetch and display the
 *    all-time high scores leaderboard.
 ***************************************************s************************/

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Vertex.Backend;

// Renamed to 'HighscoresView' to better reflect its purpose.
public class HighscoralView : BaseLeaderboardView
{
    [Header("Dependencies")]
    [SerializeField] private ScoreManager scoreMgr;
    // Note: It no longer needs references to UIManager or InputFields.

    #region Unity Lifecycle
    private void OnEnable()
    {
        // The view refreshes itself every time it becomes visible.
        Debug.Log("[HighscoresView] OnEnable: Refreshing view.");
        _ = DisplayLeaderboard();
    }
    #endregion

    #region Base Class Implementation
    protected override string LoadingMessage => Lang == 1 ? "az bekle" : "hold on";

    protected override async Task<List<ScoreEntry>> FetchLeaderboardDataAsync()
    {
        Debug.Log("[HighscoresView] Fetching all-time high scores from the server.");
        var scores = await LeaderboardService.GetHighscoresAsync();
        return scores;
    }

    protected override int GetLocalPlayerScore()
    {
        // It still needs to know the local player's score for optimistic updates.
        return scoreMgr != null ? scoreMgr.HighScore : 0;
    }
    #endregion
}