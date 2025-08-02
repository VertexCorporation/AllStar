using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.Localization.Settings;

public enum GameState
{
    Prepare,
    Playing,
    Paused,
    PreGameOver,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static event System.Action<GameState, GameState> GameStateChanged = delegate { };
    public SelectCharacter cs;
    ScoreManager sm;
    public UIManager ui;
    public Rewarded rw;
    public VertexWobble textVibration;
    public TextZoomEffect tzt;
    public Skill s;
    public Impact impact; // Make sure to assign this in the Inspector
    public GameObject Player, playerBody, t, b, m, crownt, crownb, diat, diab, iront, ironb, spawner, sb;
    public bool anka, dc, hasCalledWar = false, tekb, tekt, equipment, multiply, shield;
    public AudioSource audioSource;
    public float fadeDuration = 0.25f, ts = 0f;
    public string treeTag = "tree", tagToDestroy = "fake";
    public int puan;
    public float iinterval, iintervalb, minterval;
    private float timer = 0f, interval = 15f, timerb = 0f, intervalb = 20f, timerm = 0f;
    public Vector3 targetPosition;
    public Transform player;
    private List<Transform> flags = new List<Transform>();
    Image imageComponent, imageComponentt;
    public Image mu, mub;
    public Text scoreText;
    public bool isShieldActiveInGame;

    public GameState GameState
    {
        get
        {
            return _gameState;
        }
        private set
        {
            if (value != _gameState)
            {
                GameState oldState = _gameState;
                _gameState = value;
                GameStateChanged(_gameState, oldState);
            }
        }
    }

    [SerializeField]
    private GameState _gameState = GameState.Prepare;

    public static int GameCount
    {
        get { return _gameCount; }
        private set { _gameCount = value; }
    }

    private static int _gameCount = 0;

    [Header("Gameplay Config")]
    [Range(0.0025f, 0.5f)]
    public float refillCubeFrequency = 0.02f;

    [Range(0.0025f, 0.25f)]
    public float treeFrequency = 0.1f;

    [Range(0.0025f, 0.5f)]
    public float springFrequency = 0.1f;

    [Range(0.1f, 2f)]
    public float gameSpeed = 1;

