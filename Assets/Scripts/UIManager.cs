using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using SgLib;
using System;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Components;
using Vertex.Backend;
using Firebase.Auth;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public GameManager gameManager;
    public SelectCharacter characterSprite;
    public Skill s;
    public BuneLean bl;
    public SeasonManager seasonManager;
    public Rewarded rw;
    public GameObject title, nextBtn, prevBtn, tapToStart, restartBtn, menuButtons, settingsUI, soundOnBtn, soundOffBtn, newsBtn, bchngBtn, chngnBtn, chrctr, abeb, taptap, blglndrm, code, otfh, kb, kampanyaui, vb, vertex, bb, bbUI, kedi;
    public GameObject HTP, htpBtn, Donate, dBtn, STOP, stpBtn, ShopB, ShopUI, ShopUI2, dfbg, night, subliminal, ds, newsUI, acsUI, sureUI, shUI, BgUI, Tt, ak, codeUI, loginUI, loginBtn, signUpBtn, logoutUI, logoutButton, verificationUI, resetPasswordButton, resetPasswordUI;
    public GameObject alinditext, alinditextn, alinditexte, alinditextm, skillpuan, nightpuan, equipmentpuan, multiplypuan, equipmentyt, shieldsp, alinditextee, alinditextss, alinditexts, shieldpuan, changeNameUI, alinditextp, phoenixpuan;
    public GameObject lb, lbui, ab, abui, hy, discordsw, SB;
    public SelectCharacter cs;

    [Header("Promo Code UI")]
    [Tooltip("The input field where the user types the promo code.")]
    [SerializeField] private InputField promoCodeInput;

    [Tooltip("The button that triggers the code redemption.")]
    [SerializeField] private Button redeemCodeButton;

    [Tooltip("The Text object used to display success or error messages for the promo code.")]
    [SerializeField] private Text promoCodeErrorText;

    private Coroutine promoCodeErrorCoroutine;

    private DateTime buttonExpireDate;
    public Text score, ac, bs, bg, totalscore, creditText;
    public bool isNeymPurchased, nc, isBeginningPlayer, ad, promosyon, VertexAI;
    private const string buttonKey = "ButtonState", IS_BEGINNING_PLAYER = "IsBeginningPlayer", IS_NEYM_PURCHASED = "IsNeymPurchased", AI = "VertexAI";
    public string serverURL = "https://discord.gg/sK53fypPBZ";

    [Header("Seasonal Reward UI")]
    [SerializeField] private GameObject seasonRewardPanel;
    [SerializeField] private Text rewardText;

    [Header("Account Settings Buttons")]
    [SerializeField] private GameObject changeNameButton;
    [SerializeField] private GameObject deleteAccountButton;
    [SerializeField] private GameObject logoutAccountButton;

    private int Lang => PlayerPrefs.GetInt("SelectedLanguage", 0);

    [SerializeField] private VerificationController verificationController;
    [SerializeField] private List<Button> googleSignInButtons;

    [Header("Account Deletion UI")]
    [SerializeField] private Text passwordPromptText;

    [Header("UI Feedback Colors")]
    [SerializeField] private Color errorColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private Color successColor = new Color(0.4f, 1f, 0.4f);
    private Color originalPromptColor;

    private string originalPromptText;

    private Coroutine promptErrorCoroutine;

    [Header("Change Name UI")]
    [Tooltip("The input field for entering the new desired username.")]
    [SerializeField] private InputField newUsernameInput;

    [Tooltip("The button that confirms and initiates the name change process.")]
    [SerializeField] private Button confirmChangeNameButton;

    [Tooltip("The Text object used to display errors during the name change process.")]
    [SerializeField] private Text changeNameErrorText;

    [Header("Change Password UI")]
    [SerializeField] private InputField oldPasswordInput;
    [SerializeField] private InputField newPasswordInput;
    [SerializeField] private InputField confirmNewPasswordInput;

    [SerializeField] private Button confirmChangePasswordButton;
    [SerializeField] private Text changePasswordErrorText;
    private Coroutine changeNameErrorCoroutine;
    private Dictionary<Text, string> originalChangeNameTexts = new Dictionary<Text, string>();
    private Dictionary<Text, Color> originalChangeNameColors = new Dictionary<Text, Color>();
    private Coroutine changePasswordErrorCoroutine;
    Animator scoreAnimator;
    private bool _uiInitDone = false;

    // Add these new Header sections and fields to the top of your UIManager class.
    [Header("Authentication Panels")]
    [SerializeField] private GameObject loginPanel; // Assign the parent panel for login
    [SerializeField] private GameObject signUpPanel; // Assign the parent panel for sign-up

    [Header("Login UI")]
    [SerializeField] private InputField loginEmailInput;
    [SerializeField] private InputField loginPasswordInput;
    [SerializeField] private Text loginErrorText;

    [Header("Sign Up UI")]
    [SerializeField] private InputField signUpUsernameInput;
    [SerializeField] private InputField signUpEmailInput;
    [SerializeField] private InputField signUpPasswordInput;
    [SerializeField] private Text signUpErrorText;

    // Add these new private fields for managing the error animations.
    private Coroutine loginErrorCoroutine;
    private Coroutine signUpErrorCoroutine;

    private Dictionary<Text, Vector3> originalTextPositions = new Dictionary<Text, Vector3>();

    [Header("Module References")]
    [Tooltip("A reference to the VertexNews script component on the News UI panel.")]
    [SerializeField] private VertexNews vertexNews;

    void OnEnable()
    {
        GameManager.GameStateChanged += GameManager_GameStateChanged;
        ScoreManager.ScoreUpdated += OnScoreUpdated;

        ScoreManager.HighscoreUpdated += OnUserDataChanged;
        ScoreManager.TotalScoreUpdated += OnUserDataChanged;
        ScoreManager.ItemPurchased += OnItemPurchased;

        StartCoroutine(InternetCheckRoutine());
    }

    void OnDisable()
    {
        GameManager.GameStateChanged -= GameManager_GameStateChanged;
        ScoreManager.ScoreUpdated -= OnScoreUpdated;

        ScoreManager.HighscoreUpdated -= OnUserDataChanged;
        ScoreManager.TotalScoreUpdated -= OnUserDataChanged;
        ScoreManager.ItemPurchased -= OnItemPurchased;

        StopAllCoroutines();
    }

    async void Start() // Changed from void to async void
    {
        if (AuthService.IsSignedIn && string.IsNullOrEmpty(AuthService.Username))
        {
            Debug.Log("[UIManager] Startup Sanity Check FAILED: User is signed in, but local username is missing. Forcing logout to re-sync.");
            AuthService.Logout();
        }
        else if (AuthService.IsSignedIn && !string.IsNullOrEmpty(AuthService.Username))
        {
            Debug.Log($"[UIManager] Startup: User {AuthService.UserId} ({AuthService.Username}) appears logged in. Verifying session integrity with server...");
            try
            {
                bool isGhost = await AuthService.IsCurrentSessionGhostAsync();
                if (isGhost)
                {
                    Debug.LogWarning($"[UIManager] Startup: GHOST session detected for {AuthService.UserId} ({AuthService.Username}). Forcing local logout and migrating data to anonymous state.");
                    await AuthService.ForceLogoutAndMigrateGhostDataAsync();
                    Debug.Log("[UIManager] Startup: Ghost session migration process initiated via AuthService. ScoreManager will handle the data migration asynchronously.");
                }
                else
                {
                    Debug.Log($"[UIManager] Startup: Session for {AuthService.UserId} ({AuthService.Username}) appears valid on server.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIManager] Startup: Exception during user integrity check or ghost migration: {e.Message}\n{e.StackTrace}");
            }
        }

        // The rest of your Start method remains the same
        subliminal.SetActive(false);
        if (score != null)
        {
            scoreAnimator = score.GetComponent<Animator>();
            if (scoreAnimator == null)
            {
                Debug.LogWarning("[UIManager] Animator component not added to 'Score' object. Score animations will not work. Please add it from the Inspector.", score.gameObject);
            }
        }
        else
        {
            Debug.LogError("[UIManager] ERROR: 'Score' Text reference not assigned in the Inspector!", this.gameObject);
        }
        if (passwordPromptText != null)
        {
            originalPromptColor = passwordPromptText.color;
        }
        else
        {
            Debug.LogError("[UIManager] 'Password Prompt Text' not assigned for error display!", this.gameObject);
        }
        if (googleSignInButtons != null && googleSignInButtons.Count > 0)
        {
            foreach (Button button in googleSignInButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => GoogleController.Instance.SignInWithGoogle());
                    button.onClick.AddListener(ButtonClickSound);
                }
            }
            Debug.Log($"[UIManager] {googleSignInButtons.Count} Google buttons configured successfully!");
        }
        else
        {
            Debug.LogWarning("[UIManager] No Google buttons were added to the list in the Inspector!");
        }
        Reset();
        SetInitialLanguage();
        ShowStartUI();
        ad = false;

        SetBackgroundText(false);

        if (Application.systemLanguage == SystemLanguage.Turkish && !PlayerPrefs.HasKey(IS_BEGINNING_PLAYER))
        {
            BgUI.SetActive(true);
        }
        else
        {
            BgUI.SetActive(false);
        }

        UpdateAllShopAndUI();
        CheckForVerification();
        _uiInitDone = true;
    }

    private void OnUserDataChanged(int ignoredValue)
    {
        UpdateAllShopAndUI();
    }

    private void OnItemPurchased(string itemName)
    {
        Debug.Log($"[UIManager] Received event that '{itemName}' was purchased. Refreshing UI.");
        UpdateAllShopAndUI();
    }

    private void UpdateAllShopAndUI()
    {
        if (ScoreManager.Instance == null) return;

        bs.text = ScoreManager.Instance.HighScore.ToString();
        totalscore.text = ScoreManager.Instance.TotalScore.ToString();

        alinditext.SetActive(ScoreManager.Instance.IsSkillPurchased);
        skillpuan.SetActive(!ScoreManager.Instance.IsSkillPurchased);

        alinditextn.SetActive(ScoreManager.Instance.IsNightPurchased);
        nightpuan.SetActive(!ScoreManager.Instance.IsNightPurchased);
        bchngBtn.SetActive(ScoreManager.Instance.IsNightPurchased);

        alinditexte.SetActive(ScoreManager.Instance.IsEquipmentPurchased);
        equipmentpuan.SetActive(!ScoreManager.Instance.IsEquipmentPurchased);
        alinditextee.SetActive(ScoreManager.Instance.IsEquipmentPurchased);
        equipmentyt.SetActive(!ScoreManager.Instance.IsEquipmentPurchased);

        alinditextm.SetActive(ScoreManager.Instance.IsMultiplyPurchased);
        multiplypuan.SetActive(!ScoreManager.Instance.IsMultiplyPurchased);

        alinditexts.SetActive(ScoreManager.Instance.IsShieldPurchased);
        shieldpuan.SetActive(!ScoreManager.Instance.IsShieldPurchased);
        alinditextss.SetActive(ScoreManager.Instance.IsShieldPurchased);
        shieldsp.SetActive(!ScoreManager.Instance.IsShieldPurchased);

        alinditextp.SetActive(ScoreManager.Instance.IsPhoenixPurchased);
        phoenixpuan.SetActive(!ScoreManager.Instance.IsPhoenixPurchased);

        bool isNight = ScoreManager.Instance.IsNightBackgroundActive;
        if (dfbg != null) dfbg.SetActive(!isNight);
        if (night != null) night.SetActive(isNight);
        SetBackgroundText(isNight);

        if (gameManager != null)
        {
            gameManager.equipment = ScoreManager.Instance.IsEquipmentPurchased;
            gameManager.multiply = ScoreManager.Instance.IsMultiplyPurchased;
            gameManager.shield = ScoreManager.Instance.IsShieldPurchased;
        }
    }

    private void SetBackgroundText(bool isNight)
    {
        string currentLanguage = LocalizationSettings.SelectedLocale.Identifier.Code;

        if (isNight)
        {
            if (currentLanguage.StartsWith("tr"))
            {
                bg.text = "gece Arkaplanı";
            }
            else
            {
                bg.text = "night Backdrop";
            }
        }
        else
        {
            if (currentLanguage.StartsWith("tr"))
            {
                bg.text = "sabah Arkaplanı";
            }
            else
            {
                bg.text = "morning Backdrop";
            }
        }
    }

    void Update()
    {
        score.text = ScoreManager.Instance.Score.ToString().Replace("0", "O");
        if (rw.bonus)
        {
            score.color = Color.magenta;
        }
        if (settingsUI.activeSelf)
        {
            UpdateMuteButtons();
        }
        if (ScoreManager.Instance.Score == 6 || ScoreManager.Instance.Score == 10 || ScoreManager.Instance.Score == 16 || ScoreManager.Instance.Score == 20 || ScoreManager.Instance.Score == 24 || ScoreManager.Instance.Score == 1 && gameManager.GameState == GameState.Playing)
        {
            subliminal.SetActive(true);
        }
        else
        {
            subliminal.SetActive(false);
        }
    }

    void GameManager_GameStateChanged(GameState newState, GameState oldState)
    {
        if (newState == GameState.Playing)
        {
            ShowGameUI();
            isStartingGameProcess = false;
        }
        else if (newState == GameState.PreGameOver)
        {
            // Before game over, i.e. game potentially will be recovered
        }
        else if (newState == GameState.GameOver)
        {
            isStartingGameProcess = false;
            if (s.ilyas == false)
            {
                Invoke("ShowGameOverUI", 1f);
            }
        }
    }

    void OnScoreUpdated(int newScore)
    {
        if (scoreAnimator == null) return;

        if (score.gameObject.activeInHierarchy)
        {
            scoreAnimator.SetBool("NewScore", true);
        }
    }

    void Reset()
    {
        GameObject[] objectsToDeactivate = new GameObject[]
        {
        settingsUI, htpBtn, title.gameObject,
        score.gameObject, tapToStart, restartBtn, menuButtons, settingsUI,
        Donate, ds, dBtn, HTP, ShopUI2, ShopB, STOP, hy, lb, lbui,
        ab, abui, settingsUI, discordsw
        };

        foreach (GameObject obj in objectsToDeactivate)
        {
            if (obj != null) obj.SetActive(false);
        }

        stpBtn.SetActive(true);
        hy.SetActive(true);
        kb.SetActive(false);

        if (ScoreManager.Instance != null)
        {
            SB.SetActive(ScoreManager.Instance.IsSkillPurchased);

            bool isNight = ScoreManager.Instance.IsNightPurchased;
            if (dfbg != null) dfbg.SetActive(!isNight);
            if (night != null) night.SetActive(isNight);

            if (gameManager != null)
            {
                gameManager.equipment = ScoreManager.Instance.IsEquipmentPurchased;
                gameManager.multiply = ScoreManager.Instance.IsMultiplyPurchased;
                gameManager.shield = ScoreManager.Instance.IsShieldPurchased;
            }

            ScoreManager.Instance.scoreIsNotLocked = true;
        }
    }

    // You already have this flag, which is good. We will keep it.
    private bool isStartingGameProcess = false;

    public void StartGame()
    {
        if (!_uiInitDone || gameManager.GameState != GameState.Prepare || isStartingGameProcess)
            return;
        // Guard Clause: If the game is already running or we are already in the process of starting, exit.
        // This prevents the logic from running twice.
        if (gameManager.GameState == GameState.Playing || isStartingGameProcess)
        {
            return;
        }

        // --- THE CORE FIX ---
        // 1. Immediately set the logic flag to prevent re-entry.
        isStartingGameProcess = true;

        // 2. Immediately deactivate the "Tap to Start" GameObject.
        // This is the most crucial step. It instantly removes the source of the spam clicks.
        // Any queued-up clicks from the user will now have no object to interact with.
        if (tapToStart != null)
        {
            tapToStart.SetActive(false);
        }

        // As a good practice, you could also disable other main menu buttons here
        // if they could cause conflicts, but disabling tapToStart is the primary fix.
        // For example:
        // if (nextBtn != null) nextBtn.SetActive(false);
        // if (prevBtn != null) prevBtn.SetActive(false);


        // 3. Now, you can safely proceed with the rest of the game start logic.
        cs.audioSource.Stop();
        gameManager.StartGame(); // This will eventually trigger the GameStateChanged event and call ShowGameUI().
    }

    public void EndGame()
    {
        gameManager.GameOver();
    }

    public void RestartGame()
    {
        gameManager.RestartGame(0.2f);
    }

    public void ShowStartUI()
    {
        isStartingGameProcess = false;
        taptap.SetActive(false);
        Tt.SetActive(false);
        settingsUI.SetActive(false);
        htpBtn.SetActive(true);
        ShopUI2.SetActive(false);
        title.gameObject.SetActive(true);
        tapToStart.SetActive(true);
        nextBtn.SetActive(true);
        prevBtn.SetActive(true);
        newsBtn.SetActive(true);
        SB.SetActive(false);
        dBtn.SetActive(true);
        ShopB.SetActive(true);
        ShopUI.SetActive(false);
        gameManager.t.SetActive(false);
        ds.SetActive(false);
        discordsw.SetActive(false);
        menuButtons.SetActive(true);
        code.SetActive(true);
        hy.SetActive(false);
        STOP.SetActive(false);
        stpBtn.SetActive(false);
        lb.SetActive(true);
        ab.SetActive(true);
        kb.SetActive(true);
        kb.SetActive(true);
        kedi.SetActive(false);
        tapToStart.SetActive(true);
    }

    public void ShowGameUI()
    {
        stpBtn.GetComponent<Button>().interactable = true;
        title.gameObject.SetActive(false);
        score.gameObject.SetActive(true);
        tapToStart.SetActive(false);
        nextBtn.SetActive(false);
        prevBtn.SetActive(false);
        htpBtn.SetActive(false);
        dBtn.SetActive(false);
        ShopB.SetActive(false);
        newsBtn.SetActive(false);
        ds.SetActive(false);
        discordsw.SetActive(false);
        hy.SetActive(true);
        menuButtons.SetActive(false);
        code.SetActive(false);
        STOP.SetActive(false);
        stpBtn.SetActive(true);
        lb.SetActive(false);
        ab.SetActive(false);
        abui.SetActive(false);

        if (ScoreManager.Instance != null)
        {
            if (SB != null) SB.SetActive(ScoreManager.Instance.IsSkillPurchased);

            if (gameManager != null)
            {
                gameManager.equipment = ScoreManager.Instance.IsEquipmentPurchased;
                gameManager.multiply = ScoreManager.Instance.IsMultiplyPurchased;
                gameManager.shield = ScoreManager.Instance.IsShieldPurchased;
            }
        }

        kedi.SetActive(false);
        kb.SetActive(false);
        bb.SetActive(false);
        vb.SetActive(false);
    }

    // REPLACE your entire ShowGameOverUI function with this new, robust version.
    public void ShowGameOverUI()
    {
        // Handle ad logic
        if (ad == false && rw.ai == true)
        {
            bl.Ad();
        }
        ad = true;

        // Hide active game UI elements
        hy.SetActive(false);
        STOP.SetActive(false);
        stpBtn.GetComponent<Button>().interactable = false;

        // Animate UI elements using the BuneLean (bl) helper
        bl.cool(gameManager.t);
        bl.cool(gameManager.b);
        bl.cool(gameManager.crownb);
        bl.cool(gameManager.crownt);
        bl.cool(gameManager.diab);
        bl.cool(gameManager.diat);
        bl.cool(gameManager.ironb);
        bl.cool(gameManager.iront);
        bl.cool(gameManager.m);
        bl.cool(stpBtn);

        // Show the game over buttons
        bl.open(restartBtn);
        bl.open(discordsw);
        bl.open(htpBtn);
        bl.open(kedi);


        // --- FLAWLESS REVIVE BUTTON CHECK ---

        // 1. Determine the maximum number of revives allowed for the player.
        // The standard is 2, but if the Phoenix item is purchased, it becomes 3.
        int maxRevives = ScoreManager.Instance.IsPhoenixPurchased ? 3 : 2;

        // 2. Get the number of revives already used in this life from the GameManager's counter.
        int revivesUsed = gameManager.RevivesUsedThisLife;

        // 3. Log the current state for easy debugging.
        Debug.Log($"[UIManager] Game Over Screen: Max Revives allowed: {maxRevives}, Revives already used: {revivesUsed}");

        // 4. Show the revive button ('ds') ONLY if the 'ilyas' special skill is not active
        //    AND the number of used revives is LESS THAN the maximum allowed.
        if (s != null && !s.ilyas && revivesUsed < maxRevives)
        {
            Debug.Log("[UIManager] Player has revives remaining. Showing the revive button.");
            bl.open(ds);
        }
        else
        {
            Debug.Log("[UIManager] Player has no revives left or a special skill is active. Hiding the revive button.");
            if (ds != null) ds.SetActive(false); // Ensure the button is hidden if no revives are left.
        }
    }


    public void SetInitialLanguage()
    {
        if (!PlayerPrefs.HasKey("SelectedLanguage"))
        {
            SystemLanguage deviceLanguage = Application.systemLanguage;
            int languageIndex;

            if (deviceLanguage == SystemLanguage.Turkish)
            {
                languageIndex = 1;
            }
            else
            {
                languageIndex = 0;
            }

            PlayerPrefs.SetInt("SelectedLanguage", languageIndex);
            PlayerPrefs.Save();

            StartCoroutine(SetLocale(languageIndex));
        }
        else
        {
            int savedLanguageIndex = PlayerPrefs.GetInt("SelectedLanguage");
            StartCoroutine(SetLocale(savedLanguageIndex));
        }
    }


    IEnumerator SetLocale(int localeIndex)
    {
        yield return LocalizationSettings.InitializationOperation;
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[localeIndex];
        Debug.Log("Language set to: " + LocalizationSettings.SelectedLocale.LocaleName);

        UpdateLocalizedTexts();

        if (localeIndex == 0)
        {
            creditText.fontSize = 58;
        }
        else if (localeIndex == 1)
        {
            creditText.fontSize = 30;
        }
        else
        {
            creditText.fontSize = 40;
        }
    }

    public void ToggleLanguage()
    {
        int currentLanguage = PlayerPrefs.GetInt("SelectedLanguage", 0);

        int newLanguage = (currentLanguage == 0) ? 1 : 0;

        PlayerPrefs.SetInt("SelectedLanguage", newLanguage);
        PlayerPrefs.Save();

        StartCoroutine(SetLocale(newLanguage));
    }

    void UpdateLocalizedTexts()
    {
        var localizedStringEvent = GetComponent<LocalizeStringEvent>();
        if (localizedStringEvent != null)
        {
            localizedStringEvent.RefreshString();
        }
    }

    public void ShowSettingsUI()
    {
        settingsUI.SetActive(true);
        bl.Button(settingsUI, menuButtons);
    }

    public void HideSettingsUI()
    {
        settingsUI.SetActive(false);
    }

    public void ShowHTPUI()
    {
        bl.Button(HTP, htpBtn);
        HTP.SetActive(true);
    }

    public void HideHTPUI()
    {
        HTP.SetActive(false);
    }

    public void BGFN()
    {
        gameManager.RestartGame();
    }

    public void ShowCodeUI()
    {
        codeUI.SetActive(true);
        bl.Button(codeUI, code);
    }

    public void HideCodeUI()
    {
        ResetErrorText(promoCodeErrorText, ref promoCodeErrorCoroutine);
        codeUI.SetActive(false);
    }

    public void HideBGUI()
    {
        PlayerPrefs.SetInt(IS_BEGINNING_PLAYER, isBeginningPlayer ? 1 : 0);
        PlayerPrefs.Save();
        BgUI.SetActive(false);
    }

    public void StartTTL()
    {
        bl.StartTT();
        PlayerPrefs.SetInt(IS_BEGINNING_PLAYER, isBeginningPlayer ? 0 : 0);
        PlayerPrefs.Save();
    }

    public void Ge()
    {
        if (bl.ae == true)
        {
            bl.Phase6();
        }
        else if (bl.hm == true)
        {
            bl.Phase5();
        }
        else
        {
            bl.Phase2();
        }
    }

    public void ShowSHUI()
    {
        bl.Button(shUI, ds);
        shUI.SetActive(true);
        lbui.SetActive(false);
    }

    public void HideSHUI()
    {
        ResetErrorText(promoCodeErrorText, ref promoCodeErrorCoroutine);
        shUI.SetActive(false);
        lbui.SetActive(true);
        bl.Button(lbui, ds);
        LeanTween.scale(blglndrm, new Vector3(1.02f, 1.02f, 1f), 0.1f).setEase(LeanTweenType.easeInOutCubic).setOnComplete(() => { LeanTween.scale(blglndrm, new Vector3(1f, 1f, 1f), 0.1f).setEase(LeanTweenType.easeInOutCubic); });
        LeanTween.scale(otfh, new Vector3(1.02f, 1.02f, 1f), 0.1f).setEase(LeanTweenType.easeInOutCubic).setOnComplete(() => { LeanTween.scale(otfh, new Vector3(1f, 1f, 1f), 0.1f).setEase(LeanTweenType.easeInOutCubic); });
    }

    /// <summary>
    /// Called when the "Redeem Code" button is clicked.
    /// It retrieves the code and sends it securely to the server via AuthService.
    /// </summary>
    public async void OnClick_RedeemCode()
    {
        if (redeemCodeButton == null || !redeemCodeButton.interactable) return;

        string code = promoCodeInput.text.Trim();

        if (string.IsNullOrEmpty(code))
        {
            ShowPromoCodeFeedback(Lang == 1 ? "bi kod girmen lazım" : "you need to enter a code", isSuccess: false);
            return;
        }

        redeemCodeButton.interactable = false;

        try
        {
            RedeemCodeResponse response = await AuthService.RedeemPromoCodeAsync(code);

            if (response.IsSpecialUnlock)
            {
                Debug.Log($"[UIManager] Special code '{code}' is valid! Unlocking all items.");

                ScoreManager.Instance.PurchaseSkill(true);
                ScoreManager.Instance.PurchaseNight(true);
                ScoreManager.Instance.PurchaseEquipment(true);
                ScoreManager.Instance.PurchaseMultiply(true);
                ScoreManager.Instance.PurchasePhoenix(true);
                ScoreManager.Instance.PurchaseShield(true);

                ShowPromoCodeFeedback(Lang == 1 ? "gizli kod aktive edildi" : "special code activated", isSuccess: true);
            }
            else
            {
                ShowPromoCodeFeedback(Lang == 1 ? "kod başarıyla kullanıldı" : "code redeemed successfully", isSuccess: true);
            }

            promoCodeInput.text = "";
            Invoke(nameof(HideCodeUI), 2f);
        }
        catch (Exception e)
        {
            string errorMessage;
            switch (e.Message)
            {
                case "ERROR_CODE_INVALID":
                    errorMessage = Lang == 1 ? "bu kod geçersiz" : "this code is invalid";
                    break;
                case "ERROR_CODE_ALREADY_REDEEMED":
                    errorMessage = Lang == 1 ? "bu kodu zaten kullanmışsın" : "you've already used this code";
                    break;
                case "ERROR_NOT_SIGNED_IN":
                    errorMessage = Lang == 1 ? "kod için önce giriş yap" : "log in first to use a code";
                    break;
                default:
                    errorMessage = Lang == 1 ? "bi sorun çıktı" : "an error occurred";
                    break;
            }
            ShowPromoCodeFeedback(errorMessage, isSuccess: false);
        }
        finally
        {
            if (redeemCodeButton != null)
            {
                redeemCodeButton.interactable = true;
            }
        }
    }

    /// <summary>
    /// Displays feedback for the promo code UI.
    /// </summary>
    private void ShowPromoCodeFeedback(string message, bool isSuccess)
    {
        if (promoCodeErrorText == null) return;
        if (promoCodeErrorCoroutine != null) StopCoroutine(promoCodeErrorCoroutine);
        promoCodeErrorCoroutine = StartCoroutine(UniversalAnimateFeedbackText(promoCodeErrorText, message, isSuccess));
    }

    /// <summary>
    /// Displays feedback for the change password UI.
    /// </summary>
    private void ShowChangePasswordFeedback(string message, bool isSuccess)
    {
        if (changePasswordErrorText == null) return;
        if (changePasswordErrorCoroutine != null) StopCoroutine(changePasswordErrorCoroutine);
        changePasswordErrorCoroutine = StartCoroutine(UniversalAnimateFeedbackText(changePasswordErrorText, message, isSuccess));
    }

    /// <summary>
    /// Displays feedback for the change name UI.
    /// </summary>
    private void ShowChangeNameFeedback(string message, bool isSuccess)
    {
        if (changeNameErrorText == null) return;
        if (changeNameErrorCoroutine != null) StopCoroutine(changeNameErrorCoroutine);
        changeNameErrorCoroutine = StartCoroutine(UniversalAnimateFeedbackText(changeNameErrorText, message, isSuccess));
    }

    /// <summary>
    /// Displays feedback for authentication errors (login/signup). This is always an error.
    /// </summary>
    private void ShowAnimatedAuthError(string msg, bool isLoginError)
    {
        Text targetText = isLoginError ? loginErrorText : signUpErrorText;
        if (targetText == null)
        {
            Debug.LogError($"Cannot display auth error, the target Text object is not assigned in the UIManager inspector! (isLoginError: {isLoginError})");
            return;
        }

        ref Coroutine errorCoroutine = ref (isLoginError ? ref loginErrorCoroutine : ref signUpErrorCoroutine);
        if (errorCoroutine != null) StopCoroutine(errorCoroutine);

        // Always call with 'isSuccess: false' because this method is only for errors.
        errorCoroutine = StartCoroutine(UniversalAnimateFeedbackText(targetText, msg, false));
    }

    private void SetButtonExpireDate()
    {
        DateTime currentDate = DateTime.Now;

        buttonExpireDate = currentDate.AddDays(7);

        PlayerPrefs.SetString(buttonKey, buttonExpireDate.ToString());
        PlayerPrefs.Save();
    }

    public void ChangeNameReal()
    {
        abui.SetActive(false);
        acsUI.SetActive(false);
        ab.SetActive(true);
        SetButtonExpireDate();
    }

    #region Auth Error Handling & Display

    /// <summary>
    /// A public, centralized method to handle any authentication exception from anywhere in the app.
    /// It determines the correct error message and calls the animation coroutine.
    /// </summary>
    /// <param name="ex">The exception that occurred.</param>
    /// <param name="isLoginError">True if the error came from the login form, false if from sign-up.</param>
    public void HandleAuthException(Exception ex, bool isLoginError)
    {
        string errorMessage;
        if (ex is Firebase.FirebaseException firebaseEx)
        {
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
            switch (errorCode)
            {
                case AuthError.WrongPassword:
                    errorMessage = Lang == 1 ? "parola yanlış kanka" : "wrong password bro";
                    break;
                case AuthError.UserNotFound:
                    errorMessage = Lang == 1 ? "bu postada hesap yok" : "no account with this email";
                    break;
                case AuthError.InvalidEmail:
                    errorMessage = Lang == 1 ? "geçerli bi posta gir" : "enter a valid email";
                    break;
                case AuthError.WeakPassword:
                    errorMessage = Lang == 1 ? "şifre en az 6 karakter olmalı" : "password must be at least 6 characters";
                    break;
                case AuthError.EmailAlreadyInUse:
                    errorMessage = Lang == 1 ? "bu posta zaten kapılmış" : "this email is already taken";
                    break;
                case AuthError.NetworkRequestFailed:
                    errorMessage = Lang == 1 ? "internet gitti galiba" : "check your internet connection";
                    break;
                case AuthError.Failure when isLoginError:
                    errorMessage = Lang == 1 ? "e-posta ya da parola hatalı" : "email or password is wrong";
                    break;
                default:
                    errorMessage = Lang == 1 ? "bir sorun çıktı" : "an error occurred";
                    break;
            }
        }
        else
        {
            // These are custom exceptions from AuthService
            switch (ex.Message)
            {
                case "ERROR_USERNAME_FORMAT":
                    errorMessage = Lang == 1 ? "kullanıcı adı formatı yanlış" : "invalid username format";
                    break;
                case "ERROR_USERNAME_TAKEN":
                    errorMessage = Lang == 1 ? "bu kullanıcı adını kapmışlar" : "this username is taken";
                    break;
                case "ERROR_ALL_FIELDS_REQUIRED":
                    errorMessage = Lang == 1 ? "tüm alanları doldurman lazım" : "all fields are required";
                    break;
                case "ERROR_EMAIL_PASSWORD_REQUIRED":
                    errorMessage = Lang == 1 ? "e-posta ve şifre girmelisin" : "email and password are required";
                    break;
                default:
                    // For any other unhandled custom exception
                    errorMessage = Lang == 1 ? "bi sorun çıktı" : "an error occurred";
                    break;
            }
        }
        ShowAnimatedAuthError(errorMessage, isLoginError);
    }

    // Add this single, centralized method to UIManager.cs.
    /// <summary>
    /// A centralized coroutine to display animated feedback (both success and error) to the user.
    /// </summary>
    /// <param name="feedbackText">The UI Text element to animate.</param>
    /// <param name="message">The message to display.</param>
    /// <param name="isSuccess">Determines the color and animation style (true for green/still, false for red/shake).</param>
    private IEnumerator UniversalAnimateFeedbackText(Text feedbackText, string message, bool isSuccess)
    {
        // --- SAFETY STEP 1: Clear existing animations ---
        // This stops all running LeanTween animations on this Text object. PREVENTS SPAM BUILDUP.
        LeanTween.cancel(feedbackText.rectTransform);

        // --- SAFETY STEP 2: Save and reset the original state ---
        // If we haven't stored the original state of this Text object before, do it now.
        if (!originalChangeNameTexts.ContainsKey(feedbackText))
        {
            originalChangeNameTexts[feedbackText] = feedbackText.text;
            originalChangeNameColors[feedbackText] = feedbackText.color;
            originalTextPositions[feedbackText] = feedbackText.rectTransform.anchoredPosition;
        }

        // Instantly reset the position to its original state before every animation. PREVENTS DRIFTING.
        feedbackText.rectTransform.anchoredPosition = originalTextPositions[feedbackText];

        // --- START THE ANIMATION ---
        feedbackText.gameObject.SetActive(true);
        feedbackText.text = message;

        // --- THE CORE FIX: CHOOSE THE RIGHT COLOR ---
        // Select the color based on whether the message is a success or an error.
        feedbackText.color = isSuccess ? successColor : errorColor;

        // Only perform the "shake" animation for error messages. Success messages remain still.
        if (!isSuccess)
        {
            var sequence = LeanTween.sequence();
            sequence.append(LeanTween.moveX(feedbackText.rectTransform, originalTextPositions[feedbackText].x + 7f, 0.08f));
            sequence.append(LeanTween.moveX(feedbackText.rectTransform, originalTextPositions[feedbackText].x - 7f, 0.16f));
            sequence.append(LeanTween.moveX(feedbackText.rectTransform, originalTextPositions[feedbackText].x, 0.08f));
        }

        // Wait for a set duration before fading out.
        yield return new WaitForSeconds(3.0f);

        // --- FINISH AND CLEAN UP THE ANIMATION ---
        // If the coroutine was not interrupted, fade the text back to its original color and content.
        LeanTween.colorText(feedbackText.rectTransform, originalChangeNameColors[feedbackText], 0.5f).setEaseOutQuad();
        feedbackText.text = originalChangeNameTexts[feedbackText];

        // Clear the corresponding coroutine reference to indicate that it has finished.
        if (feedbackText == changePasswordErrorText) changePasswordErrorCoroutine = null;
        if (feedbackText == changeNameErrorText) changeNameErrorCoroutine = null;
        if (feedbackText == promoCodeErrorText) promoCodeErrorCoroutine = null;
        if (feedbackText == loginErrorText) loginErrorCoroutine = null;
        if (feedbackText == signUpErrorText) signUpErrorCoroutine = null;
    }
    // Add this helper function to UIManager.cs.
    private void ResetErrorText(Text errorText, ref Coroutine errorCoroutine)
    {
        if (errorText == null) return;

        // 1. Stop the running animation coroutine.
        if (errorCoroutine != null)
        {
            StopCoroutine(errorCoroutine);
            errorCoroutine = null;
        }

        // 2. Instantly cancel any active LeanTween animations on this Text object.
        LeanTween.cancel(errorText.rectTransform);

        // 3. If we have a stored original state for this Text, instantly revert to it.
        if (originalChangeNameTexts.ContainsKey(errorText))
        {
            errorText.text = originalChangeNameTexts[errorText];
            errorText.color = originalChangeNameColors[errorText];
            errorText.rectTransform.anchoredPosition = originalTextPositions[errorText];
        }
    }

    public async void OnClick_Login()
    {
        try
        {
            // Note: OnRefreshStarted() is not called here anymore, the UIManager should
            // handle its own loading indicators if needed.
            bool success = await AuthService.UILoginAsync(loginEmailInput.text, loginPasswordInput.text);
            if (success)
            {
                OnAuthenticationSuccess();
            }
        }
        catch (Exception e)
        {
            HandleAuthException(e, isLoginError: true);
        }
    }

    public async void OnClick_CreateAccount()
    {
        try
        {
            // Step 1: Call the registration logic, which now returns the new user.
            FirebaseUser newUser = await AuthService.UIRegisterAsync(signUpUsernameInput.text, signUpEmailInput.text, signUpPasswordInput.text);

            if (newUser == null)
            {
                // This case should be rare, but it's a good safeguard.
                throw new Exception("User creation failed unexpectedly.");
            }

            // --- THE FLAWLESS FIX - MIRRORING FLUTTER'S LOGIC ---
            // Step 2: Wait for the server-side 'onUserCreate' function to finish.
            await AuthService.EnsureUserDocumentExistsAsync(newUser.UserId);

            // Step 3: Now that the document is confirmed, fetch the username for local storage.
            // This handles the case where the server might modify the suggested username.
            string finalUsername = await AuthService.GetUsernameFromProfileAsync(newUser.UserId);
            if (!string.IsNullOrEmpty(finalUsername))
            {
                PlayerPrefs.SetString("SavedUsername", finalUsername);
                PlayerPrefs.Save();
                Debug.Log($"[UIManager] User profile for '{finalUsername}' confirmed. Saving to PlayerPrefs.");
            }

            // Step 4: ONLY NOW is it safe to proceed to the next UI screen.
            OnAuthenticationSuccess();
        }
        catch (Exception e)
        {
            // The existing robust error handler will catch any issues.
            HandleAuthException(e, isLoginError: false);
        }
    }

    #endregion

    public void ShowACUI()
    {
        FirebaseUser currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        bool hasInternet = Application.internetReachability != NetworkReachability.NotReachable;

        if (currentUser == null)
        {
            Debug.Log("[UIManager] ShowACUI: No user is signed in. Showing the login/signup panel.");
            UpdateGoogleSignInButtons(hasInternet);
            abui.SetActive(true);
            bl.Button(abui, ab);
            return;
        }

        UpdateAccountSettingsButtons(hasInternet);

        bool needsVerification = false;
        foreach (var provider in currentUser.ProviderData)
        {
            if (provider.ProviderId == "password" && !currentUser.IsEmailVerified)
            {
                needsVerification = true;
                break;
            }
        }

        if (needsVerification)
        {
            Debug.Log("[UIManager] ShowACUI: User is unverified. Preparing and showing verification panel.");

            Action onVerificationPanelClosed = () =>
            {
                StartCoroutine(TransitionFromVerificationToSettings());
            };

            verificationController?.PreparePanel(currentUser, onVerificationPanelClosed);
            if (verificationUI != null) verificationUI.SetActive(true);
            bl.Button(verificationUI, ab);
        }
        else
        {
            Debug.Log("[UIManager] ShowACUI: User is verified. Showing account settings directly.");

            bl.Button(acsUI, ab);

            ac.text = AuthService.Username;
            if (ScoreManager.Instance != null)
            {
                bs.text = ScoreManager.Instance.HighScore.ToString();
                totalscore.text = ScoreManager.Instance.TotalScore.ToString();
            }
        }
    }

    private IEnumerator InternetCheckRoutine()
    {
        while (true)
        {
            bool hasInternet = Application.internetReachability != NetworkReachability.NotReachable;

            if (acsUI != null && acsUI.activeInHierarchy)
            {
                UpdateAccountSettingsButtons(hasInternet);
            }

            if (abui != null && abui.activeInHierarchy)
            {
                UpdateGoogleSignInButtons(hasInternet);
            }

            yield return new WaitForSeconds(2f);
        }
    }

    private void UpdateAccountSettingsButtons(bool isOnline)
    {
        if (changeNameButton != null) changeNameButton.SetActive(isOnline);
        if (deleteAccountButton != null) deleteAccountButton.SetActive(isOnline);
        if (logoutAccountButton != null) logoutAccountButton.SetActive(isOnline);
    }

    private void UpdateGoogleSignInButtons(bool isOnline)
    {
        if (googleSignInButtons == null) return;

        foreach (var button in googleSignInButtons)
        {
            if (button != null)
            {
                button.interactable = isOnline;
            }
        }
    }

    private IEnumerator TransitionFromVerificationToSettings()
    {
        Debug.Log("[UIManager] Starting transition from Verification to Settings...");

        verificationUI.SetActive(false);

        yield return null;

        Debug.Log("[UIManager] Transition frame delay finished. Opening ACS panel.");
        acsUI.SetActive(true);
        bl.Button(acsUI, null);

        FirebaseUser currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        if (currentUser != null)
        {
            ac.text = AuthService.Username;
            if (ScoreManager.Instance != null)
            {
                bs.text = ScoreManager.Instance.HighScore.ToString();
                totalscore.text = ScoreManager.Instance.TotalScore.ToString();
            }
        }
    }

    public void HideACUI()
    {
        ResetErrorText(promoCodeErrorText, ref promoCodeErrorCoroutine);
        if (isNeymPurchased == false && nc == false)
        {
            abui.SetActive(false);
        }
        if (isNeymPurchased == true && nc == false)
        {
            abui.SetActive(false);
            acsUI.SetActive(true);
            bl.Button(acsUI, ds);
        }
    }

    public void ShowResetPasswordUI()
    {
        if (resetPasswordUI != null)
        {
            resetPasswordUI.SetActive(true);
            acsUI.SetActive(false);
            bl.Button(resetPasswordUI, null);
        }
    }

    public void HideResetPasswordUI()
    {
        ResetErrorText(promoCodeErrorText, ref promoCodeErrorCoroutine);
        if (resetPasswordUI != null)
        {
            resetPasswordUI.SetActive(false);
            acsUI.SetActive(true);
            bl.Button(acsUI, null);
        }
    }

    public async void ChangePasswordButtonClicked()
    {
        if (confirmChangePasswordButton == null || !confirmChangePasswordButton.interactable)
        {
            return;
        }

        string oldPass = oldPasswordInput.text;
        string newPass = newPasswordInput.text;
        string confirmPass = confirmNewPasswordInput.text;

        if (string.IsNullOrEmpty(oldPass) || string.IsNullOrEmpty(newPass) || string.IsNullOrEmpty(confirmPass))
        {
            ShowChangePasswordFeedback(Lang == 1 ? "tüm alanları doldur kanzi" : "all fields are required bro", isSuccess: false);
            return;
        }

        if (newPass != confirmPass)
        {
            ShowChangePasswordFeedback(Lang == 1 ? "yeni şifreler uyuşmuyo" : "new passwords do not match", isSuccess: false);
            return;
        }

        if (newPass.Length < 6)
        {
            ShowChangePasswordFeedback(Lang == 1 ? "şifren en az 6 karakter olmalı" : "password must be at least 6 characters", isSuccess: false);
            return;
        }

        if (oldPass == newPass)
        {
            ShowChangePasswordFeedback(Lang == 1 ? "yeni şifren eskisiyle aynı olamaz" : "new password cannot be the same as the old one", isSuccess: false);
            return;
        }

        confirmChangePasswordButton.interactable = false;

        try
        {
            await AuthService.ChangePasswordAsync(oldPass, newPass);

            oldPasswordInput.text = "";
            newPasswordInput.text = "";
            confirmNewPasswordInput.text = "";

            ShowChangePasswordFeedback(Lang == 1 ? "şifren başarıyla değişti" : "password changed successfully", isSuccess: true);

            Invoke(nameof(HideResetPasswordUI), 2f);
        }
        catch (Exception e)
        {
            HandleChangePasswordException(e);
        }
        finally
        {
            if (confirmChangePasswordButton != null)
            {
                confirmChangePasswordButton.interactable = true;
            }
        }
    }

    private void HandleChangePasswordException(Exception ex)
    {
        string errorMessage;
        var baseException = ex.GetBaseException();

        if (baseException is Firebase.FirebaseException firebaseEx)
        {
            Debug.LogError($"[UIManager] Firebase Exception on Password Change: '{firebaseEx.Message}' (Code: {firebaseEx.ErrorCode})");

            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
            switch (errorCode)
            {
                case AuthError.WrongPassword:
                    errorMessage = Lang == 1 ? "eski şifren yanlış kanki" : "incorrect old password bro";
                    break;
                case AuthError.WeakPassword:
                    errorMessage = Lang == 1 ? "şifren çok zayıf, daha güçlüsünü dene" : "password is too weak, try a stronger one";
                    break;
                case AuthError.NetworkRequestFailed:
                    errorMessage = Lang == 1 ? "internet bağlantısı kurulamadı" : "could not connect to the internet";
                    break;
                default:
                    errorMessage = Lang == 1 ? "bir sunucu hatası oluştu" : "a server error occurred";
                    break;
            }
        }
        else
        {
            switch (ex.Message)
            {
                case "ERROR_NOT_SIGNED_IN":
                    errorMessage = Lang == 1 ? "bu işlem için giriş yapman lazım" : "you must be signed in for this operation";
                    break;
                case "ERROR_PROVIDER_NO_PASSWORD":
                    errorMessage = Lang == 1 ? "google ile giriş yaptığın için şifre değiştiremezsin" : "you cannot change password when signed in with Google";
                    break;
                default:
                    Debug.LogError($"[UIManager] Unhandled Custom Exception: {ex.Message}");
                    errorMessage = Lang == 1 ? "bi sorun çıktı" : "an error occurred";
                    break;
            }
        }
        ShowChangePasswordFeedback(errorMessage, isSuccess: false);
    }

    public void HideChangeNameUI()
    {
        ResetErrorText(promoCodeErrorText, ref promoCodeErrorCoroutine);
        changeNameUI.SetActive(false);
        bl.Button(acsUI, null);
    }

    public void ShowNewsUI()
    {
        // This is the existing logic to open the panel.
        bl.Button(newsUI, newsBtn);
        newsUI.SetActive(true);

        // After showing the panel, check if the news data has been loaded.
        // If not (e.g., due to a previous network error), trigger a manual refresh.
        if (vertexNews != null && !vertexNews.IsDataLoaded)
        {
            Debug.Log("[UIManager] News data is not loaded. Requesting a refresh from VertexNews.");
            vertexNews.ForceRefresh();
        }
    }

    public void ShowLoginUI()
    {
        bl.Button(loginUI, loginBtn);
        loginUI.SetActive(true);
        abui.SetActive(false);
    }

    public void HideLoginUI()
    {
        ResetErrorText(promoCodeErrorText, ref promoCodeErrorCoroutine);
        loginUI.SetActive(false);
    }

    public void ShowSignUpUI()
    {
        bl.Button(abui, signUpBtn);
        loginUI.SetActive(false);
        abui.SetActive(true);
    }

    public void ShowLogOutUI()
    {
        bl.Button(logoutUI, logoutButton);
        acsUI.SetActive(false);
        logoutUI.SetActive(true);
    }

    public void HideLogOutUI()
    {
        ResetErrorText(promoCodeErrorText, ref promoCodeErrorCoroutine);
        bl.Button(acsUI, null);
        logoutUI.SetActive(false);
    }

    public void LogoutUser()
    {
        Debug.Log("[UIManager] Logout button clicked. Signing out...");
        logoutUI.SetActive(false);
        AuthService.Logout();
    }


    public void BgChange()
    {
        ScoreManager.Instance.ToggleBackgroundPreference();
    }

    public void HideACSUI()
    {
        ResetErrorText(promoCodeErrorText, ref promoCodeErrorCoroutine);
        acsUI.SetActive(false);
    }

    public void HideNewsUI()
    {
        ResetErrorText(promoCodeErrorText, ref promoCodeErrorCoroutine);
        newsUI.SetActive(false);
    }

    public void SureOp()
    {
        sureUI.SetActive(true);
        acsUI.SetActive(false);
        bl.Button(sureUI, ds);
    }

    public void SureCl()
    {
        sureUI.SetActive(false);
        acsUI.SetActive(true);
        bl.Button(acsUI, ds);
    }

    public void aeae()
    {
        sureUI.SetActive(false);
        bl.Button(acsUI, null);
    }

    public async void DeleteAccount()
    {
        Debug.Log("[UIManager] User confirmed deletion. Calling secure server-side request function.");

        try
        {
            // Call the modern, secure AuthService method. It doesn't need a password.
            await AuthService.RequestAccountDeletionAsync();

            Debug.Log("[UIManager] Account deletion request sent successfully. UI will now reset.");

            // On success, hide the panels and let the logout logic handle the rest.
            sureUI.SetActive(false);
            acsUI.SetActive(false);
            // OnAuthenticationSuccess() or a similar method could be called to show the main login screen.
        }
        catch (Exception e)
        {
            Debug.LogError($"[UIManager] Account deletion request failed: {e.Message}");
            // Use your existing error display to show a generic error.
            ShowPromptError(Lang == 1 ? "silme işlemi başarısız oldu" : "deletion request failed");
        }
    }

    private void ShowPromptError(string message)
    {
        if (promptErrorCoroutine != null)
        {
            StopCoroutine(promptErrorCoroutine);
        }
        promptErrorCoroutine = StartCoroutine(PromptErrorRoutine(message));
    }

    private IEnumerator PromptErrorRoutine(string errorMessage)
    {
        var localizeEvent = passwordPromptText.GetComponent<LocalizeStringEvent>();
        if (localizeEvent == null)
        {
            originalPromptText = passwordPromptText.text;
        }

        passwordPromptText.text = errorMessage;
        LeanTween.cancel(passwordPromptText.gameObject);
        LeanTween.colorText(passwordPromptText.rectTransform, errorColor, 0.2f);

        LeanTween.moveX(passwordPromptText.rectTransform, passwordPromptText.rectTransform.anchoredPosition.x + 7f, 0.4f)
            .setEase(LeanTweenType.punch)
            .setLoopPingPong(1);

        yield return new WaitForSeconds(2.5f);

        if (localizeEvent != null)
        {
            localizeEvent.RefreshString();
        }
        else
        {
            passwordPromptText.text = originalPromptText;
        }

        LeanTween.colorText(passwordPromptText.rectTransform, originalPromptColor, 0.5f).setEaseOutQuad();

        promptErrorCoroutine = null;
    }

    public async void ChangeNameButtonClicked()
    {
        if (confirmChangeNameButton == null || !confirmChangeNameButton.interactable)
        {
            return;
        }

        string newName = newUsernameInput.text.Trim();

        confirmChangeNameButton.interactable = false;

        try
        {
            // Call the new AuthService method which only takes ONE argument.
            await AuthService.ChangeUsernameAsync(newName);

            Debug.Log("[UIManager] Username change successful via UI.");
            ac.text = newName; // Update the display name in the account settings panel

            // Show a success message
            ShowChangeNameFeedback(Lang == 1 ? "ismin başarıyla değiştirildi!" : "name changed successfully!", isSuccess: true);

            newUsernameInput.text = "";

            // Optional: Hide the panel after a short delay on success
            Invoke(nameof(HideChangeNameUI), 2f);
        }
        catch (Exception e)
        {
            // The existing exception handler will catch errors from the Cloud Function
            // like "already-exists" or "resource-exhausted" (cooldown).
            HandleChangeNameException(e);
        }
        finally
        {
            if (confirmChangeNameButton != null)
            {
                confirmChangeNameButton.interactable = true;
            }
        }
    }

    private void HandleChangeNameException(Exception ex)
    {
        string errorMessage;
        var baseException = ex.GetBaseException();

        if (baseException is Firebase.FirebaseException firebaseEx)
        {
            string exceptionMessage = firebaseEx.Message.ToLower();
            Debug.LogError($"[UIManager] Firebase Exception Caught: '{exceptionMessage}' (Code: {firebaseEx.ErrorCode})");

            if (exceptionMessage.Contains("password"))
            {
                errorMessage = Lang == 1 ? "parolan yanlış kanki" : "incorrect password bro";
            }
            else if (exceptionMessage.Contains("permission"))
            {
                errorMessage = Lang == 1 ? "bu işlemi yapma iznin yok" : "you do not have permission for this";
            }
            else if (exceptionMessage.Contains("network"))
            {
                errorMessage = Lang == 1 ? "internete bağlanamadık" : "could not connect to the internet";
            }
            else
            {
                errorMessage = Lang == 1 ? "bir sunucu hatası oluştu" : "a server error occurred";
            }
        }
        else
        {
            switch (ex.Message)
            {
                case "ERROR_PASSWORD_REQUIRED":
                    errorMessage = Lang == 1 ? "ismini değiştirmek için şifreni girmelisin" : "you must enter your password to change your name";
                    break;
                case "ERROR_COOLDOWN_ACTIVE":
                    errorMessage = Lang == 1 ? "ismini daha yeni değiştirdin 7 gün beklemen lazım" : "you changed your name recently so you must wait 7 days";
                    break;
                case "ERROR_USERNAME_FORMAT":
                    errorMessage = Lang == 1 ? "kullanıcı adı 3-20 karakter olmalı ve harf/rakam içermeli (.-_ hariç)" : "username must be 3-20 chars and contain only letters/numbers (except .-_)";
                    break;
                case "ERROR_USERNAME_TAKEN":
                    errorMessage = Lang == 1 ? "bu kullanıcı adını kapmışlar" : "this username is already taken";
                    break;
                case "ERROR_NOT_SIGNED_IN":
                    errorMessage = Lang == 1 ? "bu işlem için giriş yapman lazım" : "you must be signed in for this operation";
                    break;
                default:
                    Debug.LogError($"[UIManager] Unhandled Custom Exception: {ex.Message}");
                    errorMessage = Lang == 1 ? "bi sorun çıktı" : "an error occurred";
                    break;
            }
        }

        ShowChangeNameFeedback(errorMessage, isSuccess: false);
    }


    #region Seasonal Reward Display

    /// <summary>
    /// Displays a celebratory panel notifying the user of their seasonal reward.
    /// The user must manually dismiss the panel by clicking on it.
    /// </summary>
    /// <param name="rewardLevel">The tier of the reward (7, 8, or 9).</param>
    public void ShowSeasonRewardPanel(int rewardLevel)
    {
        if (seasonRewardPanel == null || rewardText == null)
        {
            Debug.LogError("[UIManager] Cannot show reward panel because UI references are not set in the Inspector!");
            return;
        }

        string rewardTierName;
        switch (rewardLevel)
        {
            case 7: rewardTierName = "Cortex Plus"; break;
            case 8: rewardTierName = "Cortex Pro"; break;
            case 9: rewardTierName = "Cortex Ultra"; break;
            default:
                Debug.LogWarning($"[UIManager] Received an unknown reward level: {rewardLevel}");
                rewardTierName = "a Special Reward";
                break;
        }

        if (Lang == 1) // Turkish
        {
            rewardText.text = $"Tebrikler!\n\nÖnceki sezondaki üstün başarın sayesinde\n<b>{rewardTierName}</b> kazandın!";
        }
        else // English
        {
            rewardText.text = $"Congratulations!\n\nFor your outstanding performance last season, you have been awarded\n<b>{rewardTierName}</b>!";
        }

        // --- SIMPLIFIED LOGIC ---
        // Make the entire panel clickable to dismiss it.
        // We get the Button component that should be on the panel itself.
        Button panelButton = seasonRewardPanel.GetComponent<Button>();
        if (panelButton != null)
        {
            // Set up the listener to call HideSeasonRewardPanel when clicked.
            panelButton.onClick.RemoveAllListeners();
            panelButton.onClick.AddListener(HideSeasonRewardPanel);
        }
        else
        {
            // This warning helps in debugging if the panel isn't clickable.
            Debug.LogWarning("[UIManager] For dismissal, the 'seasonRewardPanel' GameObject must have a Button component attached to it.");
        }

        // Animate and show the panel using your helper.
        bl.Button(seasonRewardPanel, null);
        seasonRewardPanel.SetActive(true);
    }

    /// <summary>
    /// Hides the seasonal reward panel. This is called by the panel's own Button component.
    /// </summary>
    public void HideSeasonRewardPanel()
    {
        if (seasonRewardPanel != null)
        {
            seasonRewardPanel.SetActive(false);
        }
    }

    #endregion

    public void season()
    {
        ScoreManager.Instance.ResetForNewSeason();
        ScoreManager.Instance.ResetCurrentGameScore();
    }

    public void OnAuthenticationSuccess()
    {
        Debug.Log("[UIManager] Authentication successful. Closing auth panels and routing to the next step.");

        if (loginUI != null) loginUI.SetActive(false);
        if (abui != null) abui.SetActive(false);

        ShowACUI();
    }

    public void PurchaseName()
    {
        if (bl.ttbl == true)
        {
            isNeymPurchased = true;
            PlayerPrefs.SetInt(IS_NEYM_PURCHASED, isNeymPurchased ? 1 : 0);
            PlayerPrefs.Save();
            FindFirstObjectByType<LeaderboardSwitcher>()?.ShowSeason();
            bl.Phase4();
        }
        else
        {
            isNeymPurchased = true;
            abui.SetActive(false);
            PlayerPrefs.SetInt(IS_NEYM_PURCHASED, isNeymPurchased ? 1 : 0);
            PlayerPrefs.Save();
            ab.SetActive(true);
            ShowACUI();
            OnAuthenticationSuccess();
        }
    }

    public void ChangeName()
    {
        acsUI.SetActive(false);
        bl.Button(changeNameUI, ds);
    }

    public void PurchaseSkill()
    {
        ScoreManager.Instance.PurchaseSkill();
    }


    public void PurchaseNight()
    {
        ScoreManager.Instance.PurchaseNight();
        if (ScoreManager.Instance.IsNightPurchased && !ScoreManager.Instance.IsNightBackgroundActive)
        {
            ScoreManager.Instance.ToggleBackgroundPreference();
        }
    }

    public void PurchaseEquipment()
    {
        ScoreManager.Instance.PurchaseEquipment();
    }

    public void PurchaseMultiply()
    {
        ScoreManager.Instance.PurchaseMultiply();
    }

    public void PurchaseShield()
    {
        ScoreManager.Instance.PurchaseShield();
    }

    public void PurchasePhoenix()
    {
        ScoreManager.Instance.PurchasePhoenix();
    }

    public void OpenYouTubeChannel()
    {
        Application.OpenURL("https://youtube.com/@Vertex_Games");
        ScoreManager.Instance.PurchaseEquipment(true);
        StartCoroutine(dursunzamandursun());
    }

    public IEnumerator dursunzamandursun()
    {
        yield return new WaitForSeconds(1);
        equipmentyt.SetActive(false);
        alinditextee.SetActive(true);
        alinditexte.SetActive(true);
        equipmentpuan.SetActive(false);
    }

    public void Open(string URL)
    {
        Application.OpenURL(URL);
    }

    public void OpenCloverFP()
    {
        Application.OpenURL("https://open.spotify.com/track/5Bd0tXlRwZ3xI5KjlBE5dd?si=c04f87ff341f43aa");
        StartCoroutine(dursunzamandursundursun());

        ScoreManager.Instance.PurchaseShield(true);
    }
    public IEnumerator dursunzamandursundursun()
    {
        yield return new WaitForSeconds(5);
        shieldsp.SetActive(false);
        alinditextss.SetActive(true);
        alinditexts.SetActive(true);
        shieldpuan.SetActive(false);
    }

    public void OpenDiscordFP()
    {
        Application.OpenURL(serverURL);
        StartCoroutine(dursunzamandursundursundursun());

        ScoreManager.Instance.PurchaseNight(true);
    }
    public IEnumerator dursunzamandursundursundursun()
    {
        yield return new WaitForSeconds(5);
        nightpuan.SetActive(false);
        alinditextn.SetActive(true);
        bchngBtn.SetActive(true);
    }

    public void OpenDiscordServer()
    {
        Application.OpenURL(serverURL);
    }

    public void ShowBBUI()
    {
        bl.Button(bbUI, bb);
        bbUI.SetActive(true);
    }

    public void HideBBUI()
    {
        bbUI.SetActive(false);
    }

    public void ShowDUI()
    {
        bl.Button(Donate, dBtn);
        Donate.SetActive(true);
    }

    public void HideDUI()
    {
        Donate.SetActive(false);
    }

    public void ShowSUI()
    {
        bl.Button(ShopUI, ShopB);
        ShopUI.SetActive(true);
    }

    public void HideSUI()
    {
        ShopUI.SetActive(false);
    }

    public void ShowSTOPUI()
    {
        STOP.SetActive(true);
        stpBtn.SetActive(false);
        Time.timeScale = 0;
    }

    public void HideSTOPUI()
    {
        STOP.SetActive(false);
        stpBtn.SetActive(true);
        Time.timeScale = 1;
    }

    public void ShowSHOPUI2()
    {
        ShopUI2.SetActive(true);
        bl.Button(ShopUI2, ds);
        ShopUI.SetActive(false);
    }

    public void HideSHOPUI2()
    {
        ShopUI2.SetActive(false);
        ShopUI.SetActive(true);
        bl.Button(ShopUI, ds);
    }

    public void HideSHOPUI2C()
    {
        ShopUI2.SetActive(false);
    }

    public void ShowLBUI()
    {
        lbui.SetActive(true);
        bl.Button(lbui, lb);
        FindFirstObjectByType<LeaderboardSwitcher>()?.ShowSeason();

        LeanTween.scale(blglndrm, new Vector3(1.02f, 1.02f, 1f), 0.1f)
                 .setEase(LeanTweenType.easeInOutCubic)
                 .setOnComplete(() =>
                 {
                     LeanTween.scale(blglndrm, Vector3.one, 0.1f)
                              .setEase(LeanTweenType.easeInOutCubic);
                 });
        LeanTween.scale(otfh, new Vector3(1.02f, 1.02f, 1f), 0.1f)
                 .setEase(LeanTweenType.easeInOutCubic)
                 .setOnComplete(() =>
                 {
                     LeanTween.scale(otfh, Vector3.one, 0.1f)
                              .setEase(LeanTweenType.easeInOutCubic);
                 });
    }

    public void HideLBUI()
    {
        ResetErrorText(promoCodeErrorText, ref promoCodeErrorCoroutine);
        lbui.SetActive(false);
    }

    public void ShowKAMPANYAUI()
    {
        kampanyaui.SetActive(true);
        bl.Button(kampanyaui, kb);
        LeanTween.scale(kampanyaui, new Vector3(1.02f, 1.02f, 1f), 0.1f).setEase(LeanTweenType.easeInOutCubic).setOnComplete(() => { LeanTween.scale(kampanyaui, new Vector3(1f, 1f, 1f), 0.1f).setEase(LeanTweenType.easeInOutCubic); });
    }

    public void HideKAMPANYAUI()
    {
        kampanyaui.SetActive(false);
    }

    public void CheckForVerification()
    {
        FirebaseUser currentUser = FirebaseAuth.DefaultInstance.CurrentUser;

        Action onVerificationFinished = () =>
        {
            Debug.Log("[UIManager] Verification panel (if shown) was closed from startup flow.");
            if (verificationUI != null)
            {
                verificationUI.SetActive(false);
            }
        };

        bool isEmailPasswordProvider = false;
        if (currentUser != null)
        {
            foreach (var provider in currentUser.ProviderData)
            {
                if (provider.ProviderId == "password")
                {
                    isEmailPasswordProvider = true;
                    break;
                }
            }
        }

        if (currentUser != null && isEmailPasswordProvider && !currentUser.IsEmailVerified)
        {
            Debug.Log("[UIManager] Unverified user detected on startup. Showing verification panel.");

            verificationController?.PreparePanel(currentUser, onVerificationFinished);

            if (verificationUI != null)
            {
                verificationUI.SetActive(true);
            }
        }
        else
        {
            Debug.Log("[UIManager] No unverified user on startup, or user is already verified.");
        }
    }

    public void ToggleSound()
    {
        SoundManager.Instance.ToggleMute();
    }

    public void RateApp()
    {
        Application.OpenURL("https://play.google.com/store/apps/details?id=com.VertexGames.AllStar");
    }

    public void OpenTwitterPage()
    {
        string instagramURL = "https://www.instagram.com/vertex.23/";
        Application.OpenURL(instagramURL);
    }

    public void ButtonClickSound()
    {
        Utilities.ButtonClickSound();
    }

    void UpdateMuteButtons()
    {
        if (SoundManager.Instance.IsMuted())
        {
            soundOnBtn.gameObject.SetActive(false);
            soundOffBtn.gameObject.SetActive(true);
        }
        else
        {
            soundOnBtn.gameObject.SetActive(true);
            soundOffBtn.gameObject.SetActive(false);
        }
    }
}