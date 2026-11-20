using UnityEngine;
using TMPro;

public class HighScoreUI : MonoBehaviour
{
    public TextMeshProUGUI highScoreText;

    void Start()
    {
        ShowHighScore();
    }

    void ShowHighScore()
    {
        int highScore =
            PlayerPrefs.GetInt(
                GameKeys.HighScore,
                0
            );

        if (highScoreText != null)
        {
            highScoreText.text =
                Loc.T("EN YÜKSEK SKOR: ") + highScore;
        }
    }
}