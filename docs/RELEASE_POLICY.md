# Obvezni format izdanja Članarine

Svaka objedinjena razvojna iteracija mora imati GitHub Release s valjanim izgrađenim datotekama:

- Clanarina-Android.apk
- Clanarina-Android.aab
- Clanarina-Portable.exe (bez instalacijskog čarobnjaka; samoraspakiravajući exe)
- Clanarina-Setup.exe
- Clanarina-Demo-Portable.exe — neovisna samoraspakiravajuća demo distribucija bez licencnog ključa
- Clanarina-Demo-Setup.exe — neovisni demo instalacijski program, bez aktivacije, s izmišljenim članovima i uplatama (NSIS Modern UI2 s licencnim uvjetima)
- Clanarina-Full-Source.zip (svi izvori za Windows, Android, PHP, web i brending)
- Clanarina-Web-Source.zip (web i brending)
- LICENSE-COMMERCIAL.txt
- SHA256SUMS.txt

GitHub također automatski nudi izvore za svaki tag.

### Potpisivanje Android izdanja
Za produkcijski potpis treba postaviti GitHub Secrets:
CLANARINA_ANDROID_KEYSTORE_BASE64, CLANARINA_ANDROID_STORE_PASS,
CLANARINA_ANDROID_KEY_ALIAS, CLANARINA_ANDROID_KEY_PASS.

Ako taj komplet tajni nije konfiguriran, CI proizvodi Android testni APK/AAB
potpisan PRIVREMENIM KLJUČEM; svako izdanje dobiva drugi ključ. Taj format
ne služi za objavu ili nadogradnje na Google Playu. Ovaj razvojni release mora
biti jasno označen kao **prerelease** i TEST-ONLY.

### Windows
WinUI 3 objavljuje se self-contained. Portable.exe je NSIS samoraspakiravajući
launcher: izdvaja sve nužne datoteke u privremeni direktorij i pokreće aplikaciju,
bez formalne instalacije. Podaci o članovima ostaju u lokalnom korisničkom
AppData direktoriju i ne brišu se uklanjanjem aplikacije.
Setup.exe ima moderni installer i prihvaćanje komercijalne licence.

Windows Authenticode potpis zahtijeva stvarni certifikat; bez njega SmartScreen
može prikazati upozorenje. Ne tvrditi da je datoteka potpisana ako nije.

**Svako izdanje blokirati** ako build, provjera ili pakiranje nije uspješno.
Ne prikazivati razvojne verzije kao produkcijski dovršene, niti marketing prikaze
kao potvrđene screenshotove.

## Zaštita demo podataka i oznake izdanja

Demo je zasebna **compile-time** varijanta (`DemoBuild=true`), `ClanarinaDemo.exe` i vlastiti `LocalAppData/ClanarinaDemo/data/clanarina.db`. Demo ne koristi produkcijsku bazu, ne traži licencni ključ i sadrži 24 izmišljena člana te demonstracijska zaduženja i uplate. U sučelju je jasno označen kao DEMO. Uninstaller demo aplikacije nikada ne dira lokalne podatke pune Članarine. Demo ne sadrži stvarne osobne podatke niti ključeve.

Ne smije se prikazivati da je produkcijska licenca integrirana dok nije implementirana i testirana. Novi release blokira ako bilo koja od šest izvršnih datoteka nedostaje ili je prazna.
