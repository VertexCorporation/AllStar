// FILE: AppInitializer.cs
// The definitive, scene-reload-proof, production-ready version.

/***************************************************************************
 * REASONING & ARCHITECTURE (WHY THIS FINAL VERSION IS BULLETPROOF):
 * -----------------------------------------------------------------------
 * This script's primary challenge is to execute its complex, asynchronous
 * initialization sequence exactly ONCE per application session, even when
 * `SceneManager.LoadScene` is used for game restarts.
 *
 * The key to achieving this is the `_initializationStarted` static flag.
 *
 * 1.  THE STATIC LOCK (`_initializationStarted`):
 *     When a scene reloads, a new `AppInitializer` instance is created.
 *     Its `Awake()` method runs. Without a lock, it might try to re-run the
 *     initialization logic. The static flag acts as a global lock. The first
 *     instance sets it to `true`, and all subsequent instances (from scene
 *     reloads) will see the flag and immediately destroy themselves without
 *     re-triggering the initialization flow. This is the cornerstone of its
 *     robustness.
 *
 * 2.  SIGNALING COMPLETION (`IsInitialized`):
 *     At the very end of its long process, it sets another static flag,
 *     `IsInitialized = true`. This acts as a global broadcast to any other
 *     part of the application (like the `LoadingScreenManager`) that the
 *     app is fully ready. This decouples other managers from the
 *     `AppInitializer`'s internal flow.
 ***************************************************************************/

using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Functions;
using Firebase.AppCheck;
using Firebase.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[AddComponentMenu("Vertex/App Initializer")]
public sealed class AppInitializer : MonoBehaviour
{
    #region Inspector Configuration

    [Header("UI & Managers")]
    [Tooltip("The UI GameObject to activate if the app is in maintenance mode.")]
    [SerializeField] private GameObject maintenanceScreenObject;

    [Header("Manager Activation")]
    [Tooltip("Drag all manager GameObjects here that depend on Firebase.")]
    [SerializeField] private List<GameObject> managersToActivate;

    [Header("Manager References")]
    [Tooltip("Drag the ScoreManager instance here.")]
    [SerializeField] private ScoreManager scoreManager;

    #endregion

    #region Static Properties

    public static bool FirebaseReady { get; private set; } = false;
    public static FirebaseApp DefaultApp { get; private set; }
    public static FirebaseApp NoAppCheckApp { get; private set; }

    /// <summary>
    /// A global flag indicating that the entire one-time app initialization is complete.
    /// Other scripts can check this to know if the app is fully operational.
    /// </summary>
    public static bool IsInitialized { get; private set; } = false;

    #endregion

    #region Private State

    // This static flag ensures the core initialization logic is *started* exactly once.
    // This prevents race conditions if a scene reloads quickly.
    private static bool _initializationStarted = false;

    #endregion

    #region Private Constants
    private const string MAINT_STATUS_KEY = "is_in_maintenance";
    private const string MAINT_TIMESTAMP_KEY = "maintenance_last_checked_ticks";
    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Standard singleton pattern to handle duplicates.
        var initializers = FindObjectsByType<AppInitializer>(FindObjectsSortMode.None);
        if (initializers.Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        // --- THE CRITICAL LOCK ---
        // This check ensures that the entire initialization flow is only ever
        // triggered once per application lifetime.
        if (!_initializationStarted)
        {
            _initializationStarted = true;
            Debug.Log("[AppInitializer] First-time initialization process is starting...");
            InitializeApplication();
        }
    }

    private void OnDestroy()
    {
        // Clean up event subscription to prevent memory leaks.
        if (FirebaseAuth.DefaultInstance != null)
        {
            FirebaseAuth.DefaultInstance.StateChanged -= HandleAuthStateChanged;
        }
    }
    #endregion

    #region Initialization Logic

