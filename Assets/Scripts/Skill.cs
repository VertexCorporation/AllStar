using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

public class Skill : MonoBehaviour
{
    public SelectCharacter cs;
    public UIManager ui;
    public CameraFollow cf;
    public AudioSource audioSource;
    public GameManager gm;
    public GameObject c, cp, img, bg, di, spawner, night, buton;
    public Effect e;
    public Impact im;
    public ScoreManager sm;
    public Transform player;
    public PlayerController characterController;
    public Text scoreText2;
    public int sayiyom = 0;
    private Color targetColor;
    private float colorTransitionDuration = 1.0f;
    private Coroutine colorTransitionCoroutine;
    public Button targetButton;
    public Image buttonImage, dimg, bga;
    public bool treef, orkun = false, orkunv = false, skillbitti = false, zebanifall, iron = false, burakdaoyunda, Double = false, okan = false, evrimtree = false, ilyas = false, eren = false, pqueen = false, ms = false, hakany = false, loki = false, ttesto = false, ttunca = false, cb = false, so = false, td = false, py = false, bs = false, en = false, ot = false, bo = false, bu = false, alikol = false, isButtonClickable = true;
    public float fadeDuration = 0.25f, zaman = 4, originalGameSpeed, originaltreeFrequency, originalrefillCubeFrequency, originalspringFrequency, kasy;
    public Camera mainCamera;
    public PlayerAn pa;
    public Sprite refillSprite;

    private Dictionary<int, string> trAudioClipNames;
    private Dictionary<int, string> enAudioClipNames;
    private Dictionary<int, int> enAbilityIndexMapping;

    public void Start()
    {
        sm = Object.FindFirstObjectByType<ScoreManager>();
        audioSource = Object.FindFirstObjectByType<AudioSource>();
        bga = bg.GetComponent<Image>();
        dimg = di.GetComponent<Image>();
        
        InitializeDictionaries();
    }
    
    // REFACTOR: Moved dictionary initialization to its own method for Start() clarity.
    private void InitializeDictionaries()
    {
        trAudioClipNames = new Dictionary<int, string>()
        {
            {0, "zebani"}, {1, "baldiback"}, {2, "mevtcan"}, {3, "mert"}, {4, "lucifer"}, {5, "gebesh"}, {6, "enes"}, {7, "orkun"}, {8, "abugat"}, {9, "servet_sound"}, {10, "aaa"}, {11, "portal"}, {12, "burak"}, {13, "okaner"}, {14, "tolga"}, {15, "war"}, {16, "evrim"}, {17, "mustafa"}, {18, "mertcan"}, {19, "tuna"}, {20, "eren"}, {21, "handsome_sound"}, {22, "pqueen"}, {23, "selim"}, {24, "tunca"}, {25, "heumrage"}, {26, "aunt"}, {27, "sabo"}, {28, "hakan"}, {29, "quit"}, {30, "taylan"}, {31, "killoki"}, {32, "portea"}, {33, "baso"}, {34, "sobutay"}, {35, "eray"}, {36, "trash"}, {37, "tarikpasha"}, {38, "lazaliskill"}, {39, "altincocukskill"}, {40, "kadirhocaskill"}, {41, "oyunkonsoluskill"}, {42, "turabiskill"},
        };

        enAudioClipNames = new Dictionary<int, string>()
        {
            {0, "skibidiskill"}, {1, "orangeskill"}, {2, "shrekskill"}, {3, "stonksskill"}, {4, "pepeskill"}, {5, "dogeskill"}, {6, "baldiskill"}, {7, "batemanskill"}, {8, "rockskill"}, {9, "trollfaceskill"}, {10, "maxskill"}, {11, "brainrotskill"}, {12, "brainrotskill"}, {13, "brainrotskill"}, {14, "brainrotskill"}, {15, "brainrotskill"}, {16, "brainrotskill"}, {17, "brainrotskill"}, {18, "brainrotskill"}, {19, "brainrotskill"}, {20, "brainrotskill"},
        };

        enAbilityIndexMapping = new Dictionary<int, int>()
        {
            {10, 17},
        };
    }

