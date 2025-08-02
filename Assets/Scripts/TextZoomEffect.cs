using UnityEngine;

public class TextZoomEffect : MonoBehaviour
{
    public GameManager gm;
    private Vector3 originalScale;
    private Vector3 zoomedScale = new Vector3(3f, 3f, 1f);
    private float zoomDuration = 0.1f; // Zoomlama süresi
    private float waitDuration = 0.3f; // Bekleme süresi
    private float zoomOutDuration = 0.1f; // Küçülme süresi
    private float elapsedTime = 0f;
    private bool isZoomingIn = false;
    private bool isWaiting = false;
    private bool isZoomingOut = false;

    void Start()
    {
        originalScale = transform.localScale; // Text objesinin baþlangýç ölçek deðeri
        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        if (isZoomingIn)
        {
            elapsedTime += Time.deltaTime/gm.gameSpeed;

            if (elapsedTime < zoomDuration)
            {
                // Zoomlama
                transform.localScale = Vector3.Lerp(originalScale, zoomedScale, elapsedTime / zoomDuration);
            }
            else if (elapsedTime < zoomDuration + waitDuration)
            {
                // Bekleme süresi
                isWaiting = true;
            }
            else if (elapsedTime < zoomDuration + waitDuration + zoomOutDuration)
            {
                // Küçülme
                float progress = (elapsedTime - (zoomDuration + waitDuration)) / zoomOutDuration;
                transform.localScale = Vector3.Lerp(zoomedScale, Vector3.zero, progress);
            }
            else
            {
                isZoomingIn = false;
                isWaiting = false;
                elapsedTime = 0;
            }
        }
    }

    public void zoom()
    {
        if (!isZoomingIn && !isZoomingOut && !isWaiting)
        {
            transform.localScale = originalScale;
            isZoomingIn = true;
        }
    }
}