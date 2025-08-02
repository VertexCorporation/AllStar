/***************************************************************************
 *  SeasonalView.cs (THE FACE - UI-BOUND SCRIPT - FINAL)
 *  -----------------------------------------------------------------------
 *  • This script LIVES INSIDE the deactivatable Leaderboard UI Panel.
 *  • It inherits from BaseLeaderboardView to handle all UI drawing,
 *    loading states, and empty list messages automatically.
 *  • OnEnable, it fetches the season ID and end time from the persistent
 *    SeasonManager singleton before fetching the leaderboard data.
 *  • It correctly implements a live countdown timer for the season end.
 *  • It is fully decoupled from any season detection or reward logic.
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Vertex.Backend
{
    public class SeasonalView : BaseLeaderboardView
    {
        [Header("Season View UI")]
        [Tooltip("Text to display 'Season X' or similar.")]
        [SerializeField] private Text seasonLabel;
        [Tooltip("Text to display the live countdown timer.")]
        [SerializeField] private Text countdownText;

        [Header("Dependencies")]
        [Tooltip("Reference to the ScoreManager to get the local player's seasonal score.")]
        [SerializeField] private ScoreManager scoreMgr;

        // This is populated by the central SeasonManager singleton.
        private DateTime seasonEndUtc;

        /// <summary>
        /// Called when the GameObject becomes active. This is the perfect trigger
        /// to refresh the leaderboard view for the user.
        /// </summary>
        private void OnEnable()
        {
            Debug.Log("[SeasonalView] View has been enabled. Triggering a full refresh.");
            // The base class handles the entire refresh lifecycle, including showing the loading panel.
            _ = DisplayLeaderboard();
        }

        /// <summary>
        /// Called when the GameObject becomes inactive. We must stop the countdown
        /// invoke to prevent it from running in the background unnecessarily.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable(); // Call the base class's cleanup
            if (IsInvoking(nameof(UpdateCountdown)))
            {
                CancelInvoke(nameof(UpdateCountdown));
                Debug.Log("[SeasonalView] View disabled. Countdown timer stopped.");
            }
        }

        #region Base Class Overrides

        protected override string LoadingMessage => Lang == 1 ? "az bekle" : "hold on";

        /// <summary>
        /// This method is called by the base class to fetch the necessary data.
        /// It now communicates with the SeasonManager singleton to get the required info.
        /// </summary>
        protected override async Task<List<ScoreEntry>> FetchLeaderboardDataAsync()
        {
            // First, ensure the central SeasonManager is ready. This prevents race conditions at app start.
            while (SeasonManager.Instance == null || !SeasonManager.Instance.IsReady)
            {
                Debug.Log("[SeasonalView] Waiting for the central SeasonManager to become ready...");
                await Task.Delay(100);
            }

            // Get the critical data from the always-on manager.
            string seasonId = SeasonManager.Instance.CurrentSeasonId;
            this.seasonEndUtc = SeasonManager.Instance.SeasonEndUtc;

            if (string.IsNullOrEmpty(seasonId))
            {
                throw new InvalidOperationException("Could not fetch scores because SeasonManager failed to provide a valid Season ID.");
            }

            // The label can now also be set with data from the manager if you add more properties to it,
            // but for now, we can keep it simple.
            // seasonLabel.text = $"Season {SeasonManager.Instance.CurrentSeasonNumber}";

            if (seasonLabel != null)
            {
                                seasonLabel.text = Lang == 1 
                    ? $"{SeasonManager.Instance.CurrentSeasonNumber}. sezonun bitmesine" 
                    : $"Season {SeasonManager.Instance.CurrentSeasonNumber} ends in";
            }

            Debug.Log($"[SeasonalView] Fetching scores for SEASON '{seasonId}' from the server.");
            var scores = await LeaderboardService.GetSeasonScoresAsync();

            // --- COUNTDOWN TIMER ACTIVATION ---
            // Stop any previous timers and start a new one for the current season.
            if (IsInvoking(nameof(UpdateCountdown))) CancelInvoke(nameof(UpdateCountdown));
            InvokeRepeating(nameof(UpdateCountdown), 0f, 1f);

            return scores;
        }

        /// <summary>
        /// Provides the player's local score for optimistic updates on the leaderboard.
        /// </summary>
        protected override int GetLocalPlayerScore()
        {
            // Correctly returns the TotalScore for the seasonal leaderboard.
            return scoreMgr != null ? scoreMgr.TotalScore : 0;
        }
        #endregion

        #region UI Helpers

        /// <summary>
        /// Called every second by InvokeRepeating to update the countdown timer text.
        /// </summary>
        void UpdateCountdown()
        {
            if (countdownText == null) return;

            // Safety check in case the date wasn't populated correctly.
            if (seasonEndUtc == default(DateTime))
            {
                countdownText.text = "---";
                return;
            }

            TimeSpan timeRemaining = seasonEndUtc - DateTime.UtcNow;

            if (timeRemaining.TotalSeconds <= 0)
            {
                countdownText.text = Lang == 1 ? "Sezon Bitti" : "Season Ended";
                CancelInvoke(nameof(UpdateCountdown)); // Stop the timer when it reaches zero.
                return;
            }

            // Format the remaining time into a human-readable string.
            string F(int v, string u) => Lang == 1 ? $"{v} {u}" : $"{v} {u}{(v == 1 ? "" : "s")}";

            if (timeRemaining.Days >= 7) countdownText.text = F(timeRemaining.Days / 7, Lang == 1 ? "hafta" : "week");
            else if (timeRemaining.Days >= 1) countdownText.text = F(timeRemaining.Days, Lang == 1 ? "gün" : "day");
            else if (timeRemaining.Hours >= 1) countdownText.text = F(timeRemaining.Hours, Lang == 1 ? "saat" : "hour");
            else if (timeRemaining.Minutes >= 1) countdownText.text = F(timeRemaining.Minutes, Lang == 1 ? "dakika" : "minute");
            else countdownText.text = F(timeRemaining.Seconds, Lang == 1 ? "saniye" : "second");
        }
        #endregion
    }
}