    private int GetAbilityIndex(int characterIndex)
    {
        string currentLanguage = LocalizationSettings.SelectedLocale.Identifier.Code;
        if (currentLanguage == "en-US" && enAbilityIndexMapping.ContainsKey(characterIndex))
        {
            return enAbilityIndexMapping[characterIndex];
        }
        return characterIndex;
    }

    private string GetAudioClipName(int characterIndex)
    {
        string currentLanguage = LocalizationSettings.SelectedLocale.Identifier.Code;
        if (currentLanguage == "tr-TR" && trAudioClipNames.ContainsKey(characterIndex)) return trAudioClipNames[characterIndex];
        if (currentLanguage == "en-US" && enAudioClipNames.ContainsKey(characterIndex)) return enAudioClipNames[characterIndex];
        if (trAudioClipNames.ContainsKey(characterIndex)) return trAudioClipNames[characterIndex];
        return "";
    }

    void Update()
    {
        if (skillbitti && isButtonClickable)
        {
            SetButtonColors(new Color(0.898f, 0.898f, 0.898f), Color.white);
        }
        else if (skillbitti && !isButtonClickable)
        {
            SetButtonColors(new Color(0.898f, 0.898f, 0.898f), new Color(0.376f, 0.376f, 0.376f));
        }
        else if (!skillbitti && !isButtonClickable)
        {
            SetButtonColors(new Color(0.376f, 0.376f, 0.376f), new Color(0.376f, 0.376f, 0.376f));
        }
    }

    void SetButtonColors(Color buttonColor, Color imageColor)
    {
        if (targetButton == null) return;
        ColorBlock colors = targetButton.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = buttonColor;
        colors.pressedColor = buttonColor;
        colors.selectedColor = buttonColor;
        colors.disabledColor = buttonColor;
        targetButton.colors = colors;
        if (buttonImage != null) buttonImage.color = imageColor;
    }

    public void OnClick()
    {
        if (sm.scoreIsNotLocked && gm.GameState == GameState.Playing && isButtonClickable)
        {
            int abilityIndex = GetAbilityIndex(cs.index);
            
            // Play sound first
            string languageSpecificClipName = GetAudioClipName(cs.index);
            if (!string.IsNullOrEmpty(languageSpecificClipName))
            {
                AudioClip clip = Resources.Load<AudioClip>(languageSpecificClipName);
                if (clip != null) audioSource.PlayOneShot(clip);
            }
            
            isButtonClickable = false;
            skillbitti = false;
            
            // Handle unique case 0
            if (abilityIndex == 0)
            {
                ColorUtility.TryParseHtmlString("#8C0000", out targetColor);
                if(cp != null) cp.GetComponent<BoxCollider2D>().isTrigger = false;
                if (colorTransitionCoroutine != null) StopCoroutine(colorTransitionCoroutine);
                colorTransitionCoroutine = StartCoroutine(StartColorTransition());
                return; // Exit after starting coroutine
            }
            
            // Handle all other abilities
            StartAbilityCoroutine(abilityIndex);
        }
    }

