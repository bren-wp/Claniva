# Članarina: provjera pokretanja i siguran oporavak

## Zašto prethodni zeleni CI nije bio dovoljan
Kompilacija, stvaranje APK/AAB i NSIS pakiranje ne dokazuju da se aplikacija
pokreće. Sada mora proći pokretanje stvarnog APK-a na emulatoru te
instaliranog i portable Windows izdanja, zasebno za full i DEMO.

## Windows
1. Podržan je Windows 10/11 x64 (minimalno 10.0.19041.0).
2. Za provjeru koristite DEMO verziju (izmišljeni podaci, zasebna baza i bez licence).
3. Portable.exe privremeno izdvaja potrebne WinUI/.NET datoteke.
4. Ako se aplikacija ne otvori, provjerite lokalni zapis:
   %LOCALAPPDATA%\Clanarina\logs\startup.log
   %LOCALAPPDATA%\ClanarinaDemo\logs\startup.log
   Zapis sadrži samo tip pogreške i HRESULT, bez osobnih i financijskih podataka.
5. Pogledajte Event Viewer → Windows Logs → Application ako nema lokalnog zapisa.
   Sačuvajte Exception Code i Faulting Module, bez licence i baze.
6. NE BRIŠITE mapu %LOCALAPPDATA%\Clanarina\data: u njoj su korisnički zapisi.

## Android
1. Na uređaj instalirajte APK. AAB je namijenjen Play Console distribuciji.
2. Testna izdanja bez trajnog potpisnog ključa dobivaju novi certifikat u svakom CI
   izdanju. Ažuriranje preko postojećeg izdanja zato neće biti moguće.
   **Ne deinstalirajte staru aplikaciju bez provjerene sigurnosne kopije.**
3. Ako lokalna baza nije dostupna, aplikacija prikazuje zaslon za ponovni pokušaj
   bez brisanja podataka.
4. U slučaju rušenja koristite Android Studio Logcat ili ADB:
   - adb logcat -c
   - pokrenite aplikaciju
   - adb logcat -d -s AndroidRuntime:E ClanarinaStartup:E
   Nikada javno ne objavljujte licencne ključeve ili sadržaj baze.

## Testovi
- Native runtime launch smoke: stvarni Android emulator i Windows desktop prozori
- Release gate: pokretanje baš isporučenog APK-a i sva četiri Windows paketa
- Android screenshot i logcat: qa/runtime/android
- Windows lokalni startup zapisi: qa/runtime/windows
- Nema Release objave ako bilo koji obvezni runtime test nije uspješan

Stvarni vizualni QA za svaki ekran zaseban je kriterij. Pokretanje ne potvrđuje
1:1 podudarnost svih ekrana s odobrenim referencama.
