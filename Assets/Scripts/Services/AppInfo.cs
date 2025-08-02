using UnityEngine;

public class AppInfo : MonoBehaviour
{
    public static AppInfo Instance;

    // App-specific metadata
    public string APP_NAME = "All Star";

    public string APPSTORE_ID = "[YOUR_APPSTORE_ID";
    // App Store id

    public string BUNDLE_ID = "";
    // app bundle id

    [HideInInspector]
    public string APPSTORE_LINK = "itms-apps://itunes.apple.com/app/id";
    // App Store link

    [HideInInspector]
    public string PLAYSTORE_LINK = "https://play.google.com/store/apps/details?id=com.VertexGames.AllStar";
    // Google Play store link

    [HideInInspector]
    public string APPSTORE_SHARE_LINK = "https://itunes.apple.com/app/id";
    // App Store link

    [HideInInspector]
    public string PLAYSTORE_SHARE_LINK = "https://play.google.com/store/apps/details?id=";
    // Google Play store link

    public string PLAYSTORE_HOMEPAGE = "[YOUR_GOOGLEPLAY_PUBLISHER_NAME]";
    // e.g https://play.google.com/store/apps/developer?id=[PUBLISHER_NAME]

    public string SUPPORT_EMAIL = "vertexgames23@gmail.com";

    [Header("Set the target frame rate, pass -1 to use platform default frame rate")]
    public int targetFrameRate = 60;
    // !IMPORTANT: in this particular game, we need 60fps for smooth motion on mobiles (it's okay to put a bigger number though).

    void Start()
    {
        APPSTORE_LINK += APPSTORE_ID;
        PLAYSTORE_LINK += BUNDLE_ID;
        APPSTORE_SHARE_LINK += APPSTORE_ID;
        PLAYSTORE_SHARE_LINK += BUNDLE_ID;

        Application.targetFrameRate = targetFrameRate;
    }
}
