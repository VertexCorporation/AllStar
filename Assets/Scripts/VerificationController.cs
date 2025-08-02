/***************************************************************************
 *  VerificationController.cs (THE DEFINITIVE, POLISHED, FINAL VERSION)
 *  -----------------------------------------------------------------------
 *  • This is the definitive, server-driven implementation for the verification UI.
 *  • Countdown timer is calculated based on server data and now animates
 *    intelligently ONLY when the time format changes (e.g., from hours to minutes).
 *  • Provides clear visual feedback on the resend button when max attempts are reached.
 *  • All async/await logic for UI has been converted to pure Coroutines for
 *    simplicity and stability within Unity's environment.
 ***************************************************************************/

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;

public class VerificationController : MonoBehaviour
{
    #region Inspector Fields
    [Header("UI Core References")]
    [SerializeField] private GameObject verificationPanel;
    [SerializeField] private Button resendButton;
    [SerializeField] private Text resendButtonText;

    [Header("Content Containers")]
    [SerializeField] private CanvasGroup timerContentGroup;
    [SerializeField] private CanvasGroup messageContentGroup;
    [SerializeField] private Text messageText;

    [Header("Timer Display")]
    [SerializeField] private Text remainingLabel;
    [SerializeField] private Text timeText;

    [Header("Configuration")]
    [SerializeField] private float verificationCheckInterval = 5.0f;
    [SerializeField] private int maxResendAttempts = 1;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float messageDisplayDuration = 3.5f;
    [SerializeField] private float shakeIntensity = 10.0f;
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private Color successColor = Color.cyan;
    [SerializeField] private Color failureColor = Color.red;
    #endregion

    #region Private State
    private FirebaseUser currentUser;
    private FirebaseFirestore db;
    private DocumentReference userDocRef;
    private Coroutine mainCoroutine;
    private Action onPanelClosed;

    private DateTime accountCreationTime;
    private int verifyAttempts;
    private RectTransform panelToShake;
    private string currentFormatKey;
    private bool isShowingMessage = false; // Prevents message spam
    #endregion

    #region Unity Methods
    private void Awake()
    {
        if (verificationPanel != null) verificationPanel.SetActive(false);
        panelToShake = messageContentGroup.GetComponent<RectTransform>();

        // FLAWLESS FIX: Start with the message content group invisible and text inactive
        if (messageContentGroup != null) messageContentGroup.alpha = 0;
        if (messageText != null) messageText.gameObject.SetActive(false);
    }
    #endregion

    #region Public API
    public void PreparePanel(FirebaseUser user, Action closedCallback = null)
    {
        if (user == null || user.IsEmailVerified)
        {
            closedCallback?.Invoke();
            return;
        }
        currentUser = user;
        onPanelClosed = closedCallback;
        db = FirebaseFirestore.DefaultInstance;
        userDocRef = db.Collection("users").Document(currentUser.UserId);

        if (mainCoroutine != null) StopCoroutine(mainCoroutine);
        mainCoroutine = StartCoroutine(MasterVerificationRoutine());
    }

    public void OnCloseButtonClicked()
    {
        ClosePanel();
    }

    public void OnResendButtonClicked()
    {
        StartCoroutine(ResendEmailCoroutine());
    }
    #endregion

    #region Core Routines
    private IEnumerator MasterVerificationRoutine()
    {
        yield return StartCoroutine(FetchInitialState());

        float checkTimer = 0f;
        while (verificationPanel.activeSelf && currentUser != null && !currentUser.IsEmailVerified)
        {
            UpdateCountdownDisplay();
            checkTimer += Time.deltaTime;

            if (checkTimer >= verificationCheckInterval)
            {
                checkTimer = 0f;
                yield return StartCoroutine(PollForVerification());
            }
            yield return null;
        }
    }

    private IEnumerator FetchInitialState()
    {
        timerContentGroup.alpha = 0;

        var task = userDocRef.GetSnapshotAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Debug.LogError($"[VerificationController] Failed to fetch user data: {task.Exception.GetBaseException()}");
            yield return StartCoroutine(ShowMessageCoroutine(GetL("GenericError"), true));
            ClosePanel();
            yield break;
        }

        var snapshot = task.Result;
        if (!snapshot.Exists)
        {
            Debug.LogError("[VerificationController] User document does not exist! Cannot proceed.");
            yield return StartCoroutine(ShowMessageCoroutine(GetL("GenericError"), true));
            ClosePanel();
            yield break;
        }

        accountCreationTime = snapshot.GetValue<Timestamp>("createdAt").ToDateTime();
        verifyAttempts = snapshot.ContainsField("verifyAttempts") ? snapshot.GetValue<int>("verifyAttempts") : 0;

