using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Effect : MonoBehaviour
{
    public Text plusOneText;
    public Text plusOneText2;
    public float moveSpeed = 1.0f;
    public float fadeSpeed = 1.0f;
    public float destroyHeight = 5.0f;
    public float fadeInDuration = 1.0f;
    private bool isEffecting = false;
    private bool isEffecting2 = false;

    private Vector3 initialPosition;
    private Color initialColor;
    private Vector3 initialPosition2;
    private Color initialColor2;

    void Start()
    {
        initialPosition = plusOneText.rectTransform.position;
        initialColor = plusOneText.color;
        plusOneText.color = new Color(initialColor.r, initialColor.g, initialColor.b, 0.0f); // Opaklýðý sýfýrla
        initialPosition2 = plusOneText2.rectTransform.position;
        initialColor2 = plusOneText2.color;
        plusOneText2.color = new Color(initialColor.r, initialColor.g, initialColor.b, 0.0f); // Opaklýðý sýfýrla
    }

    public void Effecting()
    {
        if (!isEffecting) // Effecting zaten çalýþmýyorsa devam et
        {
            StartCoroutine(MoveAndFadeText());
        }
    }

    public void Effecting2()
    {
        if (!isEffecting2) // Effecting zaten çalýþmýyorsa devam et
        {
            StartCoroutine(MoveAndFadeText2());
        }
    }

    IEnumerator MoveAndFadeText()
    {
        isEffecting = true; // Effecting baþladýðýnda bayraðý true yap

        float elapsedTime = 0.0f;

        while (elapsedTime < 1f)
        {
            float yOffset = Mathf.Lerp(0, destroyHeight, elapsedTime);
            plusOneText.rectTransform.position = initialPosition + new Vector3(0, yOffset, 0);
            plusOneText.color = new Color(initialColor.r, initialColor.g, initialColor.b, elapsedTime);
            elapsedTime += Time.deltaTime * moveSpeed;
            yield return null;
        }

        StartCoroutine(FadeOutText());

        plusOneText.rectTransform.position = initialPosition + new Vector3(0, destroyHeight, 0);
    }

    IEnumerator FadeOutText()
    {
        float elapsedTime = 1f;

        while (elapsedTime > 0.0f)
        {
            plusOneText.color = new Color(initialColor.r, initialColor.g, initialColor.b, elapsedTime);
            elapsedTime -= Time.deltaTime * fadeSpeed;
            yield return null;
        }

        plusOneText.color = new Color(initialColor.r, initialColor.g, initialColor.b, 0.0f); // Opaklýðý sýfýrla
        plusOneText.rectTransform.position = initialPosition;

        isEffecting = false; // Effecting tamamlandýðýnda bayraðý false yap
    }

    IEnumerator MoveAndFadeText2()
    {
        isEffecting2 = true; // Effecting baþladýðýnda bayraðý true yap

        float elapsedTime = 0.0f;

        while (elapsedTime < 1f)
        {
            float yOffset = Mathf.Lerp(0, destroyHeight, elapsedTime);
            plusOneText2.rectTransform.position = initialPosition2 + new Vector3(0, yOffset, 0);
            plusOneText2.color = new Color(initialColor2.r, initialColor2.g, initialColor2.b, elapsedTime);
            elapsedTime += Time.deltaTime * moveSpeed;
            yield return null;
        }

        StartCoroutine(FadeOutText2());

        plusOneText2.rectTransform.position = initialPosition2 + new Vector3(0, destroyHeight, 0);
    }

    IEnumerator FadeOutText2()
    {
        float elapsedTime = 1f;

        while (elapsedTime > 0.0f)
        {
            plusOneText2.color = new Color(initialColor2.r, initialColor2.g, initialColor2.b, elapsedTime);
            elapsedTime -= Time.deltaTime * fadeSpeed;
            yield return null;
        }

        plusOneText2.color = new Color(initialColor2.r, initialColor2.g, initialColor2.b, 0.0f); // Opaklýðý sýfýrla
        plusOneText2.rectTransform.position = initialPosition2;

        isEffecting2 = false; // Effecting tamamlandýðýnda bayraðý false yap
    }
}