    [HideInInspector] public float boundsX;
    [HideInInspector] public float boundsY;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        PlayerController.PlayerDied += PlayerController_PlayerDied;
    }

    void OnDisable()
    {
        PlayerController.PlayerDied -= PlayerController_PlayerDied;
    }

    void Start()
    {
        sm = Object.FindFirstObjectByType<ScoreManager>();
        audioSource = Object.FindFirstObjectByType<AudioSource>();
        audioSource.Pause();
        audioSource.clip = null;
        GameState = GameState.Prepare;
        imageComponent = b.GetComponent<Image>();
        imageComponentt = t.GetComponent<Image>();
        Time.timeScale = gameSpeed;
        SetGameObjectsActiveState(false, t, b, m, crownb, crownt, diab, diat, ironb, iront);
    }

    public void Update()
    {
        HandleGameRestart();
        HandleRewardedAd();
        HandleScoreUpdates();
        if (sm.Score >= 50 && equipment == true && GameState == GameState.Playing || s.iron == true)
        {
            t.SetActive(true);
            timer += Time.deltaTime;
            iinterval = 15f;
            imageComponentt.color = Color.white;
            if (timer >= interval)
            {
                StartCoroutine(Salatalik());
                timer = 0f;
            }
            if (sm.Score >= 200)
            {
                crownt.SetActive(true);
                imageComponentt.color = Color.yellow;
                interval = 10f;
                iinterval = 10f;
            }
            if (sm.Score >= 400)
            {
                crownt.SetActive(false);
                diat.SetActive(true);
                imageComponentt.color = Color.cyan;
                interval = 8f;
                iinterval = 8f;
            }
        }

        if (sm.Score >= 100 && equipment == true && GameState == GameState.Playing || s.iron == true)
        {
            b.SetActive(true);
            timerb += Time.deltaTime;
            imageComponent.color = Color.white;
            iintervalb = 20f;
            if (timerb >= intervalb)
            {
                StartCoroutine(block());
                timerb = 0f;
            }
            if (sm.Score >= 300)
            {
                crownb.SetActive(true);
                imageComponent.color = Color.yellow;
                intervalb = 15f;
                iintervalb = 15f;
            }
            if (sm.Score >= 500)
            {
                crownb.SetActive(false);
                diab.SetActive(true);
                imageComponent.color = Color.cyan;
                intervalb = 9f;
                iintervalb = 9f;
            }
        }

        if (tekt == true && sm.Score >= 50 && equipment == true && GameState == GameState.Playing)
        {
            t.SetActive(true);
            StartCoroutine(Salatalik());
            tekt = false;
        }

        if (tekb == true && sm.Score >= 100 && equipment == true && GameState == GameState.Playing)
        {
            b.SetActive(true);
            StartCoroutine(block());
            tekb = false;
        }
        FindFlags();
        StartCoroutine(CallWarAfterDelay());
        iron();
    }

    private void PrepareForNewGameSession(bool resetScore)
    {
        if (resetScore)
        {
            ScoreManager.Instance.ResetCurrentGameScore();
        }

        shield = ScoreManager.Instance.IsShieldPurchased;
        equipment = ScoreManager.Instance.IsEquipmentPurchased;
        multiply = ScoreManager.Instance.IsMultiplyPurchased;
        anka = !ScoreManager.Instance.IsPhoenixPurchased;

        isShieldActiveInGame = shield;

        if (impact == null)
        {
            impact = Object.FindFirstObjectByType<Impact>();
        }

        tekb = true;
        tekt = true;
        dc = false;

        Debug.Log($"[GameManager] New session prepared. Shield active: {isShieldActiveInGame}. Score Reset: {resetScore}");
    }
    
    private void SetGameState(GameState state)
    {
        GameState = state;
        TeleportToClosestFlag(targetPosition);
        Player.SetActive(true);
        playerBody.SetActive(true);
        playerBody.GetComponent<BoxCollider2D>().enabled = true;
    }

    private void DestroyTaggedObjects()
    {
        GameObject[] objectsToDestroy = GameObject.FindGameObjectsWithTag(tagToDestroy);
        foreach (GameObject obj in objectsToDestroy)
        {
            Destroy(obj);
        }
    }

    private void HandleScoreUpdates()
    {
        if (sm.scoreIsNotLocked)
        {
            UpdateScoreMultiplier();
        }
    }

    private void UpdateScoreMultiplier()
    {
        if (multiply == true && GameState == GameState.Playing)
        {
            m.SetActive(true);
            timerm += Time.deltaTime / gameSpeed;
            if (timerm >= minterval)
            {
                ScoreManager.Instance.AddScore(puan);
                timerm = 0f;
                scoreText.text = "+" + ConvertScoreToString(puan);
                tzt.zoom();
            }
            AdjustScoreMultiplier();
        }
    }

    private void AdjustScoreMultiplier()
    {
        if (s.ttunca == true && ts <= 5)
        {
            AdjustMultiplierBasedOnScore();
        }
        else if (s.ttunca == false)
        {
            AdjustMultiplierBasedOnScoreThreshold();
        }
    }

    private void AdjustMultiplierBasedOnScore()
    {
        if (sm.Score < 250)
        {
            SetMultiplier(10, 2f, Color.red);
        }
        else if (sm.Score >= 250 && sm.Score < 500)
        {
            SetMultiplier(20, 2f, Color.red);
        }
        else if (sm.Score >= 500)
        {
            SetMultiplier(50, 2f, Color.red);
        }
    }

    private void AdjustMultiplierBasedOnScoreThreshold()
    {
        if (sm.Score >= 1000)
        {
            SetMultiplier(100, 12f, Color.red);
        }
        else if (sm.Score >= 750)
        {
            SetMultiplier(75, 10f, Color.magenta);
        }
        else if (sm.Score >= 500)
        {
            SetMultiplier(50, 10f, Color.cyan);
        }
        else if (sm.Score >= 250)
        {
            SetMultiplier(25, 12f, Color.yellow);
        }
        else if (sm.Score >= 100)
        {
            SetMultiplier(10, 7f, Color.yellow);
        }
        else if (sm.Score >= 50)
        {
            SetMultiplier(5, 5f, Color.white);
        }
        else
        {
            SetMultiplier(2, 5f, Color.white);
        }
    }

    private void SetMultiplier(int score, float interval, Color color)
    {
        if (!rw.bonus)
        {
            puan = score;
            scoreText.color = color;
        }
        else
        {
            puan = score * 2;
            scoreText.color = Color.magenta;
        }
        minterval = interval;
        ts++;
    }

    public string ConvertScoreToString(int score)
    {
        string scoreString = score.ToString();
        scoreString = scoreString.Replace('0', 'O');
        return scoreString;
    }

    void FindFlags()
    {
        flags.Clear();
        GameObject[] flagObjects = GameObject.FindGameObjectsWithTag("flag");

        foreach (GameObject flagObject in flagObjects)
        {
            flags.Add(flagObject.transform);
        }
    }

    public void TeleportToClosestFlag(Vector3 targetPosition)
    {
        if (flags.Count == 0 || player == null)
        {
            Debug.LogWarning("Flag'lar veya Player atanmamış.");
            return;
        }

        Transform closestFlag = null;
        float closestDistance = float.MaxValue;

        foreach (Transform flag in flags)
        {
            float distance = Vector3.Distance(targetPosition, flag.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestFlag = flag;
            }
        }

        if (closestFlag != null)
        {
            player.position = closestFlag.position;
        }
    }


    public void iron()
    {
        Color dict = imageComponentt.color;
        Color dic = imageComponent.color;
        if (s.iron == true)
        {
            if (sm.Score < 50)
            {
                t.SetActive(true);
            }
            if (sm.Score < 100)
            {
                b.SetActive(true);
            }
            crownt.SetActive(false);
            diat.SetActive(false);
            iront.SetActive(true);
            crownb.SetActive(false);
            diab.SetActive(false);
            ironb.SetActive(true);
            imageComponent.color = Color.cyan;
            imageComponentt.color = Color.cyan;
            interval = 1f;
            intervalb = 2f;
        }
        if (s.iron == false)
        {
            ironb.SetActive(false);
            iront.SetActive(false);
            imageComponent.color = dic;
            imageComponentt.color = dict;
            intervalb = iintervalb;
            interval = iinterval;
            if (sm.Score < 50)
            {
                t.SetActive(false);
                b.SetActive(false);
            }
            if (sm.Score >= 50 && sm.Score < 100)
            {
                b.SetActive(false);
            }
        }
    }

    public void War()
    {
        GameState = GameState.Playing;
        TeleportToClosestFlag(targetPosition);
        Player.SetActive(true);
        playerBody.SetActive(true);
        playerBody.GetComponent<BoxCollider2D>().enabled = true;
        StartCoroutine(Salatalik());
        StartCoroutine(block());
        DestroyTaggedObjects();
        s.ilyas = false;
    }

    public void ilyas()
    {
        audioSource.PlayOneShot(Resources.Load<AudioClip>("comebackwarsound"));
    }

    public IEnumerator CallWarAfterDelay()
    {
        if (hasCalledWar == false && s.ilyas == true && GameState == GameState.GameOver)
        {
            hasCalledWar = true;
            yield return new WaitForSeconds(1.5f);
            ilyas();
            yield return new WaitForSeconds(1f);
            War();
        }
    }

    public IEnumerator FadeAndDestroyTree(Renderer treeRenderer)
    {
        if (treeRenderer != null)
        {
            float startTime = Time.time;

            while (Time.time - startTime < fadeDuration)
            {
                float t = (Time.time - startTime) / fadeDuration;

                Color newColor = treeRenderer.material.color;
                newColor.a = Mathf.Lerp(1f, 0f, t);
                treeRenderer.material.color = newColor;

                yield return null;
            }

            Destroy(treeRenderer.gameObject);
        }
    }


    public IEnumerator Salatalik()
    {
        GameObject[] trees = GameObject.FindGameObjectsWithTag("tree");

        foreach (GameObject tree in trees)
        {
            BoxCollider2D collider = tree.GetComponent<BoxCollider2D>();
            if (collider != null)
            {
                collider.enabled = false;
            }
            StartCoroutine(FadeOutAndDestroy(tree));
        }

        yield return new WaitForSeconds(fadeDuration);

        foreach (GameObject tree in trees)
        {
            Destroy(tree);
        }
    }

    public IEnumerator FadeOutAndDestroy(GameObject tree)
    {
        Renderer renderer = tree.GetComponent<Renderer>();

        if (renderer != null)
        {
            float startAlpha = renderer.material.color.a;
            float startTime = Time.time;

            while (Time.time - startTime < fadeDuration)
            {
                if (renderer == null)
                {
                    yield break;
                }

                float t = (Time.time - startTime) / fadeDuration;
                Color newColor = renderer.material.color;
                newColor.a = Mathf.Lerp(startAlpha, 0f, t);
                renderer.material.color = newColor;

                yield return null;
            }

            if (renderer != null)
            {
                Destroy(tree);
            }
        }
    }


    public IEnumerator block()
    {
        foreach (GameObject c in spawner.GetComponent<SpawnCubes>().myWaves)
        {
            c.GetComponent<CubeWave>().Refill();
        }
        yield return new WaitForSeconds(0f);
    }


    void PlayerController_PlayerDied()
    {
        GameOver();
    }

    IEnumerator IncreaseGameSpeed()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);

            if (GameState == GameState.Playing)
            {
                if (gameSpeed <= 2)
                {
                    gameSpeed += 0.01f;
                    Time.timeScale = gameSpeed;
                }
            }
        }
    }

    IEnumerator IncreaseTreeFrequancy()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);

            if (GameState == GameState.Playing)
            {
                if (treeFrequency <= 0.12)
                {
                    treeFrequency += 0.001f;
                }
            }
        }
    }

    // REPLACED: This function now properly calls the new in-place reset logic.
    public void StartGame()
    {
        ResetForNewGame(true); // True to reset the score for a brand new game
    }

    // REPLACED: This function is now simplified. The responsibility for showing UI
    // is correctly handled by the UIManager listening to the GameStateChanged event.
    public void GameOver()
    {
        if (GameState == GameState.GameOver) return;

        GameState = GameState.GameOver; // This event will trigger UIManager and ScoreManager

        SoundManager.Instance.PlaySound(SoundManager.Instance.gameOver);
        SetGameObjectsActiveState(false, t, b, crownb, crownt, diab, diat, ironb, iront);

        if (s != null)
        {
            s.iron = false;
        }
        // Note: scoreIsNotLocked is reset inside ResetForNewGame or on revive.
    }

    // REPLACED: This no longer reloads the scene. It calls the new in-place reset logic.
    public void RestartGame()
    {
        ResetForNewGame(true);
    }

    // REMOVED: The CRRestartGame coroutine is no longer needed and should be deleted.
    // We are not reloading the scene anymore.

    // CORRECTED: This was a major source of data loss.
    // A stall in gameplay should result in a GameOver, which triggers a save,
    // not a silent restart that loses data.
    private void HandleGameRestart()
    {
        if (GameState == GameState.Playing && !sm.scoreIsNotLocked)
        {
            Debug.Log("[GameManager] Player stalled for too long. Triggering GameOver.");
            GameOver(); // FIX: Changed from RestartGame() to GameOver() to ensure data is saved.
        }
    }

  // Add this new property at the top of your GameManager class, near your other public variables.
