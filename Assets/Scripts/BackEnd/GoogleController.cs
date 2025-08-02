/***************************************************************************
 *  GoogleController.cs (SERVER-AUTHORITATIVE REVISION)
 *  -----------------------------------------------------------------------
 *  • Manages the Google Sign-In UI flow.
 *  • DECOUPLED: Correctly uses AuthService for all Firebase logic and
 *    UIManager for displaying results, adhering to a clean architecture.
 *  • Relies on the server-side 'onUserCreate' trigger for profile creation.
 ***************************************************************************/

using UnityEngine;
using Firebase.Auth;
using System.Threading.Tasks;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using System;
using Vertex.Backend; // Namespace for AuthService

public class GoogleController : MonoBehaviour
{
    #region Singleton
    public static GoogleController Instance { get; private set; }
    #endregion

    [Header("Dependencies")]
    [Tooltip("Reference to the UIManager for handling UI updates on auth success or failure.")]
    [SerializeField] private UIManager uiManager;

    private bool isSigningIn = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        PlayGamesPlatform.Activate();
    }

    public void SignInWithGoogle()
    {
        if (isSigningIn)
        {
            Debug.LogWarning("[GoogleController] Google Sign-In process is already in progress.");
            return;
        }

#if UNITY_EDITOR
        Debug.LogWarning("[GoogleController] Google Sign-In does not work in the Unity Editor. Simulating an error.");
        HandleSignInFailure(new Exception("Google Sign-In is disabled in the Editor."));
#else
        _ = StartGoogleSignInFlowAsync();
#endif
    }

    private async Task StartGoogleSignInFlowAsync()
    {
        isSigningIn = true;
        // NOTE: AuthService now handles its own operation state events.
        // No need to call a separate Signal method.

        try
        {
            var tcs = new TaskCompletionSource<string>();
            PlayGamesPlatform.Instance.Authenticate((SignInStatus status) =>
            {
                if (status == SignInStatus.Success)
                {
                    Debug.Log("[GoogleController] Google Play Games authentication successful. Requesting Server Auth Code.");
                    PlayGamesPlatform.Instance.RequestServerSideAccess(true, authCode =>
                    {
                        if (!string.IsNullOrEmpty(authCode)) tcs.SetResult(authCode);
                        else tcs.SetException(new Exception("Failed to get Server Auth Code."));
                    });
                }
                else
                {
                    tcs.SetException(new Exception($"Google Play Games auth failed. Status: {status}"));
                }
            });

            string serverAuthCode = await tcs.Task;
            await SignInToFirebaseAsync(serverAuthCode);
        }
        catch (Exception e)
        {
            HandleSignInFailure(e);
        }
        finally
        {
            isSigningIn = false;
        }
    }

    private async Task SignInToFirebaseAsync(string serverAuthCode)
    {
        Credential credential = PlayGamesAuthProvider.GetCredential(serverAuthCode);
        Debug.Log("[GoogleController] Firebase credential created. Passing to AuthService...");

        // Step 1: Sign in to Firebase Auth.
        FirebaseUser firebaseUser = await AuthService.SignInWithGoogleCredentialAsync(credential);

        try
        {
            // Step 2: Wait for the server-side 'onUserCreate' function to finish.
            await AuthService.EnsureUserDocumentExistsAsync(firebaseUser.UserId);

            // Step 3: Now that the document is confirmed, fetch the username for local storage.
            string username = await AuthService.GetUsernameFromProfileAsync(firebaseUser.UserId);
            if (!string.IsNullOrEmpty(username))
            {
                PlayerPrefs.SetString("SavedUsername", username);
                PlayerPrefs.Save();
                Debug.Log($"[GoogleController] Server-generated username '{username}' fetched and saved.");
            }
            else
            {
                Debug.LogWarning($"[GoogleController] User document exists, but username is null or empty for UID: {firebaseUser.UserId}");
            }

            // Step 4: ONLY NOW is it safe to proceed to the main UI.
            Debug.Log($"[GoogleController] Google Sign-In flow complete and user profile verified. Proceeding to main UI.");
            uiManager?.OnAuthenticationSuccess();
        }
        catch (Exception e)
        {
            // This will catch the TimeoutException from EnsureUserDocumentExistsAsync
            Debug.LogError($"[GoogleController] Failed to verify user profile after sign-in. Forcing logout. Error: {e.Message}");
            AuthService.Logout(); // Log out to prevent being in a broken state.
            HandleSignInFailure(e);
        }
    }

    private void HandleSignInFailure(Exception e)
    {
        Debug.LogError($"[GoogleController] Google Sign-In failed: {e.Message}\n{e.StackTrace}");
        uiManager?.HandleAuthException(e, isLoginError: true);
    }
}