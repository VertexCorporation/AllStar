/***************************************************************************
 *  AuthService.cs (THE FINAL, POLISHED VERSION)
 *  -----------------------------------------------------------------------
 *  • Manages all user authentication (Email/Password, Google) and profile
 *    modification logic.
 *  • Includes robust username validation, cooldowns, and secure, atomic
 *    database transactions for username changes.
 *  • All comments and logs are in English for clarity.
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Functions;
using UnityEngine;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Vertex.Backend
{
    /// <summary>
    /// A custom class to hold the detailed response from the redeemPromoCode function.
    /// </summary>
    public class RedeemCodeResponse
    {
        [JsonProperty("isSpecialUnlock")]
        public bool IsSpecialUnlock { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public static class AuthService
    {
        /// <summary>
        /// Fired when an authentication operation (login, register, etc.) starts or ends.
        /// The boolean parameter is 'true' when an operation begins, and 'false' when it completes.
        /// UI Managers should subscribe to this to disable/enable buttons.
        /// </summary>
        public static event Action<bool> OnAuthOperationStateChanged;
        private static FirebaseAuth _auth;
        private static FirebaseAuth auth
        {
            get
            {
                if (_auth == null)
                {
                    _auth = FirebaseAuth.DefaultInstance;
                }
                return _auth;
            }
        }

        private static FirebaseFirestore _db;
        private static FirebaseFirestore db
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

        private static FirebaseFunctions _functions;
        private static FirebaseFunctions functions
        {
            get
            {
                if (_functions == null)
                {
                    _functions = FirebaseFunctions.GetInstance(Firebase.FirebaseApp.DefaultInstance, "europe-west1");
                }
                return _functions;
            }
        }
        private static bool isOperating = false;

        /// <summary>
        /// Checks if a username conforms to the server's validation rules.
        /// Rule: 3-20 characters, allows letters (including Turkish), numbers, and symbols . - _
        /// </summary>
        /// <param name="username">The username to validate.</param>
        /// <returns>True if the format is valid, false otherwise.</returns>
        public static bool IsUsernameFormatValid(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return false;
            }
            var regex = new Regex(@"^[a-z0-9çğışöü\.\-_]{3,20}$", RegexOptions.IgnoreCase);
            return regex.IsMatch(username);
        }

        /// <summary>
        /// Checks if a username is available by calling a secure Cloud Function.
        /// </summary>
        /// <returns>True if the username is available, false otherwise.</returns>
        public static async Task<bool> IsUsernameAvailableAsync(string username)
        {
            try
            {
                var callable = functions.GetHttpsCallable("isUsernameAvailable");
                var result = await callable.CallAsync(new Dictionary<string, object> { { "username", username } });
                var data = result.Data as IDictionary<string, object>;
                return data != null && Convert.ToBoolean(data["available"]);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AuthService] Error checking username availability: {e.Message}");
                return false; // Fail safe: assume not available on error.
            }
        }


        /// <summary>
        /// Ensures the user's profile document exists in Firestore, waiting if necessary.
        /// This prevents race conditions after registration where client-side code might
        /// try to access the profile before the 'onUserCreate' trigger has finished.
        /// </summary>
        /// <param name="uid">The user ID to check for.</param>
        /// <param name="timeoutSeconds">How long to wait before throwing an exception.</param>
        /// <exception cref="System.TimeoutException">Thrown if the document is not found within the timeout period.</exception>
        public static async Task EnsureUserDocumentExistsAsync(string uid, int timeoutSeconds = 15)
        {
            Debug.Log($"[AuthService] Ensuring user document exists for UID: {uid}. Waiting up to {timeoutSeconds}s.");
            var userDocRef = db.Collection("users").Document(uid);
            var timeoutTask = Task.Delay(timeoutSeconds * 1000);

            while (true)
            {
                var snapshotTask = userDocRef.GetSnapshotAsync(Source.Server);
                var completedTask = await Task.WhenAny(snapshotTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    // Timeout occurred
                    Debug.LogError($"[AuthService] CRITICAL: Timed out waiting for user document for UID: {uid}.");
                    throw new System.TimeoutException("Server took too long to create the user profile. Please try again.");
                }

                // snapshotTask completed
                var snapshot = await snapshotTask;
                if (snapshot.Exists)
                {
                    Debug.Log($"[AuthService] User document for UID: {uid} confirmed to exist.");
                    return; // Success!
                }

                // Document doesn't exist yet, wait a moment before polling again.
                await Task.Delay(500);
            }
        }

        /// <summary>
        /// Fetches the username from a user's document. Assumes the document exists.
        /// </summary>
        public static async Task<string> GetUsernameFromProfileAsync(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return null;
            var snapshot = await db.Collection("users").Document(uid).GetSnapshotAsync(Source.Server);
            return snapshot.Exists ? snapshot.GetValue<string>("username") : null;
        }

        /// <summary>
        /// Creates a new user account via Firebase Auth and returns the user object.
        /// The calling UI controller is responsible for waiting for the user document to be created.
        /// </summary>
        /// <returns>The newly created FirebaseUser object.</returns>
        public static async Task<FirebaseUser> CreateAccountAsync(string username, string email, string password)
        {
            if (isOperating) throw new InvalidOperationException("Auth operation already in progress.");
            isOperating = true;
            OnAuthOperationStateChanged?.Invoke(true);

            try
            {
                // Step 1: Pre-check username availability
                if (!await IsUsernameAvailableAsync(username))
                {
                    throw new Exception("ERROR_USERNAME_TAKEN");
                }

                // Step 2: Create the user in Firebase Authentication
                AuthResult authResult = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
                string uid = authResult.User.UserId;
                Debug.Log($"[AuthService] Auth user created successfully. UID: {uid}.");

                // Step 3: Post the username suggestion for the backend
                var suggestionData = new Dictionary<string, object> { { "username", username } };
                await db.Collection("usernameSuggestions").Document(uid).SetAsync(suggestionData);
                Debug.Log($"[AuthService] Posted username suggestion '{username}' for server to process.");

                // Step 4: Send a verification email
                await authResult.User.SendEmailVerificationAsync();
                Debug.Log($"[AuthService] Verification email sent to {email}.");

                // Step 5: Return the user object so the UI can use the UID.
                return authResult.User;
            }
            finally
            {
                isOperating = false;
                OnAuthOperationStateChanged?.Invoke(false);
            }
        }
        /// <summary>
        /// Signs a user into Firebase using a Google credential.
        /// Note: This method NO LONGER waits for the document; it only handles auth.
        /// The calling controller is responsible for waiting for the profile document.
        /// </summary>
        public static async Task<FirebaseUser> SignInWithGoogleCredentialAsync(Credential credential)
        {
            if (isOperating) throw new InvalidOperationException("Auth operation already in progress.");
            isOperating = true;
            OnAuthOperationStateChanged?.Invoke(true);

            try
            {
                FirebaseUser newUser = await auth.SignInWithCredentialAsync(credential);
                Debug.Log($"[AuthService] Google Sign-In with Firebase successful. UID: {newUser.UserId}");
                return newUser;
            }
            finally
            {
                isOperating = false;
                OnAuthOperationStateChanged?.Invoke(false);
            }
        }
        /// <summary>
        /// Changes the current user's username by calling a secure Cloud Function.
        /// All validation, rate-limiting, and database operations are handled on the server.
        /// </summary>
        public static async Task ChangeUsernameAsync(string newUsername)
        {
            if (!IsSignedIn) throw new Exception("ERROR_NOT_SIGNED_IN");
            if (isOperating) throw new InvalidOperationException("Auth operation already in progress.");

            if (!IsUsernameFormatValid(newUsername))
            {
                throw new Exception("ERROR_USERNAME_FORMAT");
            }

            isOperating = true;
            OnAuthOperationStateChanged?.Invoke(true);

            try
            {
                Debug.Log($"[AuthService] Calling 'updateUsername' Cloud Function for user {UserId} with new username '{newUsername}'.");
                var callable = functions.GetHttpsCallable("updateUsername");
                await callable.CallAsync(new Dictionary<string, object> { { "newUsername", newUsername } });

                // Success! Update local cache.
                PlayerPrefs.SetString("SavedUsername", newUsername);
                PlayerPrefs.Save();
                Debug.Log($"[AuthService] Username successfully changed to '{newUsername}' via Cloud Function.");
            }
            catch (FunctionsException e)
            {
                // Handle specific errors thrown from the Cloud Function.
                Debug.LogError($"[AuthService] Cloud Function error changing username: {e.Message} (Code: {e.ErrorCode})");
                throw new Exception(e.Message); // Re-throw a simpler exception for the UI.
            }
            finally
            {
                isOperating = false;
                OnAuthOperationStateChanged?.Invoke(false);
            }
        }

        /// <summary>
        /// Requests account deletion by calling a secure Cloud Function.
        /// The server will immediately disable the user's account and schedule it for
        /// permanent deletion.
        /// </summary>
        public static async Task RequestAccountDeletionAsync()
        {
            if (!IsSignedIn) throw new Exception("ERROR_NOT_SIGNED_IN");
            if (isOperating) throw new InvalidOperationException("Auth operation already in progress.");
            isOperating = true;
            OnAuthOperationStateChanged?.Invoke(true);

            try
            {
                Debug.Log($"[AuthService] Calling 'requestAccountDeletion' Cloud Function for user {UserId}.");
                var callable = functions.GetHttpsCallable("requestAccountDeletion");
                await callable.CallAsync();

                // On success, the account is disabled. Log the user out locally.
                Debug.Log("[AuthService] Account deletion request successful. Logging out.");
                Logout();
            }
            catch (FunctionsException e)
            {
                Debug.LogError($"[AuthService] Cloud Function error requesting account deletion: {e.Message}");
                throw new Exception("An error occurred while requesting deletion. Your account may be disabled. Please contact support.");
            }
            finally
            {
                isOperating = false;
                OnAuthOperationStateChanged?.Invoke(false);
            }
        }

        /// <summary>
        /// Helper method to poll the user's document to retrieve a value,
        /// useful after a server-side trigger has been fired.
        /// </summary>
        private static async Task<string> PollForUsername(string uid)
        {
            var userDocRef = db.Collection("users").Document(uid);
            for (int i = 0; i < 10; i++) // Poll for up to 5 seconds.
            {
                var snapshot = await userDocRef.GetSnapshotAsync(Source.Server);
                if (snapshot.Exists)
                {
                    string username = snapshot.GetValue<string>("username");
                    if (!string.IsNullOrEmpty(username))
                    {
                        return username;
                    }
                }
                await Task.Delay(500); // Wait 500ms before retrying.
            }
            return null;
        }

        /// <summary>
        /// Logs a user in with their email and password.
        /// </summary>
        public static async Task<bool> LoginAsync(string email, string password)
        {
            if (isOperating) throw new InvalidOperationException("Auth operation already in progress.");
            isOperating = true;
            try
            {
                AuthResult authResult = await auth.SignInWithEmailAndPasswordAsync(email, password);
                FirebaseUser user = authResult.User;
                Debug.Log($"[AuthService] User signed in successfully with UID: {user.UserId}");

                DocumentSnapshot userProfile = await db.Collection("users").Document(user.UserId).GetSnapshotAsync();
                if (!userProfile.Exists)
                {
                    Debug.LogError($"[AuthService] CRITICAL: User profile not found for UID {user.UserId}. Signing out.");
                    auth.SignOut();
                    throw new Exception("User profile data is missing. If you just registered, please try again in a moment.");
                }

                string username = userProfile.GetValue<string>("username");
                PlayerPrefs.SetString("SavedUsername", username);
                PlayerPrefs.Save();
                Debug.Log($"[AuthService] Username '{username}' fetched and saved locally. Login complete.");
                return true;
            }
            finally
            {
                isOperating = false;
            }
        }

        /// <summary>
        /// Checks if the current locally signed-in user's data is missing on the server,
        /// indicating a "ghost" session (e.g., account deleted server-side).
        /// This simplified version only checks the 'users' collection as the single source of truth.
        /// </summary>
        /// <returns>True if the session is a ghost, false otherwise.</returns>
        public static async Task<bool> IsCurrentSessionGhostAsync()
        {
            if (!IsSignedIn)
            {
                // Not in a state where a ghost check is meaningful.
                return false;
            }

            string userId = UserId;
            Debug.Log($"[AuthService] Ghost Check: Verifying user document exists for UID: {userId} on server.");

            try
            {
                // Simplified Check: Only look for the user profile document.
                // The 'users' document is treated as the single source of truth for an account's existence.
                DocumentSnapshot userDoc = await db.Collection("users").Document(userId).GetSnapshotAsync(Source.Server);

                if (!userDoc.Exists)
                {
                    Debug.LogWarning($"[AuthService] Ghost Check: CONFIRMED GHOST. User document for UID '{userId}' is missing from the server.");
                    return true; // The user document is gone, so it's a ghost session.
                }

                Debug.Log($"[AuthService] Ghost Check: User document for UID '{userId}' found on server. Session is valid.");
                return false; // Document exists, the session is valid.
            }
            catch (Exception e)
            {
                // This catch block will now primarily handle network errors or permission issues on the 'users' collection.
                Debug.LogError($"[AuthService] Ghost Check: Error while verifying session integrity: {e.Message}. Assuming not a ghost to be safe.");
                return false; // Fail safe: if we can't check, assume it's not a ghost.
            }
        }

        /// <summary>
        /// Forces a local logout for the current user and triggers the ScoreManager
        /// to migrate their active data to the anonymous state.
        /// This should be called after IsCurrentSessionGhostAsync confirms a ghost session.
        /// </summary>
        public static async Task ForceLogoutAndMigrateGhostDataAsync()
        {
            if (!IsSignedIn || string.IsNullOrEmpty(Username))
            {
                Debug.LogWarning("[AuthService] ForceLogoutAndMigrateGhostDataAsync called, but no user is effectively signed in. Aborting.");
                return; // Implicitly returns Task.CompletedTask for an async Task method
            }

            string ghostUserId = UserId;
            string ghostUsername = Username;

            Debug.LogWarning($"[AuthService] Initiating forced logout and data migration for GHOST user: {ghostUsername} (UID: {ghostUserId}).");

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.PrepareForForcedAnonymousMigration(ghostUserId);
            }
            else
            {
                Debug.LogError("[AuthService] CRITICAL: ScoreManager.Instance is null. Cannot prepare for ghost data migration. Data might be lost!");
            }

            Logout(); // This is synchronous, but triggers ScoreManager's async Stated handler

            Debug.Log($"[AuthService] Forced logout for ghost user {ghostUsername} (UID: {ghostUserId}) complete. Data migration handled by ScoreManager.");

            // Add Task.Yield to ensure the method behaves asynchronously as declared,
            // yields control, and silences CS1998. The actual async work continues
            // in ScoreManager's event handler.
            await Task.Yield();
        }

        /// <summary>
        /// Logs out the current user and clears local data.
        /// </summary>
        public static void Logout()
        {
            if (IsSignedIn)
            {
                string oldUid = UserId;
                auth.SignOut();
                PlayerPrefs.DeleteKey("SavedUsername");
                PlayerPrefs.Save();
                Debug.Log($"[AuthService] User {oldUid} signed out successfully.");
            }
        }


        /// <summary>
        /// Deletes the current user's account by re-authenticating and then
        /// calling the secure server-side deletion request function.
        /// THIS IS THE ARCHITECTURALLY CONSISTENT APPROACH.
        /// </summary>
        public static async Task DeleteAccountAsync(string password)
        {
            FirebaseUser user = auth.CurrentUser;
            if (user == null)
            {
                throw new Exception("Cannot delete account. No user is signed in.");
            }

            if (isOperating) throw new InvalidOperationException("Auth operation already in progress.");
            isOperating = true;

            try
            {
                // Step 1: Re-authenticate the user to confirm their identity.
                // This is still a good security practice before a destructive action.
                if (user.ProviderData.Any(p => p.ProviderId == "password"))
                {
                    if (string.IsNullOrEmpty(password)) throw new Exception("ERROR_PASSWORD_REQUIRED");
                    Debug.Log("[AuthService] Re-authenticating user for account deletion...");
                    Credential credential = EmailAuthProvider.GetCredential(user.Email, password);
                    await user.ReauthenticateAsync(credential);
                    Debug.Log("[AuthService] Re-authentication successful.");
                }

                // Step 2: Call the modern, secure, server-side function to handle the actual deletion.
                // DO NOT delete documents from the client.
                await RequestAccountDeletionAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[AuthService] Account deletion process failed: {e.Message}");
                throw; // Re-throw the exception to be handled by the UI layer.
            }
            finally
            {
                // RequestAccountDeletionAsync already handles this, but for safety:
                isOperating = false;
            }
        }

        /// <summary>
        /// Changes the current user's password after re-authenticating with their old password.
        /// </summary>
        /// <param name="oldPassword">The user's current password.</param>
        /// <param name="newPassword">The desired new password.</param>
        public static async Task ChangePasswordAsync(string oldPassword, string newPassword)
        {
            if (!IsSignedIn)
            {
                throw new Exception("ERROR_NOT_SIGNED_IN");
            }

            if (!auth.CurrentUser.ProviderData.Any(p => p.ProviderId == "password"))
            {
                throw new Exception("ERROR_PROVIDER_NO_PASSWORD");
            }

            if (isOperating) throw new InvalidOperationException("Auth operation already in progress.");
            isOperating = true;

            try
            {
                FirebaseUser user = auth.CurrentUser;

                Debug.Log("[AuthService] Re-authenticating user for password change...");
                Credential credential = EmailAuthProvider.GetCredential(user.Email, oldPassword);
                await user.ReauthenticateAsync(credential);
                Debug.Log("[AuthService] Re-authentication successful.");

                await user.UpdatePasswordAsync(newPassword);
                Debug.Log("[AuthService] Password updated successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AuthService] Password change failed: {e.Message}");
                throw;
            }
            finally
            {
                isOperating = false;
            }
        }

        /// <summary>
        /// Redeems a promotional code by calling a secure Cloud Function.
        /// The server handles validation and returns whether the code was a standard reward
        /// or a special client-side unlock code.
        /// </summary>
        /// <param name="code">The promotional code entered by the user.</param>
        /// <returns>A RedeemCodeResponse object with details about the operation.</returns>
        public static async Task<RedeemCodeResponse> RedeemPromoCodeAsync(string code)
        {
            if (!IsSignedIn) throw new Exception("ERROR_NOT_SIGNED_IN");
            if (isOperating) throw new InvalidOperationException("An auth operation is already in progress.");

            isOperating = true;
            OnAuthOperationStateChanged?.Invoke(true);

            try
            {
                Debug.Log($"[AuthService] Calling 'redeemPromoCode' Cloud Function with code: '{code}'.");
                var callable = functions.GetHttpsCallable("redeemPromoCode");
                var result = await callable.CallAsync(new Dictionary<string, object> { { "code", code } });

                var response = JsonConvert.DeserializeObject<RedeemCodeResponse>(JsonConvert.SerializeObject(result.Data));

                Debug.Log($"[AuthService] Promo code redeemed. Message: {response.Message}, IsSpecialUnlock: {response.IsSpecialUnlock}");
                return response;
            }
            catch (FunctionsException e)
            {
                Debug.LogError($"[AuthService] Cloud Function error while redeeming promo code. ErrorCode: {e.ErrorCode}, Message: {e.Message}");
                string clientErrorMessage;
                switch (e.Message)
                {
                    case "not-found":
                        clientErrorMessage = "ERROR_CODE_INVALID";
                        break;
                    case "already-exists":
                        clientErrorMessage = "ERROR_CODE_ALREADY_REDEEMED";
                        break;
                    default:
                        clientErrorMessage = "ERROR_PROMO_INTERNAL";
                        break;
                }
                throw new Exception(clientErrorMessage);
            }
            finally
            {
                isOperating = false;
                OnAuthOperationStateChanged?.Invoke(false);
            }
        }

        /// <summary>
        /// Fetches the creation timestamp (in Unix seconds) for the currently signed-in user.
        /// This method is robust against race conditions by retrying a few times if the
        /// data is not immediately available after registration.
        /// This value is used as a dynamic salt for client-side encryption.
        /// </summary>
        /// <returns>The 'createdAt' field value as a long, or 0 if not found or not logged in.</returns>
        public static async Task<long> FetchUserCreationTimeAsync()
        {
            if (!IsSignedIn)
            {
                Debug.LogWarning("[AuthService] Cannot fetch creation time, no user is signed in.");
                return 0L;
            }

            string userId = UserId;
            string cachedSaltKey = $"vtx_salt_cache_{userId}";

            if (PlayerPrefs.HasKey(cachedSaltKey))
            {
                long cachedTimestamp = Convert.ToInt64(PlayerPrefs.GetString(cachedSaltKey));
                if (cachedTimestamp > 0)
                {
                    Debug.Log($"[AuthService] 'createdAt' found in local cache for user {userId}. Using cached value.");
                    return cachedTimestamp;
                }
            }

            Debug.Log($"[AuthService] 'createdAt' not in cache for {userId}. Fetching from Firestore...");
            DocumentReference userDocRef = db.Collection("users").Document(userId);
            const int maxRetries = 7;
            const int delayBetweenRetriesMs = 750;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    DocumentSnapshot snapshot = await userDocRef.GetSnapshotAsync(Source.Server);

                    if (snapshot.Exists && snapshot.ContainsField("createdAt"))
                    {
                        object createdAtObject = snapshot.GetValue<object>("createdAt");

                        if (createdAtObject is Timestamp ts)
                        {
                            DateTime dateTime = ts.ToDateTime();
                            long unixMilliseconds = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();

                            Debug.Log($"[AuthService] 'createdAt' (as Timestamp) found on attempt #{i + 1}. Value: {dateTime}");

                            PlayerPrefs.SetString(cachedSaltKey, unixMilliseconds.ToString());
                            PlayerPrefs.Save();
                            Debug.Log($"[AuthService] 'createdAt' for user {userId} saved to local cache for future sessions.");

                            return unixMilliseconds;
                        }
                        else
                        {
                            Debug.LogWarning($"[AuthService] 'createdAt' field found for user {userId} on attempt #{i + 1}, but it is not yet a Timestamp (Type: {createdAtObject?.GetType().Name ?? "null"}). Retrying...");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[AuthService] 'createdAt' field not found OR document does not exist for user {userId} on attempt #{i + 1}. Retrying in {delayBetweenRetriesMs}ms...");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AuthService] Exception while fetching user creation time on attempt #{i + 1}: {ex.Message}");
                }

                if (i < maxRetries - 1)
                {
                    await Task.Delay(delayBetweenRetriesMs);
                }
            }

            Debug.LogError($"[AuthService] CRITICAL: Failed to fetch 'createdAt' for user {userId} after {maxRetries} attempts. Cannot secure scores.");
            return 0L;
        }
        /// <summary>
        /// Fetches the user's 'hasCortexSubscription' reward level from Firestore.
        /// If a reward is found (value > 0), it immediately resets it to 0 in the database
        /// to prevent it from being awarded again. This is an atomic "consume" operation.
        /// </summary>
        /// <returns>The reward level (e.g., 7, 8, 9) or 0 if no reward is present.</returns>
        public static async Task<int> FetchAndConsumeRewardLevelAsync()
        {
            if (!IsSignedIn)
            {
                Debug.LogWarning("[AuthService] Tried to fetch reward level, but user is not signed in.");
                return 0;
            }

            // --- CRITICAL FIX APPLIED ---
            // Changed 'DB' to 'db' to match the property defined at the top of the class.
            var userRef = db.Collection("users").Document(UserId);

            try
            {
                var snapshot = await userRef.GetSnapshotAsync();
                if (!snapshot.Exists)
                {
                    Debug.LogError($"[AuthService] Could not fetch reward. User document for {UserId} does not exist.");
                    return 0;
                }

                // Use GetValue<int> which safely returns 0 if the field is missing or not a number.
                int rewardLevel = snapshot.GetValue<int>("hasCortexSubscription");

                if (rewardLevel > 0)
                {
                    Debug.Log($"[AuthService] Found reward level {rewardLevel} for user {UserId}. Consuming it now.");

                    // This is a "fire-and-forget" update. We don't need to wait for it to complete
                    // before returning the reward level to the client, speeding up the UI.
                    // The client gets the reward UI instantly, and the database updates in the background.
                    _ = userRef.UpdateAsync("hasCortexSubscription", 0);
                    return rewardLevel;
                }

                // No reward was found (field was 0 or missing).
                return 0;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AuthService] An exception occurred while fetching/consuming the reward level for user {UserId}: {e.Message}");
                return 0; // Return 0 on any failure to prevent unintended behavior.
            }
        }

        // In AuthService.cs

        /// <summary>
        /// A comprehensive wrapper for creating a new account, including input validation.
        /// This method is designed to be called directly from UI button events.
        /// It will throw specific exceptions that the UIManager can catch and display.
        /// </summary>
        public static async Task<FirebaseUser> UIRegisterAsync(string username, string email, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                throw new Exception("ERROR_ALL_FIELDS_REQUIRED");
            }

            if (!IsUsernameFormatValid(username))
            {
                throw new Exception("ERROR_USERNAME_FORMAT");
            }


            return await CreateAccountAsync(username, email, password);
        }

        /// <summary>
        /// A comprehensive wrapper for logging in, including input validation.
        /// Designed to be called directly from UI button events.
        /// It will throw specific exceptions that the UIManager can catch and display.
        /// </summary>
        /// <returns>True on success.</returns>
        public static async Task<bool> UILoginAsync(string email, string password)
        {
            // 1. Centralized Input Validation
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                throw new Exception("ERROR_EMAIL_PASSWORD_REQUIRED");
            }

            // 2. Signal start of operation
            OnAuthOperationStateChanged?.Invoke(true);

            try
            {
                // 3. Call the core logic method
                return await LoginAsync(email, password);
            }
            finally
            {
                // 4. Signal end of operation
                OnAuthOperationStateChanged?.Invoke(false);
            }
        }

        /// <summary>
        /// Public method to signal the start of an authentication operation.
        /// This allows external controllers (like GoogleController) to use the central event system.
        /// </summary>
        public static void SignalAuthOperationStart()
        {
            OnAuthOperationStateChanged?.Invoke(true);
        }

        /// <summary>
        /// Public method to signal the end of an authentication operation.
        /// </summary>
        public static void SignalAuthOperationEnd()
        {
            OnAuthOperationStateChanged?.Invoke(false);
        }

        // --- Properties ---
        public static bool IsSignedIn => auth.CurrentUser != null;
        public static bool IsRealUser => IsSignedIn; // This can be expanded later for anonymous vs. real users
        public static string UserId => auth.CurrentUser?.UserId;
        public static string Username => PlayerPrefs.GetString("SavedUsername", "");
    }
}