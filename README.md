# English Study

Çevrimdışı İngilizce kelime/cümle çalışma uygulaması (WPF, .NET 8, SQLite).

## Özellikler
- Kelime + örnek cümle + çevirileri, ayrı cümle kayıtları
- Kategoriler, arama ve filtreleme
- Belirli aralıklarla (1/5/10/15/30/60 dk) karışık sırayla küçük bildirim penceresi
- Excel'e aktarma, yedek alma / geri yükleme
- Sistem tepsisi, Windows ile başlatma
- Veri: `%LocalAppData%\EnglishStudy\data.db`

## Exe nasıl alınır (en kolay yol)
1. Bu klasörü GitHub'a yükle (repo kök dizininde `EnglishStudy.csproj` olmalı).
2. Repo > **Actions** sekmesi > "Build exe" tamamlanınca > **Artifacts** > `EnglishStudy-win-x64` indir.
3. Zip içindeki `EnglishStudy.exe` kurulum gerektirmeden çalışır.

## Bilgisayarda derleme
.NET 8 SDK kurulu olmalı:
```
dotnet run
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```