    private void StartAbilityCoroutine(int abilityIndex)
    {
        switch (abilityIndex)
        {
            case 1: StartCoroutine(BaldiBackle()); break;
            case 2: StartCoroutine(mevt()); break;
            case 3: StartCoroutine(mert()); break;
            case 4: StartCoroutine(lucy()); break;
            case 5: StartCoroutine(gebesh()); break;
            case 6: StartCoroutine(enes()); break;
            case 7: StartCoroutine(kask()); break;
            case 8: StartCoroutine(deha()); break;
            case 9: StartCoroutine(sayar()); break;
            case 10: StartCoroutine(aaa()); break;
            case 11: 
                StartCoroutine(oyun());
                ColorUtility.TryParseHtmlString("#572E00", out targetColor);
                if (colorTransitionCoroutine != null) StopCoroutine(colorTransitionCoroutine);
                colorTransitionCoroutine = StartCoroutine(StartColorTransition());
                break;
            case 12: StartCoroutine(burak()); burakdaoyunda = true; break;
            case 13: 
                StartCoroutine(okaner());
                ColorUtility.TryParseHtmlString("#6A005F", out targetColor);
                if (colorTransitionCoroutine != null) StopCoroutine(colorTransitionCoroutine);
                colorTransitionCoroutine = StartCoroutine(StartColorTransitionO());
                break;
            case 14: StartCoroutine(tolga()); break;
            case 15: StartCoroutine(war()); break;
            case 16:
                StartCoroutine(evrim());
                ColorUtility.TryParseHtmlString("#00FF00", out targetColor);
                if (colorTransitionCoroutine != null) StopCoroutine(colorTransitionCoroutine);
                colorTransitionCoroutine = StartCoroutine(StartColorTransitionE());
                break;
            case 17: StartCoroutine(mustafa()); break;
            case 18: StartCoroutine(mertcan()); break;
            case 19: StartCoroutine(tuna()); break;
            case 20: StartCoroutine(blackmamba()); break;
            case 21: StartCoroutine(handsome()); break;
            case 22: StartCoroutine(pQueen()); break;
            case 23: StartCoroutine(selim()); break;
            case 24: StartCoroutine(tunca()); break;
            case 25: StartCoroutine(heumrage()); break;
            case 26: StartCoroutine(aunt()); break;
            case 27: StartCoroutine(sabo()); break;
            case 28: StartCoroutine(hakan()); break;
            case 29: StartCoroutine(quit()); break;
            case 30: StartCoroutine(taylan()); break;
            case 31: StartCoroutine(killoki()); break;
            case 32: StartCoroutine(portea()); break;
            case 33: StartCoroutine(baso()); break;
            case 34: StartCoroutine(sobutay()); break;
            case 35: StartCoroutine(eray()); break;
            case 36: StartCoroutine(order()); break;
            case 37: StartCoroutine(tarikpasha()); break;
            case 38: StartCoroutine(lazali()); break;
            case 39: StartCoroutine(enes()); break;
            case 40: StartCoroutine(handsome()); break;
            case 41: StartCoroutine(deha()); break;
            case 42: StartCoroutine(mustafa()); break;
            default:
                Debug.LogWarning($"Undefined ability index: {abilityIndex}");
                skillbitti = true;
                isButtonClickable = true;
                break;
        }
    }

    private IEnumerator kask()
    {
        orkun = true;
        orkunv = true;
        yield return new WaitForSeconds(10.0f);
        orkun = false;
        orkunv = false;
        skillbitti = true;
        yield return new WaitForSeconds(10.0f);
        isButtonClickable = true;
    }

    private IEnumerator enes()
    {
        // OPTIMIZATION: Replaced duplicated fade logic with a single call to the GameManager's coroutine.
        StartCoroutine(gm.Salatalik());
        skillbitti = true;
        yield return new WaitForSeconds(10.0f);
        isButtonClickable = true;
    }

    private IEnumerator gebesh()
    {
        originalGameSpeed = gm.gameSpeed;
        gm.gameSpeed = 0.8f;
        yield return new WaitForSeconds(7.0f);
        gm.gameSpeed = originalGameSpeed;
        skillbitti = true;
        yield return new WaitForSeconds(10.0f);
        isButtonClickable = true;
    }

    private IEnumerator okaner()
    {
        okan = true;
        // FIX: Replaced calls to deleted methods with the new, unified method.
        im.ChangeObjectsColor("spring", new Color(1.0f, 0.0f, 0.8235294f));
        im.ChangeObjectsColor("refill", new Color(1.0f, 0.0f, 0.8235294f));
        yield return new WaitForSeconds(5.0f);
        okan = false;
        im.ChangeObjectsColor("spring", Color.white);
        im.ChangeObjectsColor("refill", Color.white);
        skillbitti = true;
        yield return new WaitForSeconds(25.0f);
        isButtonClickable = true;
    }

    private IEnumerator burak()
    {
        int currentCsIndex = cs.index;
        int newIndex;
        do
        {
            newIndex = Random.Range(0, 38); // Assuming 38 total characters
        } while (newIndex == currentCsIndex);
        cs.index = newIndex;
        
        isButtonClickable = true;
        yield return new WaitForSeconds(20.0f);
        cs.index = 12; // Revert to Burak's index
    }

