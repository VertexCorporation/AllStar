using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Localization.Settings;
using System.Collections;

public class SelectCharacter : MonoBehaviour
{
    // Türkçe karakterler için mevcut liste
    public List<Sprite> sprites;

    // İngilizce karakterler için yeni liste
    public List<Sprite> englishSprites;

    // Aktif sprite listesini tutmak için referans
    private List<Sprite> currentSprites;

    // Türkçe karakterler için ses dizisi
    public AudioClip[] characterSounds;

    // İngilizce karakterler için ses dizisi
    public AudioClip[] englishCharacterSounds;

    // Aktif ses dizisini tutmak için referans
    private AudioClip[] currentCharacterSounds;

    [HideInInspector] public int index = 0;
    public AudioSource audioSource;
    private GameManager gm;
    public Skill s;
    public Sprite mySprite;
    public bool scig = false;
    public bool burakoyunda = false;
    public GameObject ch, nb, pb;
    public BuneLean bl;


    void OnEnable()
    {
        StartCoroutine(InitializationRoutine());
    }

    private IEnumerator InitializationRoutine()
    {
        yield return LocalizationSettings.InitializationOperation;

        Debug.Log("[SelectCharacter] Waiting for GameManager.Instance to be ready...");
        yield return new WaitUntil(() => GameManager.Instance != null);
        Debug.Log("[SelectCharacter] GameManager.Instance is now available.");

        gm = GameManager.Instance;

        Debug.Log("[SelectCharacter] Dependencies are ready. Proceeding with full initialization.");
        SetCurrentSprites();
        SetCurrentSounds();

        if (CharacterManager.Instance != null)
        {
            index = CharacterManager.Instance.index;
        }
        audioSource = Object.FindFirstObjectByType<AudioSource>();
    }

    void Update()
    {
        if (gm == null)
        {
            return;
        }

        if (LocalizationSettings.SelectedLocale != null)
        {
            SetCurrentSprites();
            SetCurrentSounds();
        }

        if (burakoyunda)
        {
            CharacterManager.Instance.index = 12;
        }
        else
        {
            CharacterManager.Instance.index = index;
        }

        if (index < 0)
        {
            index = currentSprites.Count - 1;
        }
        if (index >= currentSprites.Count)
        {
            index = 0;
        }

        if (index == 33 && gm.GameState == GameState.Prepare)
        {
            gm.ui.bb.SetActive(true);
        }
        else
        {
            gm.ui.bb.SetActive(false);
        }

        if (s.iron != true)
        {
            GetComponent<SpriteRenderer>().sprite = currentSprites[index];
            CharacterManager.Instance.character = currentSprites[index];
        }
        if (s.iron == true)
        {
            GetComponent<SpriteRenderer>().sprite = mySprite;
        }
    }

    void SetCurrentSprites()
    {
        string currentLanguage = LocalizationSettings.SelectedLocale.Identifier.Code;

        if (currentLanguage.Equals("en-US"))
        {
            currentSprites = englishSprites;
        }
        else
        {
            currentSprites = sprites;
        }
    }

    void SetCurrentSounds()
    {
        string currentLanguage = LocalizationSettings.SelectedLocale.Identifier.Code;

        if (currentLanguage.Equals("en-US"))
        {
            currentCharacterSounds = englishCharacterSounds;
        }
        else
        {
            currentCharacterSounds = characterSounds;
        }
    }


    void ChangeCharacter(int direction)
    {
        int newIndex = index + direction;
        LeanTween.scale(ch, new Vector3(1.1f, 1.1f, 1f), 0.1f).setEase(LeanTweenType.easeInOutCubic).setOnComplete(() => { LeanTween.scale(ch, new Vector3(1f, 1f, 1f), 0.1f).setEase(LeanTweenType.easeInOutCubic); });

        if (newIndex < 0)
        {
            newIndex = currentSprites.Count - 1;
        }
        else if (newIndex >= currentSprites.Count)
        {
            newIndex = 0;
        }

        audioSource.Stop();
        PlayCharacterSound(newIndex);
        index = newIndex;
    }

    public void NextCharacter()
    {
        ChangeCharacter(1);
        bl.Button(ch, nb);
    }

    public void PreviousCharacter()
    {
        ChangeCharacter(-1);
        bl.Button(ch, pb);
    }

    void PlayCharacterSound(int index)
    {
        if (currentCharacterSounds != null && index >= 0 && index < currentCharacterSounds.Length)
        {
            audioSource.PlayOneShot(currentCharacterSounds[index]);
        }
    }
}
