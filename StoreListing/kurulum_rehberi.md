# Yayın Kurulum Rehberi

Kod tarafı hazır. Aşağıdaki adımlar Google hesapları ve eklenti kurulumu gerektirdiği için
senin yapman gereken kısımlar. Sırayla gitmek en kolayı.

---

## 1. Gizlilik politikasını yayınla (5 dk)

1. `docs/privacy-policy.html` dosyasını aç; `[GELİŞTİRİCİ ADI]`, `[DEVELOPER NAME]`,
   `[İLETİŞİM E-POSTANIZ]` ve `[YOUR CONTACT EMAIL]` yerlerini doldur. Commit'leyip push'la.
2. GitHub'da depoya git: **Settings > Pages > Build and deployment**:
   Source **Deploy from a branch**, Branch **main**, klasör **/docs** > **Save**.
3. Birkaç dakika sonra politika şu adreste yayında olur:
   `https://recepyldrm.github.io/2DShotter/privacy-policy.html`
4. Bu linki Play Console'da **Politika > Uygulama içeriği > Gizlilik politikası** alanına yapıştır.

> Not: Depo gizli (private) ise GitHub Pages ücretsiz planda çalışmaz; depoyu herkese açık
> yapmak ya da politikayı başka bir yerde yayınlamak gerekir.

---

## 2. Satın alma makbuz doğrulaması (5 dk)

1. Play Console > uygulaman > **Para kazanma > Para kazanma kurulumu** sayfasından
   **Lisans anahtarı**'nı (Base64, "MIIBIjAN..." ile başlar) kopyala.
2. Unity'de **Services > In-App Purchasing > Receipt Validation Obfuscator** menüsünü aç.
3. Anahtarı **Google Play Public License Key** alanına yapıştır > **Obfuscate Google Play License Key**.
4. Unity `Assets/Scripts/UnityPurchasing/generated/GooglePlayTangle.cs` dosyasını üretir.
   Doğrulama bu dosyayı otomatik bulur; başka bir şey yapmana gerek yok.

---

## 3. Google Play Games: liderlik tablosu + bulut kayıt (20-30 dk)

1. **Eklentiyi kur:** https://github.com/playgameservices/play-games-plugin-for-unity/releases
   adresinden en son `GooglePlayGamesPlugin-x.x.x.unitypackage` dosyasını indir,
   Unity'de **Assets > Import Package > Custom Package** ile içe aktar.
   Kurulunca kod otomatik etkinleşir (GPGS_INSTALLED tanımı kendiliğinden eklenir).
2. **Play Console:** uygulaman > **Büyüme > Play Games Hizmetleri > Kurulum ve yönetim > Yapılandırma**.
   Yeni bir oyun hizmetleri projesi oluştur, Android kimlik bilgisi ekle
   (paket adı `com.yildirim.shooter2d` + imzalama sertifikası SHA-1).
3. **Kayıtlı Oyunlar**'ı aç (Yapılandırma sayfasında "Saved Games: Açık").
4. **Liderlik tablosu oluştur:** Başarılar ve liderlik tabloları > **Liderlik tablosu ekle**
   (ad: "En Yüksek Skor", sıralama: büyük olan daha iyi).
5. Tablonun kimliğini kopyala ve `Assets/Scripts/Systems/GooglePlayGamesService.cs` içinde
   `LeaderboardId = ""` satırına yapıştır.
6. Unity'de **Window > Google Play Games > Setup > Android Setup** ekranına
   Play Console'un verdiği kaynak XML'ini yapıştır > **Setup**.
7. **Test kullanıcıları:** Play Games Hizmetleri > Test kullanıcıları'na kendi Gmail adresini ekle.

Oyuncu giriş yapınca ana menüde **LİDERLİK** butonu kendiliğinden görünür; ilerleme
Google hesabına kaydedilir ve telefon değişse de geri gelir.

---

## 4. AdMob: gerçek ödüllü reklam (15-20 dk)

1. https://admob.google.com adresinde hesap aç, **Uygulama ekle** (Android, Google Play'de yayında değil).
2. Uygulamaya **Ödüllü** türünde bir reklam birimi ekle.
3. **Eklentiyi kur:** https://github.com/googleads/googleads-mobile-unity/releases
   adresinden `GoogleMobileAds-vx.x.x.unitypackage` dosyasını indirip içe aktar.
   Kurulunca kod otomatik etkinleşir (ADMOB_INSTALLED tanımı kendiliğinden eklenir).
4. Unity'de **Assets > Google Mobile Ads > Settings**: **Android App ID** alanına AdMob'daki
   uygulama kimliğini (`ca-app-pub-...~...`) yapıştır.
5. Önce test et: kod varsayılan olarak Google'ın **test reklam birimini** kullanır.
   Telefonda "Test Ad" yazan reklam görürsen her şey çalışıyor demektir.
6. Yayından önce kendi reklam biriminin kimliğini (`ca-app-pub-.../...`)
   `Assets/Scripts/Systems/AdMobRewardedAd.cs` içinde `RewardedAdUnitId = ""` satırına yapıştır.
7. **Gizlilik izni (AB kullanıcıları):** AdMob > **Gizlilik ve mesajlaşma** > **GDPR mesajı oluştur**.
   Kod bu mesajı gerektiğinde otomatik gösterir.

> Uyarı: Kendi reklamlarına asla tıklama; AdMob hesabın kapatılabilir. Testte hep test kimliğini kullan.

---

## 5. Play Console beyanları

- **Uygulama içeriği > Reklamlar:** "Evet, uygulamam reklam içeriyor".
- **Veri güvenliği formu:** cihaz kimlikleri (AdMob reklam kimliği), uygulama etkinliği,
  satın alma geçmişi (Google Play). Veriler aktarımda şifrelenir.
- **Hedef kitle:** 13 yaş ve üzeri.
