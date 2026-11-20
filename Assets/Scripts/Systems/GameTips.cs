using UnityEngine;

// Oyun sonu özetinde ve duraklatma menüsünde gösterilen kısa ipuçları.
public static class GameTips
{
    private static readonly (string tr, string en)[] Tips =
    {
        ("Yanma hasarı kalkanları deler.", "Burn damage goes through shields."),
        ("Yıkıcı boss yere vurmaya hazırlanırken ona yüklen: yeterince hasar verirsen sersemler.", "Hit the Smasher hard while it winds up: deal enough damage and it gets stunned."),
        ("Nişancılar uzaktan ateş eder; önce onları temizle.", "Gunners shoot from range; clear them first."),
        ("Bölünen düşmanlara patlayıcı ve delici mermiler çok etkilidir.", "Explosive and piercing bullets are great against splitters."),
        ("KALKAN KIRICI kartı kalkanlı düşmanları ve Muhafız boss'u çabuk açar.", "The SHIELD BREAKER card opens shielded enemies and Guardian bosses fast."),
        ("Yenilenen boss yanarken iyileşemez. Yakıcı mermi ya da yakıcı drone kullan.", "The Regenerator can't heal while burning. Use burning bullets or a burning drone."),
        ("Zor modda düşmanlar sertleşir ama %40 daha fazla altın kazanırsın.", "Hard mode toughens enemies but pays 40% more gold."),
        ("Her drone görünümünün kendi yeteneği var. Mağazada adının altında yazar.", "Every drone skin has its own ability. It's listed under its name in the shop."),
        ("SİLAHLAR ekranından sahip olduğun silahları 5 seviyeye kadar güçlendirebilirsin.", "You can upgrade owned weapons up to level 5 in the WEAPONS screen."),
        ("Beğenmediğin kartı YENİLE ile değiştir; kalıcı ŞANSLI EL geliştirmesi ücretsiz yenileme verir.", "Swap cards you don't like with REROLL; the LUCKY HAND upgrade gives free rerolls."),
        ("Her 10 seviyede yeni bir bölüm başlar ve kurallar değişir.", "A new stage with new rules begins every 10 levels."),
        ("Görevler ve oyunlar sezon puanı kazandırır; sezon seviyeleri ödül açar.", "Quests and runs earn season points; season tiers unlock rewards."),
        ("Günlük meydan okumada herkes aynı silah ve kurallarla oynar.", "In the daily challenge everyone plays with the same weapon and rules."),
        ("Kritik vuruş şansını artıran kartlar keskin nişancı tüfeğiyle çok iyi gider.", "Critical chance cards pair really well with the sniper rifle.")
    };

    public static string Random()
    {
        (string tr, string en) tip = Tips[UnityEngine.Random.Range(0, Tips.Length)];
        return Loc.Pick("İpucu: ", "Tip: ") + Loc.Pick(tip.tr, tip.en);
    }
}
