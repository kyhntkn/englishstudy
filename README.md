# English Study v1.1

Çevrimdışı İngilizce kelime/cümle çalışma uygulaması (WPF, .NET 8, SQLite).

## Özellikler
- Kelime + örnek cümle + çevirileri, ayrı cümle kayıtları; seviye (A1-C2), sözcük türü, not
- Kategoriler, arama, tür/kategori filtresi, favoriler (★)
- Aralıklı tekrar (Leitner, 5 kutu): bildirimde "Biliyorum / Bilmiyorum / Ertele"
- Belirli aralıklarla küçük bildirim; sessiz saatler (Ayarlar)
- Quiz: çoktan seçmeli (iki yön), yazarak, boşluk doldurma
- İstatistik: kutular, günlük hedef, seri, en çok yanlışlar
- Seslendirme (Windows'un yerleşik İngilizce sesi, çevrimdışı)
- Excel / PDF'e aktarma (ekrandaki liste), Excel'den içe aktarma
- Yedek al / geri yükle, haftalık otomatik yedek
- Sistem tepsisi, Windows ile başlatma
- Veri: `%LocalAppData%\EnglishStudy\data.db` (eski veriler otomatik yeni şemaya taşınır)

## Exe nasıl alınır
GitHub'a yükle > Actions > "Build exe" > Artifacts > `EnglishStudy-win-x64`.
Ya da .NET 8 SDK ile: `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true`

## Microsoft Store paketi
Repo > Actions > "Store paketi (MSIX)" > Run workflow. Bitince Artifacts altındaki `IngilizceDefterim-msix` dosyasındaki `.msix` Partner Center'a yüklenir.
Kimlik bilgileri `AppxManifest.xml` içindedir. Mağaza metinleri ve simgeler `magaza-bilgileri` klasöründedir. Logolar iş akışında `app.ico` dosyasından üretilir.
