using UnityEngine;

public class TheTerrian : MonoBehaviour
{
    public bool isOwningASpring = true;
    public Sprite sprite2;
    public Sprite sprite3;

    public void ChangeColor()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        LeanTween.value(gameObject, 0f, 1f, 0.26f)
            .setOnUpdate((float t) =>
            {
                Color color1 = Color.Lerp(new Color(1f, 0.44f, 0f), Color.white, t);
                spriteRenderer.color = color1;
            });

        LeanTween.value(gameObject, 0f, 1f, 0.26f)
            .setOnUpdate((float t) =>
            {
                Color color2 = Color.Lerp(new Color(1f, 0.44f, 0.25f), Color.white, t);
                spriteRenderer.color = color2;

                if (t > 0.5f)
                {
                    spriteRenderer.sprite = sprite2;
                }
            });
    }

    public void Shield()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        LeanTween.value(gameObject, 0f, 1f, 0.26f)
            .setOnUpdate((float t) =>
            {
                Color color1 = Color.Lerp(new Color(0.91f, 1f, 0f), Color.white, t);
                spriteRenderer.color = color1;
            });

        LeanTween.value(gameObject, 0f, 1f, 0.26f)
            .setOnUpdate((float t) =>
            {
                Color color2 = Color.Lerp(new Color(0.91f, 1f, 0f), Color.white, t);
                spriteRenderer.color = color2;

                if (t > 0.5f)
                {
                    spriteRenderer.sprite = sprite3;
                }
            });
    }
}