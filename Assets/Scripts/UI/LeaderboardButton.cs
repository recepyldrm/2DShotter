using UnityEngine;
using UnityEngine.UI;

// Ana menüdeki "LİDERLİK" butonu. Liderlik servisi kullanılabilir
// olduğunda (oyuncu Google Play Games'e giriş yaptığında) görünür.
public class LeaderboardButton : MonoBehaviour
{
    private GameObject buttonRoot;

    void Start()
    {
        UITheme theme = UITheme.Load();

        Transform canvas = UIBuilder.CreateCanvas(transform, "LeaderboardCanvas", 140);

        Button button = UIBuilder.CreateMenuButton(theme, canvas, "LİDERLİK", UIBuilder.MenuSlotLeaderboard, out _);
        button.onClick.AddListener(() =>
        {
            UIBuilder.PlayClick();
            Leaderboards.Show();
        });

        buttonRoot = canvas.gameObject;
        buttonRoot.SetActive(Leaderboards.IsAvailable);
    }

    // Giriş işlemi oyun açıldıktan sonra tamamlanabilir.
    void Update()
    {
        bool available = Leaderboards.IsAvailable;

        if (buttonRoot != null && buttonRoot.activeSelf != available)
        {
            buttonRoot.SetActive(available);
        }
    }
}