    private IEnumerator tolga()
    {
        yield return new WaitForSeconds(2.8f * gm.gameSpeed);
        iron = true;
        yield return new WaitForSeconds(10.0f);
        iron = false;
        skillbitti = true;
        yield return new WaitForSeconds(16.0f);
        isButtonClickable = true;
    }

    private IEnumerator evrim()
    {
        evrimtree = true;
        // FIX: Replaced calls to deleted methods with the new, unified method.
        im.ChangeObjectsColor("tree", Color.green);
        yield return new WaitForSeconds(5.0f);
        evrimtree = false;
        im.ChangeObjectsColor("tree", Color.white);
        skillbitti = true;
        yield return new WaitForSeconds(25.0f);
        isButtonClickable = true;
    }

    private IEnumerator war()
    {
        ilyas = true;
        yield return new WaitForSeconds(10.0f);
        ilyas = false;
        skillbitti = true;
        yield return new WaitForSeconds(40.0f);
        gm.hasCalledWar = false;
        isButtonClickable = true;
    }

    private IEnumerator mustafa()
    {
        int scoreToAdd = 0;
        switch(sayiyom)
        {
            case 0: scoreToAdd = 1; break;
            case 1: scoreToAdd = 2; break;
            case 2: scoreToAdd = 5; break;
            case 3: scoreToAdd = 10; break;
            case 4: scoreToAdd = 25; break;
            case 5: scoreToAdd = 50; break;
            default: scoreToAdd = 100; break;
        }

        if(im.rw.bonus) scoreToAdd *= 2;
        
        ScoreManager.Instance.AddScore(scoreToAdd);
        scoreText2.text = "+" + gm.ConvertScoreToString(scoreToAdd);
        e.Effecting2();
        sayiyom++;

        skillbitti = true;
        yield return new WaitForSeconds(30f);
        isButtonClickable = true;
    }

    private IEnumerator tuna()
    {
        GameObject[] treeObjeleri = GameObject.FindGameObjectsWithTag("tree");
        foreach (GameObject treeObjesi in treeObjeleri)
        {
            if (treeObjesi == null) continue;
            Transform parent = treeObjesi.transform.parent;
            if (parent == null) continue;

            foreach (Transform child in parent)
            {
                if (child.CompareTag("refill")) child.gameObject.SetActive(true);
            }
            treeObjesi.SetActive(false);
        }
        skillbitti = true;
        yield return new WaitForSeconds(15f);
        isButtonClickable = true;
    }

    private IEnumerator mertcan()
    {
        GameObject[] allTrees = GameObject.FindGameObjectsWithTag("tree");
        List<GameObject> treesInCameraView = new List<GameObject>();

        if(Camera.main != null)
        {
            foreach (GameObject tree in allTrees)
            {
                if (tree != null)
                {
                    Vector3 screenPoint = Camera.main.WorldToViewportPoint(tree.transform.position);
                    if (screenPoint.x >= 0 && screenPoint.x <= 1 && screenPoint.y >= 0 && screenPoint.y <= 1 && screenPoint.z > 0)
                    {
                        treesInCameraView.Add(tree);
                    }
                }
            }
        }
        
        int totalScore = (im.rw.bonus ? 10 : 5) * treesInCameraView.Count;
        if (totalScore > 0)
        {
            ScoreManager.Instance.AddScore(totalScore);
            scoreText2.text = "+" + totalScore;
            e.Effecting2();
        }

        foreach (GameObject tree in treesInCameraView)
        {
            StartCoroutine(gm.FadeOutAndDestroy(tree));
        }
        
        skillbitti = true;
        yield return new WaitForSeconds(25.0f);
        isButtonClickable = true;
    }

    private IEnumerator lazali()
    {
        if (alikol)
        {
            StartCoroutine(gm.Salatalik());
        }
        else
        {
            if (spawner != null && spawner.GetComponent<SpawnCubes>() != null)
            {
                foreach (GameObject cWave in spawner.GetComponent<SpawnCubes>().myWaves)
                {
                    if (cWave != null) cWave.GetComponent<CubeWave>().Refill();
                }
            }
        }
        alikol = !alikol;
        skillbitti = true;
        yield return new WaitForSeconds(7.0f);
        isButtonClickable = true;
    }

