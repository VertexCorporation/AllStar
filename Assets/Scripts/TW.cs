using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TW : MonoBehaviour
{
    [SerializeField] public Text[] texts;
    [SerializeField] public float timeBtwChars = 0.048f;
    [SerializeField] private string leadingChar = "";

    private Coroutine[] coroutines;
    private string[] writers;

    private void Start()
    {
        ClearTexts();
    }
    private void Awake()
    {
        coroutines = new Coroutine[texts.Length];
        writers = new string[texts.Length];

        for (int i = 0; i < texts.Length; i++)
        {
            writers[i] = texts[i].text;
        }
    }

    public void StartTypewriter(int index)
    {
        if (coroutines[index] == null)
        {
            coroutines[index] = StartCoroutine(TWT(index));
        }
    }

    private void ClearTexts()
    {
        foreach (Text text in texts)
        {
            text.text = "";
        }
    }

    public IEnumerator TWT(int index)
    {
        Text currentText = texts[index];

        foreach (char c in writers[index])
        {
            if (currentText.text.Length > 0)
            {
                currentText.text = currentText.text.Substring(0, currentText.text.Length - leadingChar.Length);
            }

            currentText.text += c;
            currentText.text += leadingChar;

            yield return new WaitForSeconds(timeBtwChars);
        }

        writers[index] = "";
        coroutines[index] = null;
    }
}
