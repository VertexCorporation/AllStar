// NewsItemUI.cs
using UnityEngine;
using UnityEngine.UI;

// This small helper script is attached to each News Item Prefab.
// Its only job is to hold references to its own UI components,
// making it easy for the main VertexNews script to access and modify them.
public class NewsItemUI : MonoBehaviour
{
    [Header("UI Element References")]
    public Image CoverImage;
    public Text TitleText;
    public Text SummaryText;
    public Text ContentText;
    public Button ReferenceButton;
    public Text ReferenceButtonText; // The text component inside the button
}