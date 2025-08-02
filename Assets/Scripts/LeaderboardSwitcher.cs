/***************************************************************************
 *  LeaderboardSwitcher.cs (2025-06-22 - DEFINITIVE & FINAL)
 *  -----------------------------------------------------------------------
 *  • REPAIRED: Correctly uses SetActive() on GameObjects to properly
 *    trigger the OnEnable/OnDisable lifecycle in the managers, ensuring
 *    cache is cleared and loading animations are shown on every switch.
 ***************************************************************************/

using UnityEngine;
using UnityEngine.UI;

public class LeaderboardSwitcher : MonoBehaviour
{
    [Header("View Objects (CRITICAL - Assign Correctly!)")]
    [SerializeField] private GameObject seasonViewObject;
    [SerializeField] private GameObject highscoresViewObject;

    [Header("Bounce")]
    [SerializeField] private BuneLean bl;
    [SerializeField] private GameObject bounceRoot;
    [SerializeField] private GameObject toggleBtn;

    [Header("Misc UI")]
    [SerializeField] private GameObject[] seasonExtras;
    [SerializeField] private GameObject[] highExtras;
    [SerializeField] private Text toggleLabel;

    private bool showingSeason = true;
    private bool isSwitching = false;
    private bool _firstPaintDone;

    void Start()
    {
        // Start by showing the season leaderboard by default.
        ShowSeason();
    }

    public void Toggle()
    {
        if (isSwitching) return;
        isSwitching = true;

        if (showingSeason) ShowHigh();
        else ShowSeason();

        isSwitching = false;
    }

    public void ShowSeason()
    {
        if (seasonViewObject) seasonViewObject.SetActive(true);
        if (highscoresViewObject) highscoresViewObject.SetActive(false);

        SetExtras(seasonExtras, true);
        SetExtras(highExtras, false);
        if (toggleLabel) toggleLabel.text = "Highscores";
        showingSeason = true;

        if (_firstPaintDone) FireBounce();
        _firstPaintDone = true;
    }

    private void ShowHigh()
    {
        if (seasonViewObject) seasonViewObject.SetActive(false);
        if (highscoresViewObject) highscoresViewObject.SetActive(true);

        SetExtras(seasonExtras, false);
        SetExtras(highExtras, true);
        if (toggleLabel) toggleLabel.text = "Season";
        showingSeason = false;

        if (_firstPaintDone) FireBounce();
        _firstPaintDone = true;
    }

    private void FireBounce()
    {
        if (bl && bounceRoot && toggleBtn) bl.Button(bounceRoot, toggleBtn);
        else Debug.LogWarning("[LeaderboardSwitcher] Bounce skipped - references missing.");
    }

    private static void SetExtras(GameObject[] list, bool on)
    {
        if (list == null) return;
        foreach (var go in list) if (go) go.SetActive(on);
    }
}