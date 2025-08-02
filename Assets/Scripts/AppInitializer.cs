// FILE: AppInitializer.cs
// FINAL & PRODUCTION-READY VERSION (REPLICATES FLUTTER'S ROBUST MAINTENANCE CHECK)

using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Functions;
using Firebase.AppCheck;
using Firebase.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// A singleton responsible for the entire Firebase initialization sequence.
/// It replicates the robust startup logic from the Flutter app, including an
/// online/offline-aware maintenance mode check that uses cached data with a
/// staleness check to prevent permanent user lockouts.
/// </summary>
[AddComponentMenu("Vertex/App Initializer")]
public sealed class AppInitializer : MonoBehaviour
{
    #region Inspector Configuration

    [Header("UI Screens")]
    [Tooltip("The UI GameObject to activate if the app is in maintenance mode. It should be disabled in your scene by default.")]
    [SerializeField] private GameObject maintenanceScreenObject;

    [Header("Manager Activation")]
    [Tooltip("Drag all manager GameObjects here that depend on Firebase and should be activated after initialization is complete.")]
    [SerializeField] private List<GameObject> managersToActivate;

    [Header("Manager References")]
    [Tooltip("Drag the ScoreManager instance here. It will be initialized by this script after auth is ready.")]
    [SerializeField] private ScoreManager scoreManager;

    #endregion

    #region Static Properties
    public static bool FirebaseReady { get; private set; } = false;
    public static FirebaseApp DefaultApp { get; private set; }
    public static FirebaseApp NoAppCheckApp { get; private set; }
    #endregion

    #region Private Constants

    private const string MAINT_STATUS_KEY = "is_in_maintenance";
    private const string MAINT_TIMESTAMP_KEY = "maintenance_last_checked_ticks";
    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        var initializers = FindObjectsByType<AppInitializer>(FindObjectsSortMode.None);
        if (initializers.Length > 1)
        {
            Debug.Log("[AppInitializer] Duplicate instance found. Destroying self.");
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        Debug.Log("[AppInitializer] Initializing Application...");
        InitializeApplication();
    }