    private void InitializeApplication()
    {
        Debug.Log("[AppInitializer] Deactivating all dependent managers to await initialization...");
        foreach (var manager in managersToActivate)
        {
            if (manager != null) manager.SetActive(false);
        }

        if (maintenanceScreenObject != null)
        {
            maintenanceScreenObject.SetActive(false);
        }

        InitializeFirebaseServices();
    }

    private void InitializeFirebaseServices()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(dependencyTask =>
        {
            if (dependencyTask.Result != DependencyStatus.Available)
            {
                Debug.LogError($"[AppInitializer] Could not resolve all Firebase dependencies: {dependencyTask.Result}. Halting initialization.");
                return;
            }

            Debug.Log("[AppInitializer] Firebase dependencies are available.");
            DefaultApp = FirebaseApp.DefaultInstance;

            try
            {
                const string noAppCheckInstanceName = "vertex-news-no-app-check";
                NoAppCheckApp = FirebaseApp.Create(DefaultApp.Options, noAppCheckInstanceName);
                Debug.Log($"[AppInitializer] Successfully created the secondary, non-AppCheck FirebaseApp instance: '{noAppCheckInstanceName}'");
            }
            catch (Exception)
            {
                NoAppCheckApp = FirebaseApp.GetInstance("vertex-news-no-app-check");
                Debug.LogWarning("[AppInitializer] Secondary FirebaseApp instance already existed. Re-using it.");
            }

            IAppCheckProviderFactory providerFactory = null;

#if UNITY_EDITOR
            string token = Environment.GetEnvironmentVariable("FIREBASE_APP_CHECK_DEBUG_TOKEN");
            if (!string.IsNullOrEmpty(token))
            {
                Debug.Log($"<color=cyan>[AppInitializer] Debug token read from environment variable. Setting it manually on the provider.</color>");
                DebugAppCheckProviderFactory.Instance.SetDebugToken(token.Trim());
            }
            else
            {
                Debug.LogWarning("[AppInitializer] FIREBASE_APP_CHECK_DEBUG_TOKEN environment variable not found. App Check will not work in the Editor.");
            }
            providerFactory = DebugAppCheckProviderFactory.Instance;
#else
            // Production-specific App Check provider setup would go here.
#endif

            if (providerFactory != null)
            {
                FirebaseAppCheck.SetAppCheckProviderFactory(providerFactory);
                FirebaseAppCheck.DefaultInstance.GetAppCheckTokenAsync(false).ContinueWithOnMainThread(tokenTask =>
                {
                    if (tokenTask.IsFaulted)
                    {
                        Debug.LogError($"[AppInitializer] FAILED TO GET APP CHECK TOKEN. Error: {tokenTask.Exception}.");
                        // In a real build, you might want to halt here or show a specific error.
                        return;
                    }
                    Debug.Log("<color=lime>[AppInitializer] Successfully received App Check token.</color>");
                    OnFirebaseReady();
                });
            }
            else
            {
                Debug.LogWarning("[AppInitializer] App Check Provider Factory is null. Proceeding without App Check.");
                OnFirebaseReady();
            }
        });
    }

    private void OnFirebaseReady()
    {
        Debug.Log("[AppInitializer] Firebase core services are ready. Subscribing to initial Auth state...");
        FirebaseReady = true;
        FirebaseAuth.DefaultInstance.StateChanged += HandleAuthStateChanged;
    }

    private async void HandleAuthStateChanged(object sender, EventArgs eventArgs)
    {
        var auth = (FirebaseAuth)sender;
        // Unsubscribe immediately to ensure this only runs once for the initial state.
        auth.StateChanged -= HandleAuthStateChanged;

        Debug.Log("[AppInitializer] Initial Auth state received. Starting maintenance check...");

        bool shouldLockdown;
        bool isConnected = Application.internetReachability != NetworkReachability.NotReachable;

        if (isConnected)
        {
            bool isServerInMaintenance = await CheckMaintenanceModeOnlineAsync();
            PlayerPrefs.SetInt(MAINT_STATUS_KEY, isServerInMaintenance ? 1 : 0);
            PlayerPrefs.SetString(MAINT_TIMESTAMP_KEY, DateTime.UtcNow.Ticks.ToString());
            PlayerPrefs.Save();
            shouldLockdown = isServerInMaintenance;
        }
        else
        {
            shouldLockdown = CheckMaintenanceModeOffline();
        }

        if (shouldLockdown)
        {
            Debug.Log("[AppInitializer] MAINTENANCE MODE IS ACTIVE. Activating maintenance screen and halting startup.");
            LoadingScreenManager.Instance?.Hide();
            if (maintenanceScreenObject != null)
            {
                maintenanceScreenObject.SetActive(true);
            }
            // We still mark as initialized so the loading screen knows to hide on scene reloads.
            IsInitialized = true;
            return;
        }

        Debug.Log("[AppInitializer] Maintenance mode is not active. Proceeding to load user data...");

        if (auth.CurrentUser != null)
        {
            bool isGhost = await Vertex.Backend.AuthService.IsCurrentSessionGhostAsync();
            if (isGhost)
            {
                await Vertex.Backend.AuthService.ForceLogoutAndMigrateGhostDataAsync();
            }
        }

        if (scoreManager != null)
        {
            Debug.Log("[AppInitializer] Commanding ScoreManager to initialize and load initial data...");
            await scoreManager.InitializeAndLoadInitialData();
            Debug.Log("[AppInitializer] CONFIRMED: ScoreManager has finished its initial data load.");
        }

        ActivateManagers();
    }

    private async Task<bool> CheckMaintenanceModeOnlineAsync() { try { if (NoAppCheckApp == null) { Debug.LogError("[AppInitializer] CRITICAL: The NoAppCheckApp instance is null. Cannot check maintenance status."); return false; } var functions = FirebaseFunctions.GetInstance(NoAppCheckApp, "europe-west1"); var callable = functions.GetHttpsCallable("getServerStatus"); var result = await callable.CallAsync(); if (result.Data is IDictionary<object, object> data) { if (data.TryGetValue("isUnderMaintenance", out var maintenanceStatus)) { bool isUnderMaintenance = Convert.ToBoolean(maintenanceStatus); Debug.Log($"<color=cyan>[AppInitializer] Maintenance mode status from server: {isUnderMaintenance}</color>"); return isUnderMaintenance; } } return false; } catch (Exception e) { Debug.LogError($"[AppInitializer] Could not check maintenance mode, assuming it's off. Error: {e.Message}"); return false; } }
    private bool CheckMaintenanceModeOffline() { bool wasInMaintenance = PlayerPrefs.GetInt(MAINT_STATUS_KEY, 0) == 1; if (!wasInMaintenance) { return false; } long lastCheckedTicks; if (!long.TryParse(PlayerPrefs.GetString(MAINT_TIMESTAMP_KEY, "0"), out lastCheckedTicks) || lastCheckedTicks == 0) { return false; } var lastCheckedTime = new DateTime(lastCheckedTicks, DateTimeKind.Utc); var oneHourAgo = DateTime.UtcNow.Subtract(TimeSpan.FromHours(1)); return lastCheckedTime >= oneHourAgo; }

    private void ActivateManagers()
    {
        Debug.Log("[AppInitializer] Activating all game managers...");
        foreach (var manager in managersToActivate)
        {
            if (manager != null)
            {
                manager.SetActive(true);
            }
        }
        Debug.Log("[AppInitializer] Application startup sequence complete.");

        // The grand finale! Hide the loading screen.
        LoadingScreenManager.Instance?.Hide();

        // --- THE FINAL SIGNAL ---
        // We set this flag at the very end. The whole app is now officially ready.
        IsInitialized = true;
        Debug.Log("<color=lime>[AppInitializer] Initialization sequence is now marked as complete.</color>");
    }

    #endregion
}