        UpdateUIState();
        StartCoroutine(FadeCanvasGroup(timerContentGroup, 1, fadeDuration));
    }

    private IEnumerator PollForVerification()
    {
        if (currentUser == null) yield break;

        var reloadTask = currentUser.ReloadAsync();
        yield return new WaitUntil(() => reloadTask.IsCompleted);

        if (reloadTask.IsFaulted)
        {
            Debug.LogError($"[VerificationController] Failed to reload user data: {reloadTask.Exception.GetBaseException().Message}");
        }
        else if (currentUser.IsEmailVerified)
        {
            Debug.Log("[VerificationController] SUCCESS: User email has been verified!");
            var deleteTask = userDocRef.UpdateAsync("verifyAttempts", FieldValue.Delete);
            yield return new WaitUntil(() => deleteTask.IsCompleted);
            ClosePanel();
        }
    }

    private IEnumerator ResendEmailCoroutine()
    {
        if (currentUser == null || !resendButton.interactable) yield break;

        resendButton.interactable = false;

        if (verifyAttempts >= maxResendAttempts)
        {
            yield return StartCoroutine(ShowMessageCoroutine(GetL("MaxAttempts"), true));
            UpdateUIState();
            yield break;
        }

        Task updateTask = userDocRef.UpdateAsync("verifyAttempts", FieldValue.Increment(1));
        yield return new WaitUntil(() => updateTask.IsCompleted);

        if (updateTask.IsFaulted)
        {
            HandleResendException(updateTask.Exception);
            UpdateUIState();
            yield break;
        }

        Task sendTask = currentUser.SendEmailVerificationAsync();
        yield return new WaitUntil(() => sendTask.IsCompleted);

        if (sendTask.IsFaulted)
        {
            HandleResendException(sendTask.Exception);
        }
        else
        {
            verifyAttempts++;
            yield return StartCoroutine(ShowMessageCoroutine(GetL("EmailSent"), false));
        }

        UpdateUIState();
    }
    #endregion

    #region UI & Display Logic

    private void UpdateUIState()
    {
        if (resendButtonText == null) return;

        if (verifyAttempts >= maxResendAttempts)
        {
            resendButton.interactable = false;
            resendButtonText.text = GetL("MaxAttemptsShort");
        }
        else
        {
            resendButton.interactable = true;
            resendButtonText.text = GetL("Resend");
        }
    }

    private void UpdateCountdownDisplay()
    {
        TimeSpan timeSinceCreation = DateTime.UtcNow - accountCreationTime;
        double totalHoursAllowed = 24 * (verifyAttempts + 1);
        TimeSpan totalTimeAllowed = TimeSpan.FromHours(totalHoursAllowed);
        TimeSpan remaining = totalTimeAllowed - timeSinceCreation;

        string newTimeText = FormatTime(remaining, out string newFormatKey);

        if (currentFormatKey != newFormatKey)
        {
            currentFormatKey = newFormatKey;
            StartCoroutine(AnimateTextUpdate(timeText, newTimeText));
        }
        else
        {
            timeText.text = newTimeText;
        }

        remainingLabel.text = GetL("TimeRemaining");
    }

    private string FormatTime(TimeSpan time, out string formatKey)
    {
        if (time.TotalSeconds <= 0)
        {
            formatKey = "TimeUp";
            return GetL(formatKey);
        }
        if (time.TotalDays >= 1)
        {
            formatKey = "Days";
            return $"{time.Days} {GetL("Day", time.Days)} {time.Hours:D2} {GetL("Hour", time.Hours)}";
        }
        if (time.TotalHours >= 1)
        {
            formatKey = "Hours";
            return $"{time.Hours:D2} {GetL("Hour", time.Hours)} {time.Minutes:D2} {GetL("Minute", time.Minutes)}";
        }
        if (time.TotalMinutes >= 1)
        {
            formatKey = "Minutes";
            return $"{time.Minutes:D2} {GetL("Minute", time.Minutes)} {time.Seconds:D2} {GetL("Second", time.Seconds)}";
        }

        formatKey = "Seconds";
        return $"{Mathf.FloorToInt(time.Seconds)} {GetL("Second", (int)time.Seconds)}";
    }

    private void ClosePanel()
    {
        if (mainCoroutine != null) StopCoroutine(mainCoroutine);
        onPanelClosed?.Invoke();
    }
    #endregion

    #region Animations, Messages & Error Handling
    private void HandleResendException(Exception ex)
    {
        string errorMessage;
        if (ex.GetBaseException() is Firebase.FirebaseException firebaseEx)
        {
            switch ((AuthError)firebaseEx.ErrorCode)
            {
                case AuthError.TooManyRequests:
                    errorMessage = GetL("UnusualActivity");
                    break;
                default:
                    errorMessage = GetL("GenericError");
                    break;
            }
        }
        else
        {
            errorMessage = GetL("GenericError");
        }
        StartCoroutine(ShowMessageCoroutine(errorMessage, true));
    }

    private IEnumerator AnimateTextUpdate(Text textElement, string newText)
    {
        yield return StartCoroutine(FadeText(textElement, 0, fadeDuration / 2));
        textElement.text = newText;
        yield return StartCoroutine(FadeText(textElement, 1, fadeDuration / 2));
    }

    private IEnumerator FadeText(Text textElement, float targetAlpha, float duration)
    {
        Color startColor = textElement.color;
        float time = 0;
        while (time < duration)
        {
            textElement.color = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, targetAlpha), time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        textElement.color = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);
    }

    private IEnumerator ShowMessageCoroutine(string message, bool isError)
    {
        if (isShowingMessage) yield break;
        isShowingMessage = true;

        // FLAWLESS FIX: Activate the messageText GameObject before showing
        if (messageText != null) messageText.gameObject.SetActive(true);

        messageText.text = message;
        messageText.color = isError ? failureColor : successColor;

        if (isError) StartCoroutine(ShakeCoroutine());

        yield return StartCoroutine(FadeCanvasGroup(timerContentGroup, 0, fadeDuration));
        yield return StartCoroutine(FadeCanvasGroup(messageContentGroup, 1, fadeDuration));

        yield return new WaitForSeconds(messageDisplayDuration);

        if (!verificationPanel.activeSelf) // Check if panel is still active before proceeding
        {
            // FLAWLESS FIX: Ensure messageText is deactivated if panel closes early
            if (messageText != null) messageText.gameObject.SetActive(false);
            isShowingMessage = false;
            yield break;
        }

        yield return StartCoroutine(FadeCanvasGroup(messageContentGroup, 0, fadeDuration));

        // FLAWLESS FIX: Deactivate the messageText GameObject after fading out
        if (messageText != null) messageText.gameObject.SetActive(false);

        // Only fade timer back in if the panel itself hasn't been closed
        if (verificationPanel.activeSelf)
        {
            yield return StartCoroutine(FadeCanvasGroup(timerContentGroup, 1, fadeDuration));
        }

        isShowingMessage = false;
    }

    private IEnumerator ShakeCoroutine()
    {
        Vector3 originalPos = panelToShake.anchoredPosition;
        float elapsedTime = 0f;
        while (elapsedTime < shakeDuration)
        {
            panelToShake.anchoredPosition = originalPos + (Vector3)UnityEngine.Random.insideUnitCircle * shakeIntensity;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        panelToShake.anchoredPosition = originalPos;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float targetAlpha, float duration)
    {
        float startAlpha = cg.alpha;
        float time = 0;
        while (time < duration)
        {
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        cg.alpha = targetAlpha;
    }
    #endregion

    #region Localization

    private string GetL(string key, int amount = 1)
    {
        bool isTurkish = PlayerPrefs.GetInt("SelectedLanguage", 0) == 1;
        bool isPlural = amount != 1;

        if (isTurkish)
        {
            switch (key)
            {
                case "TimeRemaining": return "kalan zaman:";
                case "TimeUp": return "süre doldu";
                case "MaxAttempts": return "maksimum deneme hakkına ulaştın";
                case "MaxAttemptsShort": return "HAKKIN DOLDU";
                case "Resend": return "TEKRAR YOLLA";
                case "EmailSent": return "doğrulama e-postası gönderildi";
                case "GenericError": return "bir hata oluştu, lütfen tekrar dene";
                case "UnusualActivity": return "sıradışı aktivite tespit edildi, lütfen daha sonra tekrar dene";
                case "Day": return "gün";
                case "Hour": return "saat";
                case "Minute": return "dakika";
                case "Second": return "saniye";
                default: return key;
            }
        }
        else // English
        {
            switch (key)
            {
                case "TimeRemaining": return "time remaining:";
                case "TimeUp": return "time's up";
                case "MaxAttempts": return "you have reached the maximum number of attempts";
                case "MaxAttemptsShort": return "MAX ATTEMPTS";
                case "Resend": return "RESEND";
                case "EmailSent": return "verification email has been sent";
                case "GenericError": return "an error occurred, please try again";
                case "UnusualActivity": return "unusual activity detected, please try again later";
                case "Day": return isPlural ? "days" : "day";
                case "Hour": return isPlural ? "hours" : "hour";
                case "Minute": return isPlural ? "minutes" : "minute";
                case "Second": return isPlural ? "seconds" : "second";
                default: return key;
            }
        }
    }
    #endregion
}