// It will track revives for the current game session.
public int RevivesUsedThisLife { get; private set; }

// REPLACE your existing HandleRewardedAd function with this one.
// It is now much simpler and delegates the work to the new PerformRevive function.
private void HandleRewardedAd()
{
    // This function now only checks if the reward from an ad has been granted.
    // The decision to show the revive button was already handled correctly by the UIManager.
    if (rw.rewardb == true && GameState == GameState.GameOver)
    {
        PerformRevive();
    }
}

// ADD this new function to your GameManager.cs file.
// This is the new, centralized function that handles all the logic for reviving the player.
public void PerformRevive()
{
    Debug.Log("[GameManager] A revive has been triggered. Reviving player...");

    // 1. Clear the world of all obstacles before reviving.
    DestroyTaggedObjects();    // Remove any leftover death effects (like "fakeBall").
    StartCoroutine(Salatalik()); // Clear all trees.
    StartCoroutine(block());     // Regenerate all blocks.

    // 2. Prepare the player's state for the new life (without resetting the score).
    PrepareForNewGameSession(false);

    // 3. Place the player back in the world and resume the game.
    SetGameState(GameState.Playing);

    // 4. CRITICAL: Increment the counter for revives used in this session.
    RevivesUsedThisLife++;
    Debug.Log($"[GameManager] Revives used in this session is now: {RevivesUsedThisLife}");

    // 5. Handle ad and UI logic.
    rw.rewardb = false; // Reset the ad reward trigger.
    sm.scoreIsNotLocked = true;
    if (ui != null)
    {
        ui.restartBtn.SetActive(false); // Ensure the regular restart button is hidden.
    }
    rw.LoadRewardedAd(); // Pre-load the next rewarded ad.
}