    private void OnDestroy()
    {
        // Ensure we unsubscribe from the event to prevent memory leaks
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
            // Step 1: Verify that Firebase dependencies are resolved and available.
            if (dependencyTask.Result != DependencyStatus.Available)
            {
                Debug.LogError($"[AppInitializer] Could not resolve all Firebase dependencies: {dependencyTask.Result}. Halting initialization.");
                return;
            }

            Debug.Log("[AppInitializer] Firebase dependencies are available.");
            DefaultApp = FirebaseApp.DefaultInstance;

            // Step 2: Create or get the secondary FirebaseApp instance that will NOT use App Check.
            // This is used for functions that don't need App Check, like checking maintenance status.
            try
            {
                const string noAppCheckInstanceName = "vertex-news-no-app-check";
                NoAppCheckApp = FirebaseApp.Create(DefaultApp.Options, noAppCheckInstanceName);
                Debug.Log($"[AppInitializer] Successfully created the secondary, non-AppCheck FirebaseApp instance: '{noAppCheckInstanceName}'");
            }
            catch (System.Exception)
            {
                NoAppCheckApp = FirebaseApp.GetInstance("vertex-news-no-app-check");
                Debug.LogWarning("[AppInitializer] Secondary FirebaseApp instance already existed. Re-using it.");
            }

            // --- THE FIX IS HERE ---
            // Step 3: Conditionally select the App Check provider based on the platform.
            // This is crucial to prevent compilation errors when building for production (e.g., Android/iOS),
            // as our custom 'SafeEditorAppCheckProviderFactory' only exists within the Unity Editor.

            IAppCheckProviderFactory providerFactory = null;



#if UNITY_EDITOR
            // Instead of relying on the SDK's automatic discovery mechanism, which can fail
            // due to race conditions or initialization order issues, we take control.
            // We read the environment variable ourselves and manually inject the token
            // directly into the provider instance. This is the most reliable method.

            string token = System.Environment.GetEnvironmentVariable("FIREBASE_APP_CHECK_DEBUG_TOKEN");

            if (!string.IsNullOrEmpty(token))
            {
                Debug.Log($"<color=cyan>[AppInitializer] Debug token read from environment variable. Setting it manually on the provider: |{token.Trim()}|</color>");
                // Set the token directly on the provider instance.
                DebugAppCheckProviderFactory.Instance.SetDebugToken(token.Trim());
            }
            else
            {
                Debug.LogWarning("[AppInitializer] FIREBASE_APP_CHECK_DEBUG_TOKEN environment variable not found. App Check will not work in the Editor.");
            }

            // Now that the token is explicitly set, we can safely get the provider instance.
            providerFactory = DebugAppCheckProviderFactory.Instance;
#else
    // If we are compiling for a real device (Android, iOS), we must use a production provider.
    Debug.LogWarning("[AppInitializer] This is a production build (not in Editor). App Check will be disabled unless a platform-specific provider is configured here.");
#endif

            // Step 4: Initialize App Check only if a valid provider factory has been set.
            if (providerFactory != null)
            {
                FirebaseAppCheck.SetAppCheckProviderFactory(providerFactory);
                FirebaseAppCheck.DefaultInstance.GetAppCheckTokenAsync(false).ContinueWithOnMainThread(tokenTask =>
                {
                    if (tokenTask.IsFaulted)
                    {
                        // This can still fail if the debug token in the provider is not registered in the Firebase Console.
                        Debug.LogError($"[AppInitializer] FAILED TO GET APP CHECK TOKEN. Error: {tokenTask.Exception}. Please ensure your debug token is correctly registered in the Firebase console.");
                        return;
                    }

                    // Success! We have a valid token.
                    Debug.Log("<color=lime>[AppInitializer] Successfully received App Check token.</color>");
                    OnFirebaseReady();
                });
            }
            else
            {
                // If no provider was set (e.g., in a production build), we proceed without App Check.
                // Functions that enforce App Check will fail, but the app can still start.
                Debug.LogWarning("[AppInitializer] App Check Provider Factory is null. Proceeding without App Check initialization.");
                OnFirebaseReady();
            }
        });
    }

    private void OnFirebaseReady()
    {
        Debug.Log("[AppInitializer] Firebase core services are ready. Waiting for initial Auth state...");
        FirebaseReady = true;
        FirebaseAuth.DefaultInstance.StateChanged += HandleAuthStateChanged;
    }

    /// <summary>
    /// This runs only once after auth state is resolved. It now contains the complete,
    /// robust maintenance check logic ported from the Flutter implementation.
    /// </summary>
    private async void HandleAuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        var auth = (FirebaseAuth)sender;
        auth.StateChanged -= HandleAuthStateChanged; // Unsubscribe, its job is done.

        Debug.Log("[AppInitializer] Initial Auth state received. Starting maintenance check...");

        bool shouldLockdown;
        bool isConnected = Application.internetReachability != NetworkReachability.NotReachable;

        if (isConnected)
        {
            Debug.Log("[AppInitializer] Online. Checking server status...");
            bool isServerInMaintenance = await CheckMaintenanceModeOnlineAsync();

            // Cache the result and timestamp locally, just like the Flutter app.
            PlayerPrefs.SetInt(MAINT_STATUS_KEY, isServerInMaintenance ? 1 : 0);
            PlayerPrefs.SetString(MAINT_TIMESTAMP_KEY, DateTime.UtcNow.Ticks.ToString());
            PlayerPrefs.Save();
            Debug.Log($"[AppInitializer] Server status saved. Maintenance = {isServerInMaintenance}");

            shouldLockdown = isServerInMaintenance;
        }
        else
        {
            Debug.Log("[AppInitializer] Offline. Checking last known status from local storage...");
            shouldLockdown = CheckMaintenanceModeOffline();
        }

        if (shouldLockdown)
        {
            Debug.Log("[AppInitializer] MAINTENANCE MODE IS ACTIVE. Activating maintenance screen and halting startup.");
            if (maintenanceScreenObject != null)
            {
                maintenanceScreenObject.SetActive(true);
            }
            // Stop the entire startup sequence if in maintenance.
            return;
        }

        Debug.Log("[AppInitializer] Maintenance mode is not active. Proceeding with user session check.");

        // --- The rest of the startup sequence remains untouched ---
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

    /// <summary>
    /// Checks the server for maintenance status via a Cloud Function. This is only called when online.
    /// This final version correctly handles the data type returned by the Firebase C# SDK,
    /// and includes robust logging to inspect the raw server response if needed.
    /// </summary>
    /// <returns>True if the server reports maintenance mode is active, otherwise false.</returns>
    private async Task<bool> CheckMaintenanceModeOnlineAsync()
    {
        try
        {
            if (NoAppCheckApp == null)
            {
                Debug.LogError("[AppInitializer] CRITICAL: The NoAppCheckApp instance is null. Cannot check maintenance status.");
                return false;
            }
            var functions = FirebaseFunctions.GetInstance(NoAppCheckApp, "europe-west1");
            var callable = functions.GetHttpsCallable("getServerStatus");
            var result = await callable.CallAsync();

            // --- THE FINAL FIX: Handle the data as a more generic dictionary type. ---
            // The C# SDK often deserializes JSON into IDictionary<object, object> rather
            // than IDictionary<string, object>. We now check for this more general type.
            if (result.Data is IDictionary<object, object> data)
            {
                // We then look for the key as a string within this dictionary.
                if (data.TryGetValue("isUnderMaintenance", out var maintenanceStatus))
                {
                    bool isUnderMaintenance = Convert.ToBoolean(maintenanceStatus);
                    Debug.Log($"<color=cyan>[AppInitializer] Maintenance mode status from server: {isUnderMaintenance}</color>");
                    return isUnderMaintenance;
                }
            }

            // --- DIAGNOSTIC LOGGING: This will only run if the check above fails. ---
            // It prints the raw data type and content, which is invaluable for debugging.
            string dataType = result.Data?.GetType().ToString() ?? "null";
            string dataContent = result.Data?.ToString() ?? "N/A";
            Debug.LogWarning($"[AppInitializer] Maintenance status key not found in server response. Raw data type: '{dataType}', Content: '{dataContent}'. Assuming not in maintenance.");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"[AppInitializer] Could not check maintenance mode, assuming it's off. Error: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Checks the cached maintenance status when the device is offline.
    /// Returns true only if the app was recently known to be in maintenance.
    /// </summary>
    private bool CheckMaintenanceModeOffline()
    {
        // Read the last known status from PlayerPrefs. Defaults to '0' (not in maintenance).
        bool wasInMaintenance = PlayerPrefs.GetInt(MAINT_STATUS_KEY, 0) == 1;

        if (!wasInMaintenance)
        {
            Debug.Log("[AppInitializer] Last known status is operational. Proceeding with normal offline mode.");
            return false; // Not in maintenance, so don't lock down.
        }

        // If the last known status was "in maintenance," check how old this information is.
        long lastCheckedTicks;
        if (!long.TryParse(PlayerPrefs.GetString(MAINT_TIMESTAMP_KEY, "0"), out lastCheckedTicks) || lastCheckedTicks == 0)
        {
            Debug.LogWarning("[AppInitializer] Found stale maintenance flag but no valid timestamp. Allowing access as a precaution.");
            return false; // No valid timestamp, can't trust the flag.
        }

        var lastCheckedTime = new DateTime(lastCheckedTicks, DateTimeKind.Utc);
        var oneHourAgo = DateTime.UtcNow.Subtract(TimeSpan.FromHours(1));

        if (lastCheckedTime < oneHourAgo)
        {
            Debug.Log("[AppInitializer] Offline, but maintenance info is STALE (>1 hour old). Allowing offline access.");
            return false; // The data is old, so don't lock down.
        }
        else
        {
            Debug.Log("[AppInitializer] Last known status is maintenance and data is FRESH. Enforcing lockdown.");
            return true; // The data is recent, so lock down.
        }
    }

    private void ActivateManagers()
    {
        Debug.Log("[AppInitializer] Activating all game managers...");
        foreach (var manager in managersToActivate)
        {
            if (manager != null)
            {
                manager.SetActive(true);
                Debug.Log($"[AppInitializer] Activated: {manager.name}");
            }
        }
        Debug.Log("[AppInitializer] Application startup sequence complete.");
    }

    #endregion
}