    private IEnumerator eray()
    {
        characterController.isreadyForBigJump = true;
        int scoreToAdd = im.rw.bonus ? 40 : 20;
        ScoreManager.Instance.AddScore(scoreToAdd);
        scoreText2.text = "+" + gm.ConvertScoreToString(scoreToAdd);
        e.Effecting2();
        skillbitti = true;
        yield return new WaitForSeconds(7.0f);
        isButtonClickable = true;
    }

    private IEnumerator sobutay()
    {
        bu = true;
        im.ChangeObjectsColor("spring", new Color(0.8f, 0.08f, 0.08f));
        yield return new WaitForSeconds(18.0f);
        bu = false;
        im.ChangeObjectsColor("spring", Color.white);
        skillbitti = true;
        yield return new WaitForSeconds(14.0f);
        isButtonClickable = true;
    }

    private IEnumerator baso()
    {
        bo = true;
        im.ChangeObjectsColor("refill", new Color(0.4f, 0.8f, 0.08f));
        yield return new WaitForSeconds(10.0f);
        bo = false;
        im.ChangeObjectsColor("refill", Color.white);
        skillbitti = true;
        yield return new WaitForSeconds(18.0f);
        isButtonClickable = true;
    }

    private IEnumerator blackmamba()
    {
        eren = true;
        im.ChangeObjectsColor("refill", Color.red);
        yield return new WaitForSeconds(5.0f);
        eren = false;
        im.ChangeObjectsColor("refill", Color.white);
        skillbitti = true;
        yield return new WaitForSeconds(25.0f);
        isButtonClickable = true;
    }

    private IEnumerator tarikpasha()
    {
        StartCoroutine(gm.Salatalik());
        kasy++;
        float waitTime = 20.0f - (kasy * 2.0f);
        if (waitTime < 4.0f) waitTime = 4.0f;
        
        yield return new WaitForSeconds(waitTime);
        skillbitti = true;
        isButtonClickable = true;
    }

    private IEnumerator order()
    {
        if (spawner != null && spawner.GetComponent<SpawnCubes>() != null)
        {
            foreach (GameObject cWave in spawner.GetComponent<SpawnCubes>().myWaves)
            {
                if (cWave != null) cWave.GetComponent<CubeWave>().Refill();
            }
        }
        originalGameSpeed = gm.gameSpeed;
        gm.gameSpeed = originalGameSpeed + 0.5f;
        en = true;
        yield return new WaitForSeconds(14.0f);
        en = false;
        gm.gameSpeed = originalGameSpeed;
        skillbitti = true;
        yield return new WaitForSeconds(25.0f);
        isButtonClickable = true;
    }

    private IEnumerator portea()
    {
        py = true;
        yield return new WaitForSeconds(18.0f);
        py = false;
        skillbitti = true;
        yield return new WaitForSeconds(14.0f);
        isButtonClickable = true;
    }

    private IEnumerator aunt()
    {
        float oldtreeFrequency = gm.treeFrequency;
        td = true;
        yield return new WaitForSeconds(22.0f);
        gm.treeFrequency = oldtreeFrequency;
        td = false;
        skillbitti = true;
        yield return new WaitForSeconds(10.0f);
        isButtonClickable = true;
    }

    private IEnumerator sabo()
    {
        float oldspringFrequency = gm.springFrequency;
        so = true;
        yield return new WaitForSeconds(22.0f);
        gm.springFrequency = oldspringFrequency;
        so = false;
        skillbitti = true;
        yield return new WaitForSeconds(10.0f);
        isButtonClickable = true;
    }

    private IEnumerator quit()
    {
        float oldRefillFrequency = gm.refillCubeFrequency;
        cb = true;
        yield return new WaitForSeconds(40.0f);
        gm.refillCubeFrequency = oldRefillFrequency;
        cb = false;
        skillbitti = true;
        yield return new WaitForSeconds(10.0f);
        isButtonClickable = true;
    }

    private IEnumerator tunca()
    {
        bool oldMultiply = gm.multiply;
        gm.multiply = true;
        ttunca = true;
        yield return new WaitForSeconds(12.0f);
        gm.multiply = oldMultiply;
        gm.ts = 0;
        ttunca = false;
        skillbitti = true;
        yield return new WaitForSeconds(20.0f);
        isButtonClickable = true;
    }

