using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Tools > Analitik Özeti
// Editor'da / Development Build'de kaydedilen olaylardan denge özeti çıkarır:
// oyuncular hangi seviyede ölüyor / bırakıyor, hangi kart ne sıklıkla seçiliyor.
// Telefondaki dosyayı incelemek için: dosyayı cihazdan alıp aynı klasöre kopyala
// (Android: /sdcard/Android/data/<paket adı>/files/analytics.jsonl).
public class AnalyticsSummaryWindow : EditorWindow
{
    private readonly List<AnalyticsRecord> records = new List<AnalyticsRecord>();
    private Vector2 scroll;

    [MenuItem("Tools/Analitik Özeti")]
    static void Open()
    {
        AnalyticsSummaryWindow window = GetWindow<AnalyticsSummaryWindow>("Analitik Özeti");
        window.Load();
    }

    void OnEnable()
    {
        Load();
    }

    void Load()
    {
        records.Clear();

        string path = LocalFileAnalyticsSink.FilePath;

        if (!File.Exists(path))
            return;

        foreach (string line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                records.Add(JsonUtility.FromJson<AnalyticsRecord>(line));
            }
            catch
            {
                // Bozuk satırı atla
            }
        }
    }

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Yenile"))
        {
            Load();
        }

        if (GUILayout.Button("Klasörü Aç"))
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }

        if (GUILayout.Button("Kayıtları Sil") &&
            EditorUtility.DisplayDialog("Analitik", "Tüm analitik kayıtları silinsin mi?", "Sil", "Vazgeç"))
        {
            File.Delete(LocalFileAnalyticsSink.FilePath);
            Load();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("Dosya: " + LocalFileAnalyticsSink.FilePath, EditorStyles.miniLabel);
        EditorGUILayout.LabelField("Toplam olay: " + records.Count, EditorStyles.miniLabel);

        if (records.Count == 0)
        {
            EditorGUILayout.HelpBox("Henüz kayıt yok. Oyunu Editor'da oynayınca olaylar burada özetlenir.", MessageType.Info);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawRuns();
        DrawCards();
        DrawOther();

        EditorGUILayout.EndScrollView();
    }

    // ==================================
    // OYUNLAR
    // ==================================

    void DrawRuns()
    {
        List<AnalyticsRecord> runs = Of(GameAnalytics.Run_End);
        List<AnalyticsRecord> deaths = Of(GameAnalytics.Death);

        Header("OYUNLAR");

        if (runs.Count == 0)
        {
            EditorGUILayout.LabelField("Biten oyun yok.");
            return;
        }

        int died = runs.Count(r => r.flag);

        EditorGUILayout.LabelField("Oyun sayısı", runs.Count + "  (ölerek: " + died + ", yarıda bırakılan: " + (runs.Count - died) + ")");
        EditorGUILayout.LabelField("Ortalama son seviye", runs.Average(r => r.level).ToString("0.0"));
        EditorGUILayout.LabelField("Ortalama süre", FormatTime(runs.Average(r => r.seconds)));
        EditorGUILayout.LabelField("Ortalama öldürme", runs.Average(r => r.kills).ToString("0"));
        EditorGUILayout.LabelField("Ortalama skor", runs.Average(r => r.score).ToString("0"));

        if (deaths.Count > 0)
        {
            int revived = deaths.Count(d => d.flag);
            EditorGUILayout.LabelField("Reklamla devam etme", revived + " / " + deaths.Count + " ölüm");
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Oyunun bittiği seviye", EditorStyles.boldLabel);
        DrawHistogram(runs.Select(r => r.level));

        // Silah dengesi: hangi silahla ne kadar ilerleniyor
        List<IGrouping<string, AnalyticsRecord>> byWeapon = runs
            .Where(r => !string.IsNullOrEmpty(r.detail))
            .GroupBy(r => r.detail)
            .OrderByDescending(g => g.Average(r => r.level))
            .ToList();

        if (byWeapon.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Silaha göre", EditorStyles.boldLabel);

            foreach (IGrouping<string, AnalyticsRecord> group in byWeapon)
            {
                EditorGUILayout.LabelField(
                    group.Key,
                    group.Count() + " oyun   ort. seviye " + group.Average(r => r.level).ToString("0.0") +
                    "   ort. skor " + group.Average(r => r.score).ToString("0")
                );
            }
        }
    }

    // ==================================
    // KARTLAR
    // ==================================

    void DrawCards()
    {
        Dictionary<string, int> offered = new Dictionary<string, int>();
        Dictionary<string, int> chosen = new Dictionary<string, int>();
        Dictionary<string, int> rerolledAway = new Dictionary<string, int>();

        foreach (AnalyticsRecord record in Of(GameAnalytics.Cards_Offered))
        {
            foreach (string card in (record.detail ?? string.Empty).Split(','))
            {
                if (card.Length > 0)
                    Increment(offered, card);
            }
        }

        foreach (AnalyticsRecord record in Of(GameAnalytics.Card_Rerolled))
        {
            // Yenilemeyle gelen kart da oyuncuya sunulmuş sayılır.
            if (!string.IsNullOrEmpty(record.detail))
                Increment(offered, record.detail);

            if (!string.IsNullOrEmpty(record.card))
                Increment(rerolledAway, record.card);
        }

        foreach (AnalyticsRecord record in Of(GameAnalytics.Card_Chosen))
        {
            Increment(chosen, record.card);
        }

        Header("KARTLAR");

        if (offered.Count == 0)
        {
            EditorGUILayout.LabelField("Kart verisi yok.");
            return;
        }

        EditorGUILayout.LabelField("Seçilme oranı = seçildi / sunuldu. Düşük oran: kart zayıf ya da sıkıcı olabilir.", EditorStyles.wordWrappedMiniLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Kart", EditorStyles.boldLabel, GUILayout.Width(150));
        EditorGUILayout.LabelField("Sunuldu", EditorStyles.boldLabel, GUILayout.Width(70));
        EditorGUILayout.LabelField("Seçildi", EditorStyles.boldLabel, GUILayout.Width(70));
        EditorGUILayout.LabelField("Yenilendi", EditorStyles.boldLabel, GUILayout.Width(70));
        EditorGUILayout.LabelField("Seçilme oranı", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        foreach (KeyValuePair<string, int> entry in offered.OrderByDescending(e => Rate(chosen, e.Key, e.Value)))
        {
            chosen.TryGetValue(entry.Key, out int picks);
            rerolledAway.TryGetValue(entry.Key, out int rerolls);
            float rate = Rate(chosen, entry.Key, entry.Value);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(entry.Key, GUILayout.Width(150));
            EditorGUILayout.LabelField(entry.Value.ToString(), GUILayout.Width(70));
            EditorGUILayout.LabelField(picks.ToString(), GUILayout.Width(70));
            EditorGUILayout.LabelField(rerolls.ToString(), GUILayout.Width(70));
            Rect bar = GUILayoutUtility.GetRect(100, 16, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(bar, rate, (rate * 100f).ToString("0") + "%");
            EditorGUILayout.EndHorizontal();
        }
    }

    // ==================================
    // DİĞER
    // ==================================

    void DrawBosses()
    {
        List<AnalyticsRecord> spawned = Of(GameAnalytics.Boss_Spawned);

        if (spawned.Count == 0)
            return;

        List<AnalyticsRecord> defeated = Of(GameAnalytics.Boss_Defeated);

        Header("BOSS'LAR");
        EditorGUILayout.LabelField("Yenilme oranı düşükse boss çok güçlü olabilir.", EditorStyles.wordWrappedMiniLabel);

        foreach (IGrouping<int, AnalyticsRecord> group in spawned.GroupBy(r => r.value).OrderBy(g => g.Key))
        {
            List<AnalyticsRecord> wins = defeated.Where(d => d.value == group.Key).ToList();
            float rate = (float)wins.Count / group.Count();
            string time = wins.Count > 0 ? "  ort. " + FormatTime(wins.Average(w => w.seconds)) : string.Empty;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(group.Key + ". boss", GUILayout.Width(80));
            Rect bar = GUILayoutUtility.GetRect(100, 16, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(bar, rate, wins.Count + " / " + group.Count() + " yenildi" + time);
            EditorGUILayout.EndHorizontal();
        }
    }

    void DrawOther()
    {
        DrawBosses();

        Header("DİĞER");

        foreach (IGrouping<string, AnalyticsRecord> group in Of(GameAnalytics.Ad_Watched).GroupBy(r => r.detail))
        {
            EditorGUILayout.LabelField("Reklam: " + group.Key, group.Count(r => r.flag) + " ödüllü / " + group.Count() + " açılan");
        }

        foreach (IGrouping<string, AnalyticsRecord> group in Of(GameAnalytics.Purchase).GroupBy(r => r.detail))
        {
            EditorGUILayout.LabelField("Satın alma: " + group.Key, group.Count().ToString());
        }

        foreach (IGrouping<string, AnalyticsRecord> group in Of(GameAnalytics.Permanent_Upgrade).GroupBy(r => r.detail))
        {
            EditorGUILayout.LabelField("Kalıcı yükseltme: " + group.Key, "en yüksek seviye " + group.Max(r => r.value));
        }

        EditorGUILayout.LabelField("Günlük ödül alındı", Of(GameAnalytics.Daily_Reward).Count.ToString());
        EditorGUILayout.LabelField("Başarım ödülü alındı", Of(GameAnalytics.Achievement).Count.ToString());
    }

    // ==================================
    // YARDIMCILAR
    // ==================================

    List<AnalyticsRecord> Of(string eventName) =>
        records.Where(r => r.e == eventName).ToList();

    static void Increment(Dictionary<string, int> counts, string key)
    {
        if (string.IsNullOrEmpty(key))
            return;

        counts.TryGetValue(key, out int count);
        counts[key] = count + 1;
    }

    static float Rate(Dictionary<string, int> chosen, string card, int offeredCount)
    {
        chosen.TryGetValue(card, out int picks);
        return offeredCount > 0 ? Mathf.Clamp01((float)picks / offeredCount) : 0f;
    }

    static void DrawHistogram(IEnumerable<int> values)
    {
        List<IGrouping<int, int>> groups = values.GroupBy(v => v).OrderBy(g => g.Key).ToList();
        int max = groups.Max(g => g.Count());

        foreach (IGrouping<int, int> group in groups)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Seviye " + group.Key, GUILayout.Width(80));
            Rect bar = GUILayoutUtility.GetRect(100, 16, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(bar, (float)group.Count() / max, group.Count() + " oyun");
            EditorGUILayout.EndHorizontal();
        }
    }

    static void Header(string title)
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }

    static string FormatTime(double seconds)
    {
        int total = Mathf.RoundToInt((float)seconds);
        return (total / 60) + " dk " + (total % 60) + " sn";
    }
}
