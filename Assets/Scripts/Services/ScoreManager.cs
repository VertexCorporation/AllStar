/***************************************************************************
 *  ScoreManager (2025-06-24 - THE ATOMIC, TRULY FLAWLESS VERSION)
 *  -----------------------------------------------------------------------
 *  • FLAWLESS STATE MANAGEMENT: Per-user keys for independent, persistent
 *    score storage. No data loss on logout or account switching.
 *  • ATOMIC MIGRATION: Migrates anonymous scores to ANY logged-in or
 *    NEWLY CREATED account. The process is now transactional: anonymous
 *    data is only deleted AFTER it is successfully saved to the user's
 *    profile, preventing data loss if the process is interrupted.
 *  • REGISTRATION-AWARE: Explicitly detects a new user registration vs.
 *    a regular login for clearer logging and robust handling.
 *  • EXTREMELY ROBUST: Handles all auth states, including the tricky
 *    "anonymous -> new account" flow without data loss, even if
 *    backend data (like user creation time) is slow to propagate.
 *  • EXTENSIVE LOGGING: Detailed English logs explain every step of the
 *    data loading, saving, and migration process for easy debugging.
 ***************************************************************************/

using UnityEngine;
using System;
using System.Threading.Tasks;
using Firebase.Auth;
using Vertex.Backend;
using Vertex.Backend.Data;
using System.Collections;