    private IEnumerator taylan()
    {
        if (ot)
        {
            ttesto = true;
            yield return new WaitForSeconds(5.0f);
            ttesto = false;
        }
        else
        {
            originalGameSpeed = gm.gameSpeed;
            gm.gameSpeed = Mathf.Min(2.0f, originalGameSpeed + 0.4f); // Simpler speed increase
            ttesto = true;
            yield return new WaitForSeconds(10.0f);
            ttesto = false;
            ot = true;
            gm.gameSpeed = originalGameSpeed;
        }
        skillbitti = true;
        yield return new WaitForSeconds(5.0f);
        isButtonClickable = true;
    }

    private IEnumerator heumrage()
    {
        if (spawner != null && spawner.GetComponent<SpawnCubes>() != null)
        {
            foreach (GameObject cWave in spawner.GetComponent<SpawnCubes>().myWaves)
            {
                if (cWave != null) cWave.GetComponent<CubeWave>().Refill();
            }
        }
        StartCoroutine(gm.Salatalik());
        skillbitti = true;
        yield return new WaitForSeconds(15.0f);
        isButtonClickable = true;
    }

    private IEnumerator killoki()
    {
        loki = true;
        ot = !ot;
        yield return new WaitForSeconds(10.0f);
        loki = false;
        skillbitti = true;
        yield return new WaitForSeconds(5.0f);
        isButtonClickable = true;
    }

    private IEnumerator hakan()
    {
        originalGameSpeed = gm.gameSpeed;
        gm.gameSpeed = 0.6f;
        hakany = true;
        yield return new WaitForSeconds(6.0f);
        hakany = false;
        gm.gameSpeed = originalGameSpeed;
        skillbitti = true;
        yield return new WaitForSeconds(25.0f);
        isButtonClickable = true;
    }

    private IEnumerator selim()
    {
        if (spawner != null && spawner.GetComponent<SpawnCubes>() != null)
        {
            foreach (GameObject cWave in spawner.GetComponent<SpawnCubes>().myWaves)
            {
                if(cWave != null) cWave.GetComponent<CubeWave>().Refill();
            }
        }
        ms = true;
        yield return new WaitForSeconds(10.0f);
        ms = false;
        skillbitti = true;
        yield return new WaitForSeconds(20.0f);
        isButtonClickable = true;
    }

    private IEnumerator pQueen()
    {
        pqueen = true;
        while (zaman > 0)
        {
            yield return new WaitForSeconds(1.0f);
            zaman--;
        }
        pqueen = false;
        zaman = 4;
        skillbitti = true;
        yield return new WaitForSeconds(20.0f);
        isButtonClickable = true;
    }

    private IEnumerator handsome()
    {
        gm.shield = true;
        skillbitti = true;
        yield return new WaitForSeconds(15f);
        isButtonClickable = true;
    }

    private IEnumerator mert()
    {
        Double = true;
        yield return new WaitForSeconds(10.0f);
        Double = false;
        skillbitti = true;
        yield return new WaitForSeconds(10.0f);
        isButtonClickable = true;
    }

    private IEnumerator mevt()
    {
        characterController.isreadyForBigJump = true;
        skillbitti = true;
        yield return new WaitForSeconds(2.0f);
        isButtonClickable = true;
    }

    private IEnumerator lucy()
    {
        originalspringFrequency = gm.springFrequency;
        gm.springFrequency = 0.6f;
        yield return new WaitForSeconds(5.0f);
        gm.springFrequency = originalspringFrequency;
        skillbitti = true;
        yield return new WaitForSeconds(10.0f);
        isButtonClickable = true;
    }

    private IEnumerator deha()
    {
        if (spawner != null && spawner.GetComponent<SpawnCubes>() != null)
        {
            foreach (GameObject cWave in spawner.GetComponent<SpawnCubes>().myWaves)
            {
                if(cWave != null) cWave.GetComponent<CubeWave>().Refill();
            }
        }
        skillbitti = true;
        yield return new WaitForSeconds(10.0f);
        isButtonClickable = true;
    }