// REPLACE your existing ResetForNewGame function with this updated version.
// The key change is resetting our new counter instead of the old boolean flags.
public void ResetForNewGame(bool resetCurrentScore)
{
    Debug.Log($"[GameManager] Resetting for a new game. Reset Score: {resetCurrentScore}");

    StopAllCoroutines();

    // Clean up the game world for a fresh start.
    StartCoroutine(Salatalik());
    StartCoroutine(block());
    DestroyTaggedObjects();

    if (resetCurrentScore)
    {
        ScoreManager.Instance.ResetCurrentGameScore();
    }
    sm.scoreIsNotLocked = true;

    PrepareForNewGameSession(resetCurrentScore);
    gameSpeed = 1f;
    Time.timeScale = gameSpeed;
    timer = 0f;
    timerb = 0f;
    timerm = 0f;
    hasCalledWar = false;

    // --- CRITICAL RESET ---
    // Reset the revive counter for the new game. This replaces the old boolean flags.
    RevivesUsedThisLife = 0;
    // The old flags tekro and tekrt are no longer needed and can be deleted from the class.

    TeleportToClosestFlag(targetPosition);
    if (Player != null) Player.SetActive(true);
    if (playerBody != null)
    {
        playerBody.SetActive(true);
        playerBody.GetComponent<BoxCollider2D>().enabled = true;
    }

    GameState = GameState.Playing;
    StartCoroutine(IncreaseGameSpeed());
    StartCoroutine(IncreaseTreeFrequancy());

    // Step 7: Manage Audio (This part remains unchanged)
    if (cs != null && audioSource != null)
    {
        cs.audioSource.Stop();

        string currentLanguage = LocalizationSettings.SelectedLocale.Identifier.Code;

        Dictionary<int, string> trAudioClipNames = new Dictionary<int, string>
        {
            { 0, "efemusic" }, { 1, "baldibackmusic" }, { 2, "mevtmusic" }, { 3, "mertmusic" }, { 4, "lucifermusic" }, { 5, "gebeshmusic" }, { 6, "enesmusic" }, { 7, "orkunmusic" }, { 8, "abugatmusic" }, { 9, "servetmusic" }, { 10, "onurcanmusic" }, { 11, "oyunportal" }, { 12, "burakmusic" }, { 13, "okanermusic" }, { 14, "tolgamusic" }, { 15, "warmusic" }, { 16, "evrimmusic" }, { 17, "mustafamusic" }, { 18, "mertcanmusic" }, { 19, "tunamusic" }, { 20, "erenmusic" }, { 21, "handsomemusic" }, { 22, "pqueenmusic" }, { 23, "selimmusic" }, { 24, "tuncamusic" }, { 25, "heumragemusic" }, { 26, "auntmusic" }, { 27, "sabomusic" }, { 28, "hakanmusic" }, { 29, "quitmusic" }, { 30, "taylanmusic" }, { 31, "killokimusic" }, { 32, "porteamusic" }, { 33, "basomusic" }, { 34, "sobutaymusic" }, { 36, "trashmusic" }, { 38, "lazalisong" }, { 39, "altincocuksong" }, { 40, "kadirhocasong" }, { 41, "oyunkonsolusong" }, { 42, "turabisong" },
        };

        Dictionary<int, string> enAudioClipNames = new Dictionary<int, string>
        {
            { 0, "skibidisong" }, { 1, "orangesong" }, { 2, "shreksong" }, { 3, "stonkssong" }, { 4, "pepesong" }, { 5, "dogesong" }, { 6, "baldisong" }, { 7, "batemansong" }, { 8, "rocksong" }, { 9, "trollfacesong" }, { 10, "maxsong" }, { 11, "brainrotsong" }, { 12, "brainrotsong" }, { 13, "larilisong" }, { 14, "brainrotsong" }, { 15, "brainrotsong" }, { 16, "brainrotsong" }, { 17, "brainrotsong" }, { 18, "brainrotsong" }, { 19, "brainrotsong" }, { 20, "brainrotsong" },
        };

        Dictionary<int, string> selectedAudioClipNames = (currentLanguage == "tr-TR") ? trAudioClipNames : enAudioClipNames;

        if (selectedAudioClipNames.ContainsKey(cs.index))
        {
            audioSource.clip = Resources.Load<AudioClip>(selectedAudioClipNames[cs.index]);
            audioSource.loop = true;
            audioSource.Play();

            if (currentLanguage == "tr-TR" && cs.index == 12)
            {
                cs.burakoyunda = true;
            }
        }
    }
    Debug.Log("[GameManager] In-place reset complete. Game is now playing.");
}

    public void RestartGame(float delay = 0)
    {
        StartCoroutine(CRRestartGame(delay));
        SetGameObjectsActiveState(false, t, b, crownb, crownt, diab, diat, ironb, iront);
    }

    IEnumerator CRRestartGame(float delay = 0)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        SetGameObjectsActiveState(false, t, b, crownb, crownt, diab, diat, ironb, iront);
    }

    private void SetGameObjectsActiveState(bool state, params GameObject[] gameObjects)
    {
        foreach (var obj in gameObjects)
        {
            if (obj != null)
            {
                obj.SetActive(state);
            }
        }
    }
}