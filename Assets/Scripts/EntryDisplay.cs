using UnityEngine;
using UnityEngine.UI;

public class EntryDisplay : MonoBehaviour
{
    [SerializeField] private Text _rankText, _usernameText, _scoreText;
    [SerializeField] private Image _rankCover;
    [SerializeField] private Outline[] otc;

    public void SetEntry(Vertex.Backend.ScoreEntry entry)
    {
        // NOTE: ScoreEntry defines 'rank', 'username', 'score', 'date' in lowercase
        _rankText.text     = entry.rank.ToString();
        _usernameText.text = entry.username;
        _scoreText.text    = entry.score.ToString();

        otc = _rankCover.GetComponents<Outline>();

        // Convert stored Unix epoch (seconds) to a DateTime if you want to use it:
        var dateTime = new System.DateTime(1970, 1, 1, 
                                           0, 0, 0, 
                                           System.DateTimeKind.Utc)
                            .AddSeconds(entry.date);

        // Highlight background if this entry belongs to the local user
        if (entry.IsMine())
            GetComponent<Image>().color = Color.yellow;

        // Apply special decoration if it's rank 1/2/3
        switch (entry.rank)
        {
            case 1:
                _rankCover.color        = Color.yellow;
                otc[0].effectColor      = HexToColor("E7E733");
                otc[1].effectColor      = HexToColor("A6AB50");
                break;

            case 2:
                _rankCover.color        = Color.white;
                otc[0].effectColor      = HexToColor("EAEAEA");
                otc[1].effectColor      = HexToColor("C8C8C8");
                break;

            case 3:
                _rankCover.color        = HexToColor("CF834A");
                otc[0].effectColor      = HexToColor("B36E3A");
                otc[1].effectColor      = HexToColor("8A5127");
                break;

            // (Optional) default: no special styling
        }
    }

    private Color HexToColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString("#" + hex, out var c))
            return c;
        return Color.white;
    }
}