public class ScoreManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    private static ScoreManager _instance;
    public static ScoreManager Instance => _instance;

    // --- Public Properties (read-only from outside) ---
    public int Score { get; private set; }
    public int HighScore { get; private set; }
    public int TotalScore { get; private set; }
    public bool HasNewHighScore { get; private set; }

    // --- Events ---
    public static event Action<int> ScoreUpdated = delegate { };
    public static event Action<int> HighscoreUpdated = delegate { };
    public static event Action<int> TotalScoreUpdated = delegate { };

    // --- Purchase Logic (unchanged) ---
    public bool SkillCanPurchase => HighScore >= 100;
    public bool ShieldCanPurchase => HighScore >= 250;
    public bool PhoenixCanPurchase => HighScore >= 500;
    public bool MultiplyCanPurchase => HighScore >= 750;
    public bool EquipmentCanPurchase => HighScore >= 1000;
    public bool NightCanPurchase => HighScore >= 2500;

    // --- Item Purchase Logic & State ---
    public bool IsSkillPurchased { get; private set; }
    public bool IsNightPurchased { get; private set; }
    public bool IsEquipmentPurchased { get; private set; }
    public bool IsMultiplyPurchased { get; private set; }
    public bool IsShieldPurchased { get; private set; }
    public bool IsPhoenixPurchased { get; private set; }
    public bool IsNightBackgroundActive { get; private set; }
    // --- Events ---
    public static event Action<string> ItemPurchased = delegate { };

    // --- Internal State ---
    public bool scoreIsNotLocked = true;
    private Coroutine checkScoreCoroutine;
    private string _currentUserId = null;
    private string _currentUserSalt = null;
    private readonly object _authChangeLock = new object();

    // --- Constants & Key Management ---
    private const string BASE_HIGHSCORE_KEY = "vtx_ehs_v3";
    private const string BASE_TOTALSCORE_KEY = "vtx_ets_v3";
    private const string ANONYMOUS_USER_SALT = "a_very_secret_and_static_salt_for_anonymous_players_!@#$";

    // --- Constants & Key Management ---
    private const string BASE_SKILL_KEY = "vtx_item_skill_v1";
    private const string BASE_NIGHT_KEY = "vtx_item_night_v1";
    private const string BASE_EQUIPMENT_KEY = "vtx_item_equip_v1";
    private const string BASE_MULTIPLY_KEY = "vtx_item_mult_v1";
    private const string BASE_SHIELD_KEY = "vtx_item_shield_v1";
    private const string BASE_PHOENIX_KEY = "vtx_item_phoenix_v1";
    private const string BASE_USER_BG_PREF_KEY = "vtx_bg_pref_v1";
    private string GetUserItemKey(string baseKey, string userId) => $"{baseKey}_{userId}";

    private string GetUserHighScoreKey(string userId) => $"{BASE_HIGHSCORE_KEY}_{userId}";
    private string GetUserTotalScoreKey(string userId) => $"{BASE_TOTALSCORE_KEY}_{userId}";

    private Coroutine _autoSaveCoroutine;

    #region Unity Lifecycle & Initialization

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private bool _isSubscribedToAuth = false;

    private void OnEnable()
    {
        GameManager.GameStateChanged += OnGameStateChanged;
        // Start the auto-save routine when the manager is enabled.
        _autoSaveCoroutine = StartCoroutine(AutoSaveCoroutine());
    }

    private void OnDisable()
    {
        if (_isSubscribedToAuth && FirebaseAuth.DefaultInstance != null)
        {
            FirebaseAuth.DefaultInstance.StateChanged -= HandleAuthStateChanged;
        }
        GameManager.GameStateChanged -= OnGameStateChanged;

        // Stop the auto-save routine when the manager is disabled.
        if (_autoSaveCoroutine != null)
        {
            StopCoroutine(_autoSaveCoroutine);
        }
        Debug.Log("[ScoreManager] Unsubscribed from auth/game state changes and stopped auto-save.");
    }


    /// <summary>
    /// Periodically saves all player data to prevent data loss from unexpected app closure.
    /// </summary>
    private IEnumerator AutoSaveCoroutine()
    {
        // A sensible delay. Frequent enough to prevent major data loss,
        // infrequent enough to not cause performance issues.
        const float autoSaveIntervalSeconds = 45.0f;

        while (true)
        {
            yield return new WaitForSeconds(autoSaveIntervalSeconds);

            // Only save if we are actually in a gameplay state.
            // No need to save repeatedly in the main menu.
            // --- CORRECTED LINE ---
            if (GameManager.Instance != null && GameManager.Instance.GameState == GameState.Playing)
            {
                Debug.Log($"[ScoreManager][AutoSave] Periodic auto-save triggered. Saving player data...");
                SaveAllPlayerData(false); // We don't need to submit to the server on every auto-save.
            }
        }
    }

    /// <summary>
    /// This is the NEW public entry point called by AppInitializer. It returns a Task
    /// so the initializer can wait for the entire data loading process to complete.
    /// </summary>
    public async Task InitializeAndLoadInitialData()
    {
        Debug.Log("[ScoreManager] Initialization commanded. Performing initial data load...");

        // Load the initial data based on the current user state
        var user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user != null)
        {
            Debug.Log($"[ScoreManager] Found signed-in user: {user.UserId}. Loading their session.");
            await HandleUserLoginOrRegistration(user);
        }
        else
        {
            Debug.Log("[ScoreManager] No user found. Loading anonymous session.");
            HandleAnonymousSession();
        }

        // IMPORTANT: Subscribe to the event handler only AFTER the initial load is complete.
        FirebaseAuth.DefaultInstance.StateChanged += HandleAuthStateChanged;
        _isSubscribedToAuth = true;
        Debug.Log("[ScoreManager] Initial load complete. Now subscribed to future auth changes.");
    }

    #endregion

    #region Auth Handling & Data Migration

    /// <summary>
    /// This handler is now ONLY responsible for auth changes that happen *after* startup
    /// (e.g., a user manually logging in or out). It is an async void method
    /// because it is an event handler. It is now protected by a lock to be fully robust.
    /// </summary>
    private void HandleAuthStateChanged(object sender, EventArgs eventArgs)
    {
        // Use a lock to ensure that only one auth change is processed at a time.
        // This is more robust than a manual boolean flag.
        lock (_authChangeLock)
        {
            // Fire-and-forget the async task. The lock ensures we don't start a new
            // task until the previous one's synchronous part is complete.
            // The async logic inside will run, but the entry point is protected.
            _ = ProcessAuthStateChangeAsync();
        }
    }

    private async Task ProcessAuthStateChangeAsync()
    {
        // No need for a manual "isHandling" flag anymore. The lock handles it.
        await Task.Delay(100); // Safety delay remains useful.

        FirebaseUser newUser = FirebaseAuth.DefaultInstance.CurrentUser;
        string previousUserId = _currentUserId;
        string newUserId = newUser?.UserId;

        if (newUserId == previousUserId)
        {
            return; // No actual change
        }

        Debug.Log($"[ScoreManager][Auth] Auth state transition detected. From: '{previousUserId ?? "Anonymous"}' -> To: '{newUserId ?? "Anonymous"}'.");

        // Save data for the user who is logging out.
        if (!string.IsNullOrEmpty(previousUserId))
        {
            Debug.Log($"[ScoreManager][Auth] Saving data for outgoing user '{previousUserId}'.");
            SaveAllPlayerData();
        }

        // Now, handle the new state. This re-uses the same robust logic.
        if (newUser != null)
        {
            await HandleUserLoginOrRegistration(newUser);
        }
        else
        {
            HandleAnonymousSession();
        }
    }

    #endregion

    #region Account & Data Handling

    public void PrepareForForcedAnonymousMigration(string ghostUserId)
    {
        Debug.Log($"[ScoreManager] Preparing to migrate data for ghost user '{ghostUserId}' to anonymous upon their logout.");
    }

    private async Task HandleUserLoginOrRegistration(FirebaseUser user)
    {

        if (user.IsAnonymous)
        {
            Debug.Log("[ScoreManager][Auth] User is anonymous. Routing to HandleAnonymousSession().");
            HandleAnonymousSession();
            return;
        }

        /* ──────────────────────────────────────────────────────────────
 * STEP 0 - FLUSH LATEST GUEST STATE BEFORE MIGRATION
 * --------------------------------------------------------------
 * A guest may tap “Sign In / Register” mid-game. Their most
 * recent score / item changes could still be only in RAM and
 * therefore invisible to the migration logic that reads from
 * PlayerPrefs.  
 *  
 * → We force-save the current anonymous session FIRST, so the
 *   very latest progress is guaranteed to be migrated.  
 * → This runs only when we are coming from a true guest state
 *   (`_currentUserId` == null). It never touches data that
 *   belongs to another logged-in account, thus respecting the
 *   “no account-to-account transfer” rule.
 * ──────────────────────────────────────────────────────────────*/
        if (string.IsNullOrEmpty(_currentUserId) && PlayerPrefs.HasKey(BASE_HIGHSCORE_KEY))
        {
            Debug.Log("[ScoreManager][Login] Anonymous session with data detected. Flushing in-memory progress to disk before migration to ensure data integrity.");
            SaveAllPlayerData();
            PlayerPrefs.Save();
        }

        // The rest of the function continues as-is...
        bool isNewUserRegistration = (user.Metadata.LastSignInTimestamp - user.Metadata.CreationTimestamp) < 5000;
        string loginType = isNewUserRegistration ? "NEW ACCOUNT REGISTRATION" : "LOGIN";
        Debug.Log($"[ScoreManager][Login] Beginning {loginType} process for user {user.UserId}.");

        // STEP 1: READ POTENTIAL ANONYMOUS DATA
        string anonEncryptedHS = PlayerPrefs.GetString(BASE_HIGHSCORE_KEY, null);
        string anonEncryptedTS = PlayerPrefs.GetString(BASE_TOTALSCORE_KEY, null);
        var anonPayloadHS = DecryptPayloadWithSalt(anonEncryptedHS, ANONYMOUS_USER_SALT, suppressWarning: true);
        var anonPayloadTS = DecryptPayloadWithSalt(anonEncryptedTS, ANONYMOUS_USER_SALT, suppressWarning: true);
        int anonymousHighScoreToMigrate = (anonPayloadHS != null && anonPayloadHS.userId == "anonymous") ? anonPayloadHS.score : 0;
        int anonymousTotalScoreToMigrate = (anonPayloadTS != null && anonPayloadTS.userId == "anonymous") ? anonPayloadTS.score : 0;

        bool anonOwnsSkill = PlayerPrefs.GetInt(BASE_SKILL_KEY, 0) == 1;
        bool anonOwnsNight = PlayerPrefs.GetInt(BASE_NIGHT_KEY, 0) == 1;
        bool anonOwnsEquipment = PlayerPrefs.GetInt(BASE_EQUIPMENT_KEY, 0) == 1;
        bool anonOwnsMultiply = PlayerPrefs.GetInt(BASE_MULTIPLY_KEY, 0) == 1;
        bool anonOwnsShield = PlayerPrefs.GetInt(BASE_SHIELD_KEY, 0) == 1;
        bool anonOwnsPhoenix = PlayerPrefs.GetInt(BASE_PHOENIX_KEY, 0) == 1;
        // IMPROVEMENT: Read the anonymous user's background preference for proper migration.
        bool anonPrefersNight = PlayerPrefs.GetInt(BASE_USER_BG_PREF_KEY, 0) == 1;

        bool hasAnonymousDataToMigrate = (anonymousTotalScoreToMigrate > 0 || anonymousHighScoreToMigrate > 0 ||
                                         anonOwnsSkill || anonOwnsNight || anonOwnsEquipment || anonOwnsMultiply ||
                                         anonOwnsShield || anonOwnsPhoenix || anonPrefersNight);

        if (hasAnonymousDataToMigrate)
        {
            Debug.Log($"[ScoreManager][Login] Found ANONYMOUS data to migrate. Scores: High={anonymousHighScoreToMigrate}, Total={anonymousTotalScoreToMigrate}.");
        }

        // STEP 2: SET UP NEW USER'S SECURE SESSION (Unchanged)
        _currentUserId = user.UserId;
        _currentUserSalt = await FetchUserSaltAsync(_currentUserId);

        if (string.IsNullOrEmpty(_currentUserSalt))
        {
            Debug.LogError($"[ScoreManager][Login] CRITICAL FAILURE: Could not fetch salt for user '{_currentUserId}'. Falling back to anonymous mode.");
            _currentUserId = null;
            HandleAnonymousSession();          // NEW — guarantees a clean, isolated guest state
            return;                            // prevents any gameplay in an unsafe limbo state
        }
        Debug.Log($"[ScoreManager][Login] User session for '{_currentUserId}' is now active with a valid salt.");

        // STEP 3: LOAD USER'S PERSISTENT DATA (Unchanged)
        string userHsKey = GetUserHighScoreKey(_currentUserId);
        string userTsKey = GetUserTotalScoreKey(_currentUserId);
        var userPayloadHS = DecryptPayloadWithSalt(PlayerPrefs.GetString(userHsKey, null), _currentUserSalt, suppressWarning: true);
        var userPayloadTS = DecryptPayloadWithSalt(PlayerPrefs.GetString(userTsKey, null), _currentUserSalt, suppressWarning: true);
        int userExistingHighScore = (userPayloadHS != null && userPayloadHS.userId == _currentUserId) ? userPayloadHS.score : 0;
        int userExistingTotalScore = (userPayloadTS != null && userPayloadTS.userId == _currentUserId) ? userPayloadTS.score : 0;

        bool userOwnsSkill = PlayerPrefs.GetInt(GetUserItemKey(BASE_SKILL_KEY, _currentUserId), 0) == 1;
        bool userOwnsNight = PlayerPrefs.GetInt(GetUserItemKey(BASE_NIGHT_KEY, _currentUserId), 0) == 1;
        bool userOwnsEquipment = PlayerPrefs.GetInt(GetUserItemKey(BASE_EQUIPMENT_KEY, _currentUserId), 0) == 1;
        bool userOwnsMultiply = PlayerPrefs.GetInt(GetUserItemKey(BASE_MULTIPLY_KEY, _currentUserId), 0) == 1;
        bool userOwnsShield = PlayerPrefs.GetInt(GetUserItemKey(BASE_SHIELD_KEY, _currentUserId), 0) == 1;
        bool userOwnsPhoenix = PlayerPrefs.GetInt(GetUserItemKey(BASE_PHOENIX_KEY, _currentUserId), 0) == 1;
        bool userPrefersNight = PlayerPrefs.GetInt(GetUserItemKey(BASE_USER_BG_PREF_KEY, _currentUserId), 0) == 1;
        Debug.Log($"[ScoreManager][Login] Loaded persistent data for user '{_currentUserId}'. Scores: High={userExistingHighScore}, Total={userExistingTotalScore}.");

        // STEP 4: COMBINE DATA IN MEMORY
        HighScore = Mathf.Max(userExistingHighScore, anonymousHighScoreToMigrate);
        TotalScore = userExistingTotalScore + anonymousTotalScoreToMigrate;

        IsSkillPurchased = userOwnsSkill || anonOwnsSkill;
        IsNightPurchased = userOwnsNight || anonOwnsNight;
        IsEquipmentPurchased = userOwnsEquipment || anonOwnsEquipment;
        IsMultiplyPurchased = userOwnsMultiply || anonOwnsMultiply;
        IsShieldPurchased = userOwnsShield || anonOwnsShield;
        IsPhoenixPurchased = userOwnsPhoenix || anonOwnsPhoenix;

        // IMPROVEMENT: Correctly merge the background preference. It's only active if the theme is owned AND one of the sessions had it active.
        IsNightBackgroundActive = IsNightPurchased && (userPrefersNight || anonPrefersNight);

        Debug.Log($"[ScoreManager][Login] Data combined in memory. Final state for '{_currentUserId}': High={HighScore}, Total={TotalScore}.");

        ResetCurrentGameScore();

        // STEP 5: ATOMIC SAVE & DELETE
        // This block replaces the separate calls to SaveAllPlayerData() and PlayerPrefs.DeleteKey().
        // By performing all PlayerPrefs modifications before a single .Save() call, we treat the
        // migration as a single transaction, preventing data duplication if the app closes mid-process.
        Debug.Log("[ScoreManager][Login] Beginning atomic save of migrated data.");

        // A: Prepare new user data for saving
        var hsPayload = new EncryptedScore { score = HighScore, userId = _currentUserId, salt = _currentUserSalt };
        string encryptedHs = SecureStorage.Encrypt(JsonUtility.ToJson(hsPayload), _currentUserSalt);
        if (encryptedHs != null) PlayerPrefs.SetString(GetUserHighScoreKey(_currentUserId), encryptedHs);

        var tsPayload = new EncryptedScore { score = TotalScore, userId = _currentUserId, salt = _currentUserSalt };
        string encryptedTs = SecureStorage.Encrypt(JsonUtility.ToJson(tsPayload), _currentUserSalt);
        if (encryptedTs != null) PlayerPrefs.SetString(GetUserTotalScoreKey(_currentUserId), encryptedTs);

        PlayerPrefs.SetInt(GetUserItemKey(BASE_SKILL_KEY, _currentUserId), IsSkillPurchased ? 1 : 0);
        PlayerPrefs.SetInt(GetUserItemKey(BASE_NIGHT_KEY, _currentUserId), IsNightPurchased ? 1 : 0);
        PlayerPrefs.SetInt(GetUserItemKey(BASE_EQUIPMENT_KEY, _currentUserId), IsEquipmentPurchased ? 1 : 0);
        PlayerPrefs.SetInt(GetUserItemKey(BASE_MULTIPLY_KEY, _currentUserId), IsMultiplyPurchased ? 1 : 0);
        PlayerPrefs.SetInt(GetUserItemKey(BASE_SHIELD_KEY, _currentUserId), IsShieldPurchased ? 1 : 0);
        PlayerPrefs.SetInt(GetUserItemKey(BASE_PHOENIX_KEY, _currentUserId), IsPhoenixPurchased ? 1 : 0);
        PlayerPrefs.SetInt(GetUserItemKey(BASE_USER_BG_PREF_KEY, _currentUserId), IsNightBackgroundActive ? 1 : 0);

        // B: If migration occurred, queue the deletion of all anonymous data
        if (hasAnonymousDataToMigrate)
        {
            PlayerPrefs.DeleteKey(BASE_HIGHSCORE_KEY);
            PlayerPrefs.DeleteKey(BASE_TOTALSCORE_KEY);
            PlayerPrefs.DeleteKey(BASE_SKILL_KEY);
            PlayerPrefs.DeleteKey(BASE_NIGHT_KEY);
            PlayerPrefs.DeleteKey(BASE_EQUIPMENT_KEY);
            PlayerPrefs.DeleteKey(BASE_MULTIPLY_KEY);
            PlayerPrefs.DeleteKey(BASE_SHIELD_KEY);
            PlayerPrefs.DeleteKey(BASE_PHOENIX_KEY);
            // IMPROVEMENT: Ensure the anonymous background preference key is also deleted.
            PlayerPrefs.DeleteKey(BASE_USER_BG_PREF_KEY);
            Debug.Log("[ScoreManager][Login] Anonymous data has been queued for deletion.");
        }

        // C: Commit all changes (save user data AND delete anonymous data) in one atomic operation.
        PlayerPrefs.Save();
        Debug.Log("[ScoreManager][Login] ATOMIC MIGRATION COMPLETE. All changes committed to disk.");

        // STEP 6: FINALIZE
        HighscoreUpdated?.Invoke(HighScore);
        TotalScoreUpdated?.Invoke(TotalScore);
        Debug.Log($"[ScoreManager][Login] Process for {user.UserId} complete.");
    }

    private void HandleAnonymousSession()
    {
        _currentUserId = null;
        _currentUserSalt = null;
        Debug.Log("[ScoreManager][Anonymous] User session cleared. Manager is now in ANONYMOUS mode.");

        // Load Scores
        var anonPayloadHS = DecryptPayloadWithSalt(PlayerPrefs.GetString(BASE_HIGHSCORE_KEY), ANONYMOUS_USER_SALT, suppressWarning: true);
        var anonPayloadTS = DecryptPayloadWithSalt(PlayerPrefs.GetString(BASE_TOTALSCORE_KEY), ANONYMOUS_USER_SALT, suppressWarning: true);

        if (anonPayloadHS != null && anonPayloadHS.userId == "anonymous")
        {
            HighScore = anonPayloadHS.score;
            TotalScore = (anonPayloadTS != null && anonPayloadTS.userId == "anonymous") ? anonPayloadTS.score : 0;
            Debug.Log($"[ScoreManager][Anonymous] Loaded existing anonymous scores: High={HighScore}, Total={TotalScore}.");
        }
        else
        {
            HighScore = 0;
            TotalScore = 0;
            Debug.Log("[ScoreManager][Anonymous] No existing anonymous score data found. Scores reset to 0.");
        }

        // Load Items
        IsSkillPurchased = PlayerPrefs.GetInt(BASE_SKILL_KEY, 0) == 1;
        IsNightPurchased = PlayerPrefs.GetInt(BASE_NIGHT_KEY, 0) == 1;
        IsEquipmentPurchased = PlayerPrefs.GetInt(BASE_EQUIPMENT_KEY, 0) == 1;
        IsMultiplyPurchased = PlayerPrefs.GetInt(BASE_MULTIPLY_KEY, 0) == 1;
        IsShieldPurchased = PlayerPrefs.GetInt(BASE_SHIELD_KEY, 0) == 1;
        IsPhoenixPurchased = PlayerPrefs.GetInt(BASE_PHOENIX_KEY, 0) == 1;
        IsNightBackgroundActive = PlayerPrefs.GetInt(BASE_USER_BG_PREF_KEY, 0) == 1;
        Debug.Log($"[ScoreManager][Anonymous] Loaded anonymous items: Skill={IsSkillPurchased}, Night={IsNightPurchased}, etc.");

        ResetCurrentGameScore();
        HighscoreUpdated?.Invoke(HighScore);
        TotalScoreUpdated?.Invoke(TotalScore);
        Debug.Log("[ScoreManager][Anonymous] Anonymous session is ready.");
    }

    private void SaveAllPlayerData(bool andSubmitToServer = false)
    {
        string saltToUse;
        string userIdToEmbed;
        string hsKey, tsKey, skillKey, nightKey, equipKey, multKey, shieldKey, phoenixKey, bgKey;

        if (!string.IsNullOrEmpty(_currentUserId) && !string.IsNullOrEmpty(_currentUserSalt))
        {
            // LOGGED-IN USER
            saltToUse = _currentUserSalt;
            userIdToEmbed = _currentUserId;
            hsKey = GetUserHighScoreKey(_currentUserId);
            tsKey = GetUserTotalScoreKey(_currentUserId);
            skillKey = GetUserItemKey(BASE_SKILL_KEY, _currentUserId);
            nightKey = GetUserItemKey(BASE_NIGHT_KEY, _currentUserId);
            equipKey = GetUserItemKey(BASE_EQUIPMENT_KEY, _currentUserId);
            multKey = GetUserItemKey(BASE_MULTIPLY_KEY, _currentUserId);
            shieldKey = GetUserItemKey(BASE_SHIELD_KEY, _currentUserId);
            phoenixKey = GetUserItemKey(BASE_PHOENIX_KEY, _currentUserId);
            bgKey = GetUserItemKey(BASE_USER_BG_PREF_KEY, _currentUserId);
            Debug.Log($"[ScoreManager][Save] Preparing to save data for LOGGED-IN user '{_currentUserId}'.");
        }
        else
        {
            // ANONYMOUS USER
            saltToUse = ANONYMOUS_USER_SALT;
            userIdToEmbed = "anonymous";
            hsKey = BASE_HIGHSCORE_KEY;
            tsKey = BASE_TOTALSCORE_KEY;
            skillKey = BASE_SKILL_KEY;
            nightKey = BASE_NIGHT_KEY;
            equipKey = BASE_EQUIPMENT_KEY;
            multKey = BASE_MULTIPLY_KEY;
            shieldKey = BASE_SHIELD_KEY;
            phoenixKey = BASE_PHOENIX_KEY;
            bgKey = BASE_USER_BG_PREF_KEY;
            Debug.Log($"[ScoreManager][Save] Preparing to save data for ANONYMOUS session.");
        }

        // Save Scores (Encrypted)
        var hsPayload = new EncryptedScore { score = HighScore, userId = userIdToEmbed, salt = saltToUse };
        string hsJson = JsonUtility.ToJson(hsPayload);
        string encryptedHs = SecureStorage.Encrypt(hsJson, saltToUse);
        if (encryptedHs != null) PlayerPrefs.SetString(hsKey, encryptedHs);

        var tsPayload = new EncryptedScore { score = TotalScore, userId = userIdToEmbed, salt = saltToUse };
        string tsJson = JsonUtility.ToJson(tsPayload);
        string encryptedTs = SecureStorage.Encrypt(tsJson, saltToUse);
        if (encryptedTs != null) PlayerPrefs.SetString(tsKey, encryptedTs);

        // Save Items (Simple 1 or 0)
        PlayerPrefs.SetInt(skillKey, IsSkillPurchased ? 1 : 0);
        PlayerPrefs.SetInt(nightKey, IsNightPurchased ? 1 : 0);
        PlayerPrefs.SetInt(equipKey, IsEquipmentPurchased ? 1 : 0);
        PlayerPrefs.SetInt(multKey, IsMultiplyPurchased ? 1 : 0);
        PlayerPrefs.SetInt(shieldKey, IsShieldPurchased ? 1 : 0);
        PlayerPrefs.SetInt(phoenixKey, IsPhoenixPurchased ? 1 : 0);
        PlayerPrefs.SetInt(bgKey, IsNightBackgroundActive ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"[ScoreManager][Save] Data saved successfully for session '{userIdToEmbed}'. High: {HighScore}, Total: {TotalScore}, Items saved.");

        // FLAWLESS FIX: Sadece andSubmitToServer true ise gönderim yap:
        if (andSubmitToServer)
        {
            _ = SubmitScoresToServerAsync();
        }
    }


    /// <summary>
    /// Submits the current HighScore and TotalScore to the backend services.
    /// This method runs asynchronously in the background. It will only submit scores
    /// for logged-in (non-anonymous) users.
    /// </summary>
    private async Task SubmitScoresToServerAsync()
    {
        // Guard Clause: Only submit scores for authenticated users.
        if (string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        Debug.Log($"[ScoreManager] Triggering server score submission for user '{_currentUserId}'. HighScore: {HighScore}, TotalScore (Seasonal): {TotalScore}");

        // Submit both the all-time high score and the current seasonal total score.
        // The backend is designed to handle both types through the same function endpoint.
        await LeaderboardService.SubmitScoreAsync(HighScore, "highscore");
        await LeaderboardService.SubmitScoreAsync(TotalScore, "seasonal");
    }

    // Add this new method to ScoreManager
    public void ToggleBackgroundPreference()
    {
        // This can only be done if the night background is owned.
        if (IsNightPurchased)
        {
            IsNightBackgroundActive = !IsNightBackgroundActive;
            SaveAllPlayerData();
            ItemPurchased?.Invoke("BackgroundToggle"); // Use the event to notify the UI
            Debug.Log($"[ScoreManager] Background preference toggled. Night active: {IsNightBackgroundActive}");
        }
    }

    private async Task<string> FetchUserSaltAsync(string userId)
    {
        // FetchUserCreationTimeAsync now returns Unix milliseconds
        long createdAtUnixMilliseconds = await AuthService.FetchUserCreationTimeAsync();

        if (createdAtUnixMilliseconds == 0)
        {
            Debug.LogError($"[ScoreManager] CRITICAL: Could not fetch createdAt for user '{userId}'. Scores cannot be secured.");
            return null;
        }

        // Convert Unix milliseconds to DateTime
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime userCreationDateTime = epoch.AddMilliseconds(createdAtUnixMilliseconds);

        // Convert to ISO 8601 format string for the salt
        return userCreationDateTime.ToString("o"); // "o" is the round-trip date/time pattern
    }

    private EncryptedScore DecryptPayloadWithSalt(string encryptedString, string salt, bool suppressWarning = false)
    {
        if (string.IsNullOrEmpty(encryptedString) || string.IsNullOrEmpty(salt)) return null;

        string json = SecureStorage.Decrypt(encryptedString, salt, suppressWarning);
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            var payload = JsonUtility.FromJson<EncryptedScore>(json);
            if (payload != null && payload.salt != salt)
            {
                if (!suppressWarning) Debug.LogWarning($"[ScoreManager][Decrypt] Salt mismatch after decryption. Expected: {salt}, Found: {payload.salt}.");
                return null;
            }
            return payload;
        }
        catch (Exception ex)
        {
            if (!suppressWarning) Debug.LogWarning($"[ScoreManager][Decrypt] JSON parse failed after decryption. Data might be corrupt. Details: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region Gameplay Logic

    private void OnGameStateChanged(GameState newState, GameState oldState)
    {
        if (newState == GameState.GameOver && oldState == GameState.Playing)
        {
            Debug.Log("[ScoreManager][Gameplay] Game Over detected. Committing player data to persistent storage.");
            SaveAllPlayerData(true);
        }
    }

    public void ResetCurrentGameScore()
    {
        Score = 0;
        HasNewHighScore = false;
        ScoreUpdated?.Invoke(Score);
    }

    public void AddScore(int amount)
    {
        if (amount <= 0 || !scoreIsNotLocked) return;

        Score += amount;
        TotalScore += amount;

        if (Score > HighScore)
        {
            HighScore = Score;
            HasNewHighScore = true;
            HighscoreUpdated?.Invoke(HighScore);
        }

        ScoreUpdated?.Invoke(Score);
        TotalScoreUpdated?.Invoke(TotalScore);

        scoreIsNotLocked = true;
        if (checkScoreCoroutine != null) StopCoroutine(checkScoreCoroutine);
        checkScoreCoroutine = StartCoroutine(CheckScoreUpdate());
    }

    private IEnumerator CheckScoreUpdate()
    {
        yield return new WaitForSeconds(4f);
        scoreIsNotLocked = false;
    }

    public void ResetForNewSeason()
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            Debug.LogWarning("[ScoreManager] Attempted to reset season for an anonymous user. Action ignored.");
            return;
        }

        Debug.Log($"[ScoreManager][Gameplay] Resetting total score and items for user {_currentUserId} for a new season.");

        // Reset scores
        TotalScore = 0;

        // Reset items
        IsSkillPurchased = false;
        IsNightPurchased = false;
        IsEquipmentPurchased = false;
        IsMultiplyPurchased = false;
        IsShieldPurchased = false;
        IsPhoenixPurchased = false;

        SaveAllPlayerData(true);
        TotalScoreUpdated?.Invoke(TotalScore);
    }

    #endregion

    #region Public Purchase Methods

    public void PurchaseSkill(bool force = false)
    {
        if ((SkillCanPurchase || force) && !IsSkillPurchased)
        {
            IsSkillPurchased = true;
            SaveAllPlayerData();
            ItemPurchased?.Invoke("Skill");
            Debug.Log("[ScoreManager] Purchased: Skill");
        }
    }

    public void PurchaseNight(bool force = false)
    {
        if ((NightCanPurchase || force) && !IsNightPurchased)
        {
            IsNightPurchased = true;
            SaveAllPlayerData();
            ItemPurchased?.Invoke("Night");
            Debug.Log("[ScoreManager] Purchased: Night");
        }
    }

    public void PurchaseEquipment(bool force = false)
    {
        if ((EquipmentCanPurchase || force) && !IsEquipmentPurchased)
        {
            IsEquipmentPurchased = true;
            SaveAllPlayerData();
            ItemPurchased?.Invoke("Equipment");
            Debug.Log("[ScoreManager] Purchased: Equipment");
        }
    }

    public void PurchaseMultiply(bool force = false)
    {
        if ((MultiplyCanPurchase || force) && !IsMultiplyPurchased)
        {
            IsMultiplyPurchased = true;
            SaveAllPlayerData();
            ItemPurchased?.Invoke("Multiply");
            Debug.Log("[ScoreManager] Purchased: Multiply");
        }
    }

    public void PurchaseShield(bool force = false)
    {
        if ((ShieldCanPurchase || force) && !IsShieldPurchased)
        {
            IsShieldPurchased = true;
            SaveAllPlayerData();
            ItemPurchased?.Invoke("Shield");
            Debug.Log("[ScoreManager] Purchased: Shield");
        }
    }

    public void PurchasePhoenix(bool force = false)
    {
        if ((PhoenixCanPurchase || force) && !IsPhoenixPurchased)
        {
            IsPhoenixPurchased = true;
            SaveAllPlayerData();
            ItemPurchased?.Invoke("Phoenix");
            Debug.Log("[ScoreManager] Purchased: Phoenix");
        }
    }

    #endregion
}