    private IEnumerator BaldiBackle()
    {
        originalrefillCubeFrequency = gm.refillCubeFrequency;
        gm.refillCubeFrequency = 0.6f;
        yield return new WaitForSeconds(5.0f);
        gm.refillCubeFrequency = originalrefillCubeFrequency;
        skillbitti = true;
        yield return new WaitForSeconds(10.0f);
        isButtonClickable = true;
    }

    private IEnumerator sayar()
    {
        originalrefillCubeFrequency = gm.refillCubeFrequency;
        originalspringFrequency = gm.springFrequency;
        gm.refillCubeFrequency = 0.3f;
        gm.springFrequency = 0.3f;
        yield return new WaitForSeconds(5.0f);
        gm.refillCubeFrequency = originalrefillCubeFrequency;
        gm.springFrequency = originalspringFrequency;
        skillbitti = true;
        yield return new WaitForSeconds(10.0f);
        isButtonClickable = true;
    }

    private IEnumerator aaa()
    {
        originaltreeFrequency = gm.treeFrequency;
        gm.treeFrequency = 0f;
        yield return new WaitForSeconds(10.0f);
        gm.treeFrequency = originaltreeFrequency;
        skillbitti = true;
        yield return new WaitForSeconds(10.0f);
        isButtonClickable = true;
    }

    private IEnumerator oyun()
    {
        treef = true;
        yield return new WaitForSeconds(10.0f);
        treef = false;
        skillbitti = true;
        yield return new WaitForSeconds(10.0f);
        isButtonClickable = true;
    }

    private IEnumerator StartColorTransition()
    {
        bool isNightActive = ScoreManager.Instance.IsNightBackgroundActive;
        GameObject background = isNightActive ? night : img;
        Color originalBGColor = background.GetComponent<Image>().color;
        Color originalPlayerColor = c.GetComponent<SpriteRenderer>().color;

        yield return AnimateColors(colorTransitionDuration, originalPlayerColor, targetColor, originalBGColor, targetColor, background);
        yield return new WaitForSeconds(9f);
        yield return AnimateColors(1.0f, targetColor, Color.white, targetColor, Color.white, background);

        skillbitti = true;
        if (cp != null) cp.GetComponent<BoxCollider2D>().isTrigger = true;
        yield return new WaitForSeconds(10.0f);
        colorTransitionCoroutine = null;
        isButtonClickable = true;
    }

    private IEnumerator StartColorTransitionO()
    {
        bool isNightActive = ScoreManager.Instance.IsNightBackgroundActive;
        GameObject background = isNightActive ? night : img;
        Color originalBGColor = background.GetComponent<Image>().color;

        yield return AnimateColors(colorTransitionDuration, Color.clear, Color.clear, originalBGColor, targetColor, background, false);
        yield return new WaitForSeconds(4f);
        yield return AnimateColors(1.0f, Color.clear, Color.clear, targetColor, Color.white, background, false);

        yield return new WaitForSeconds(10.0f);
        colorTransitionCoroutine = null;
    }

    private IEnumerator StartColorTransitionE()
    {
        bool isNightActive = ScoreManager.Instance.IsNightBackgroundActive;
        GameObject background = isNightActive ? night : img;
        Color originalBGColor = background.GetComponent<Image>().color;
        
        yield return AnimateColors(colorTransitionDuration, Color.clear, Color.clear, originalBGColor, targetColor, background, false);
        yield return new WaitForSeconds(4f);
        yield return AnimateColors(1.0f, Color.clear, Color.clear, targetColor, Color.white, background, false);

        yield return new WaitForSeconds(10.0f);
        colorTransitionCoroutine = null;
    }
    
    private IEnumerator AnimateColors(float duration, Color startPlayer, Color endPlayer, Color startBG, Color endBG, GameObject background, bool animatePlayer = true)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            if (animatePlayer && c != null) c.GetComponent<SpriteRenderer>().color = Color.Lerp(startPlayer, endPlayer, t);
            if (background != null) background.GetComponent<Image>().color = Color.Lerp(startBG, endBG, t);

            yield return null;
        }
    }
}