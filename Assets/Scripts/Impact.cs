// Filename: Impact.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Impact : MonoBehaviour
{
    public GameObject theBall;
    public GameObject theBallsSprite;
    public Rewarded rw;
    public PlayerController playerController;
    public UIManager ui;
    public GameObject fakeBall;
    public Effect e;
    public float destructionDelay = 0.5f;
    GameObject endBall;
    public GameObject spawner;
    public GameManager gm;
    public Skill s;
    public GameObject cp;
    public Text scoreText2;
    public SelectCharacter cs;
    public AudioSource musicManager;
    public int counters, counterk;
    public PlayerAn pa;
    public Text scoreText;

    // --- FIX #1: A CLEAR AND PRECISE FLAG ---
    // This flag will track only the first landing of a new game or new life.
    private bool isFirstContactInGame = true;

    private int conditionCounter = 0;
    private int requiredConditionCount = 3;
    public float fadeDuration = 0.5f;

    public void Start()
    {
        musicManager = Object.FindFirstObjectByType<AudioSource>();
        gm.targetPosition.y = transform.position.y;
    }

    public void Update()
    {
        if (Say())
        {
            conditionCounter++;
            if (conditionCounter >= requiredConditionCount)
            {
                s.orkun = false;
            }
        }
    }

    private bool Say()
    {
        return conditionCounter >= requiredConditionCount;
    }

    // --- FIX #2: FLAWLESS ONTRIGGERENTER2D LOGIC ---
    void OnTriggerEnter2D(Collider2D coll)
    {
        if (playerController.isJumping)
        {
            return;
        }

        if (coll.CompareTag("ground"))
        {
            gm.targetPosition.y = transform.position.y;

            if (isFirstContactInGame)
            {
                isFirstContactInGame = false;
            }
            else
            {
                if (GameManager.Instance.isShieldActiveInGame)
                {
                    coll.gameObject.GetComponent<TheTerrian>().Shield();
                }
                else
                {
                    coll.gameObject.GetComponent<TheTerrian>().ChangeColor();
                }

                e.Effecting();

                HandleScoreOnGroundHit();
            }

            theBall.transform.position = coll.gameObject.transform.Find("flag").transform.position;
            transform.position = theBall.transform.position;

            HandleSpringInteraction(coll);
        }
        else if (coll.CompareTag("vacant"))
        {
            HandleVacantBlockHit(coll);
        }
        else if (coll.CompareTag("refill"))
        {
            HandleRefillHit(coll);
        }
        else if (coll.CompareTag("tree"))
        {
            HandleTreeHit(coll);
        }

        playerController.isCheckingCollision = false;
    }

    // --- FIX #3: ESSENTIAL RESET FUNCTION ---
    // This function must be called by the GameManager at the start of every new game or revive.
    public void ResetForNewGame()
    {
        isFirstContactInGame = true;
        Debug.Log("[Impact] State has been reset for a new game.");
    }

    // --- The following methods are now helper functions called by OnTriggerEnter2D ---

    // REPLACE your old HandleScoreOnGroundHit method with this new, clean version.
    private void HandleScoreOnGroundHit()
    {
        // Get the number of revives used in this life directly from the GameManager.
        int revivesUsed = GameManager.Instance.RevivesUsedThisLife;

        // --- Tiered Scoring Logic based on Character Skills and Revives Used ---

        if (s.Double || s.en) // Logic for 'Double' or 'en' skills
        {
            switch (revivesUsed)
            {
                case 0: // First life (no revives)
                    UpdateScoreAndText(2, "+2");
                    break;
                case 1: // Second life (1 revive used)
                    UpdateScoreAndText(rw.bonus ? 6 : 3, rw.bonus ? "+6" : "+3");
                    break;
                case 2: // Third life (2 revives used)
                    UpdateScoreAndText(rw.bonus ? 8 : 4, rw.bonus ? "+8" : "+4");
                    break;
                default: // Fourth life and beyond (e.g., with Phoenix)
                    UpdateScoreAndText(rw.bonus ? 10 : 5, rw.bonus ? "+1O" : "+5");
                    break;
            }
        }
        else if (s.pqueen || s.hakany) // Logic for 'pqueen' or 'hakany' skills
        {
            switch (revivesUsed)
            {
                case 0: // First life (no revives)
                    UpdateScoreAndText(4, "+4");
                    break;
                case 1: // Second life (1 revive used)
                    UpdateScoreAndText(rw.bonus ? 10 : 5, rw.bonus ? "+1O" : "+5");
                    break;
                case 2: // Third life (2 revives used)
                    UpdateScoreAndText(rw.bonus ? 12 : 6, rw.bonus ? "+12" : "+6");
                    break;
                default: // Fourth life and beyond
                    UpdateScoreAndText(rw.bonus ? 14 : 7, rw.bonus ? "+14" : "+7");
                    break;
            }
        }
        else // Default scoring logic for all other characters
        {
            switch (revivesUsed)
            {
                case 0: // First life (no revives)
                    UpdateScoreAndText(rw.bonus ? 2 : 1, rw.bonus ? "+2" : "+1");
                    break;
                case 1: // Second life (1 revive used)
                    UpdateScoreAndText(rw.bonus ? 4 : 2, rw.bonus ? "+4" : "+2");
                    break;
                case 2: // Third life (2 revives used)
                    UpdateScoreAndText(rw.bonus ? 6 : 3, rw.bonus ? "+6" : "+3");
                    break;
                default: // Fourth life and beyond
                    UpdateScoreAndText(rw.bonus ? 8 : 4, rw.bonus ? "+8" : "+4");
                    break;
            }
        }

        if (s.ms)
        {
            if (counters >= 3)
            {
                // Refill all cube waves
                foreach (GameObject c in spawner.GetComponent<SpawnCubes>().myWaves)
                {
                    c.GetComponent<CubeWave>().Refill();
                }
                counters = 0; // Reset the counter
            }
            else
            {
                counters++;
            }
        }
        else if (s.loki && s.ot)
        {
            counterk++;
        }
        else if (s.loki && !s.ot)
        {
            UpdateCounterScore(rw.bonus ? 4 : 2);
        }
        else if (!s.loki && s.ttesto)
        {
            if (s.ot)
            {
                UpdateCounterScore(rw.bonus ? 4 : 2);
            }
            else
            {
                counterk++;
            }
        }
    }

    private void HandleSpringInteraction(Collider2D coll)
    {
        if (coll.GetComponentInChildren<TheTerrian>(true)?.isOwningASpring ?? false)
        {
            playerController.isreadyForBigJump = true; ScoreManager.Instance.AddScore(1);
            foreach (Transform child in coll.transform) { if (child.CompareTag("spring")) { if (s.pqueen) s.zaman += 2; else if (s.okan || s.bu) AddScoreAndEffect(rw.bonus ? 39 : 19, rw.bonus ? "+4O" : "+2O"); else if (s.so) { AddScoreAndEffect(rw.bonus ? 7 : 3, rw.bonus ? "+8" : "+4"); gm.springFrequency += 0.02f; } else if (s.py) AddScoreAndEffect(rw.bonus ? 15 : 7, rw.bonus ? "+16" : "+8"); Destroy(child.gameObject); break; } }
        }
    }

    private void HandleVacantBlockHit(Collider2D coll)
    {
        gm.targetPosition.y = transform.position.y;
        if (s.orkunv == false) { FallAndDie(); }
        else { if (s.Double) { UpdateScoreAndText(rw.bonus ? 4 : 2, rw.bonus ? "+4" : "+2"); } else { UpdateScoreAndText(rw.bonus ? 2 : 1, rw.bonus ? "+2" : "+1"); } theBall.transform.position = coll.gameObject.transform.Find("flag").transform.position; transform.position = theBall.transform.position; s.orkunv = false; }
    }

    private void HandleRefillHit(Collider2D coll)
    {
        gm.targetPosition.y = transform.position.y; Destroy(coll.gameObject);
        if (s.okan || s.bo) AddScoreAndEffect(rw.bonus ? 40 : 20, rw.bonus ? "+4O" : "+2O"); else if (s.eren) AddScoreAndEffect(rw.bonus ? 60 : 30, rw.bonus ? "+6O" : "+3O"); else if (s.pqueen) s.zaman += 2; else if (s.cb) gm.refillCubeFrequency += 0.02f; else if (s.td || s.py) { AddScoreAndEffect(rw.bonus ? 11 : 5, rw.bonus ? "+12" : "+6"); if (s.td) gm.treeFrequency -= 0.02f; }
        foreach (GameObject c in spawner.GetComponent<SpawnCubes>().myWaves) { c.GetComponent<CubeWave>().Refill(); }
    }

    private void HandleTreeHit(Collider2D coll)
    {
        gm.targetPosition.y = transform.position.y;

        if (gm.isShieldActiveInGame && gm.GameState == GameState.Playing && !s.treef && !s.evrimtree && !s.orkun)
        {
            musicManager.PlayOneShot(Resources.Load<AudioClip>("break"));
            StartCoroutine(gm.FadeAndDestroyTree(coll.GetComponent<Renderer>()));
            gm.isShieldActiveInGame = false;

            pa.Death();
        }
        else if (s.evrimtree)
        {
            AddScoreAndEffect(rw.bonus ? 20 : 10, rw.bonus ? "+2O" : "+1O");
            Destroy(coll.gameObject);
        }
        else if (s.orkun || s.treef)
        {
            Destroy(coll.gameObject);
        }
        else
        {
            HitTreeAndDie();
        }
    }


    private void FallAndDie()
    {
        playerController.Die(); theBall.SetActive(false); GetComponent<BoxCollider2D>().enabled = false;
        endBall = Instantiate(fakeBall, transform.position, Quaternion.identity) as GameObject;
        endBall.GetComponent<SpriteRenderer>().sprite = CharacterManager.Instance.character;
        endBall.GetComponent<Renderer>().sortingOrder = -1; gameObject.SetActive(false);
    }

    private void HitTreeAndDie()
    {
        pa.Death(); playerController.Die(); theBall.SetActive(false); GetComponent<BoxCollider2D>().enabled = false;
        endBall = Instantiate(fakeBall, transform.position, Quaternion.identity) as GameObject;
        endBall.GetComponent<SpriteRenderer>().sprite = CharacterManager.Instance.character;
        gameObject.SetActive(false);
    }

    void UpdateScoreAndText(int score, string text) { ScoreManager.Instance.AddScore(score); scoreText.text = text; }
    void AddScoreAndEffect(int score, string text) { ScoreManager.Instance.AddScore(score); scoreText2.text = text; e.Effecting2(); }
    void UpdateCounterScore(int multiplier) { ScoreManager.Instance.AddScore(counterk * multiplier); scoreText2.text = "+" + gm.ConvertScoreToString(counterk * multiplier); e.Effecting2(); counterk = 0; s.ot = true; }
    public void ChangeObjectsColor(string tag, Color color) { GameObject[] objects = GameObject.FindGameObjectsWithTag(tag); foreach (GameObject obj in objects) { if (obj != null) { ChangeObjectColor(obj, color); } } }
    private void ChangeObjectColor(GameObject obj, Color newColor) { SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>(); if (spriteRenderer != null) { spriteRenderer.color = newColor; } }
}