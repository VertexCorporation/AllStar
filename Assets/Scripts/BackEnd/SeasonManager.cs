/***************************************************************************
 *  SeasonManager.cs (THE BRAIN - PERSISTENT SINGLETON - FINAL)
 *  -----------------------------------------------------------------------
 *  • This is a pure data manager that lives on an always-active GameObject.
 *  • It is a Singleton to be easily accessible by any View.
 *  • Its SOLE RESPONSIBILITY is to determine the current season, check for
 *    rewards at startup, and hold the active season's critical data.
 *  • It does NOT handle any UI drawing.
 ***************************************************************************/

using System;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

namespace Vertex.Backend
{
    public class SeasonManager : MonoBehaviour
    {
        // --- Singleton Pattern ---
        public static SeasonManager Instance { get; private set; }

        [Header("Dependencies")]
        [SerializeField] private ScoreManager scoreMgr;
        [SerializeField] private UIManager uiManager; // Needed to show the reward panel

        // --- PUBLIC PROPERTIES FOR VIEWS ---
        // Views can access these read-only properties once IsReady is true.
        public string CurrentSeasonId { get; private set; }
        public DateTime SeasonEndUtc { get; private set; }   // <<< *** THIS IS THE CRITICAL ADDITION ***
        public int CurrentSeasonNumber { get; private set; } // <<< Nice-to-have bonus property
        public bool IsReady { get; private set; } = false;

        private FirebaseFirestore _db;
        private FirebaseFirestore db
        {
            get
            {
                if (_db == null)
                {
                    _db = FirebaseFirestore.DefaultInstance;
                }
                return _db;
            }
        }

        private const string LAST_KNOWN_SEASON_ID_PREF_KEY = "LastKnownSeasonId";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private async void Start()
        {
            Debug.Log("[SeasonManager] Singleton is awake. Starting season configuration process...");
            await LoadCurrentSeasonConfigAsync();
            IsReady = true;
            Debug.Log($"[SeasonManager] Configuration complete. Current Season is #{CurrentSeasonNumber} (ID: {CurrentSeasonId})");
        }

        private async Task LoadCurrentSeasonConfigAsync()
        {
            if (scoreMgr == null || uiManager == null)
            {
                Debug.LogError("[SeasonManager] FATAL: Dependencies (ScoreManager, UIManager) are not assigned in the Inspector!");
                return;
            }

            Debug.Log("[SeasonManager] Loading current season configuration from server...");
            QuerySnapshot snap = await db.Collection("seasons")
                                             .OrderByDescending("seasonNumber").Limit(1).GetSnapshotAsync();
            if (snap.Count == 0)
            {
                Debug.LogError("[SeasonManager] CRITICAL: No seasons found in the database. Leaderboard will not function.");
                return;
            }

            DocumentSnapshot seasonDoc = snap.Documents.First();
            string newSeasonId = seasonDoc.Id;
            string lastKnownSeasonId = PlayerPrefs.GetString(LAST_KNOWN_SEASON_ID_PREF_KEY, string.Empty);

            if (!string.IsNullOrEmpty(lastKnownSeasonId) && newSeasonId != lastKnownSeasonId)
            {
                Debug.Log($"[SeasonManager] NEW SEASON DETECTED! Old: {lastKnownSeasonId}, New: {newSeasonId}. Resetting local data and checking for rewards.");
                scoreMgr.ResetForNewSeason();

                if (AuthService.IsSignedIn)
                {
                    int rewardLevel = await AuthService.FetchAndConsumeRewardLevelAsync();
                    if (rewardLevel > 0)
                    {
                        Debug.Log($"[SeasonManager] Handing off reward level {rewardLevel} to UIManager.");
                        uiManager.ShowSeasonRewardPanel(rewardLevel);
                    }
                }
            }
            
            // --- Finalize Setup: Populate public properties ---
            PlayerPrefs.SetString(LAST_KNOWN_SEASON_ID_PREF_KEY, newSeasonId);
            PlayerPrefs.Save();
            
            CurrentSeasonId = newSeasonId;

            // --- *** THIS BLOCK POPULATES THE NEW PROPERTIES *** ---
            CurrentSeasonNumber = seasonDoc.GetValue<int>("seasonNumber");
            Timestamp startTs = seasonDoc.GetValue<Timestamp>("seasonStart");
            int lengthSec = seasonDoc.GetValue<int>("seasonLengthSec");
            SeasonEndUtc = startTs.ToDateTime().ToUniversalTime().AddSeconds(lengthSec);
        }
    }
}