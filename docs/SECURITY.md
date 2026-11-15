# Članarina — sigurnost i granice prvog izdanja

## Lokalni podaci
Obje aplikacije spremaju članove, zaduženja i uplate lokalno, bez registracije i servera. Trenutačne razvojne SQLite baze nisu šifrirane ključem aplikacije; to je **otvoren sigurnosni zadatak** prije obrade stvarnih podataka. Ne unositi stvarne podatke djece, članova ili financija dok zaštita baze, zaštita kopija, migracije i recovery nisu verificirani.

## Licenciranje
PHP servis koristi JSON pohranu, file lock, privremenu datoteku i rename, uz Ed25519 digitalne potpise. Privatni ključ se nikada ne komitira. Vlasnik hostinga mora postaviti private/ izvan document root, HTTPS, dugačak CLANARINA_THROTTLE_SECRET, datotečne dozvole 0700/0600 i backup izvan javnog direktorija. PHP sodium je obavezan.

### Administracija bez korisničkih računa u aplikacijama
Izdavanje licenci radi se isključivo kroz PHP CLI na privatnom računalu ili administratorskom serveru. Nema javnog administratorskog sučelja bez autentifikacije.

### Online
Klijent stvara zaseban installation Ed25519 par ključeva i šalje samo javni ključ i kupljeni licencni ključ na POST /api/activate.php. Odgovor je potpisan; klijent mora provjeriti javni potpis, product, installation key i rok trajanja bez interneta. **Nativni klijenti tu integraciju još nemaju; backend je prototip.**

### Offline
Korisnik izveze zahtjev JSON (installationPublicKey i platform). Administrator izdaje odgovor preko CLI offline naredbe. Klijent uvozi token, lokalno provjerava potpis i veže aktivaciju uz instalacijski ključ. Bez interneta ne postoji pouzdan trenutan opoziv već izdane licence niti dokaz stvarnog broja offline uređaja.

### Prvi pokus na izoliranom testnom hostingu
1. Postavite document root na licensing/public, a ključeve u licensing/private/ ili izvan web root direktorija.
2. Postavite CLANARINA_THROTTLE_SECRET (najmanje 32 nasumična znaka) u konfiguraciju PHP procesa.
3. Pokrenite php licensing/bin/license.php init, spremite javni ključ i izolirajte privatni ključ.
4. Pokrenite php licensing/bin/license.php issue 2027-12-31 2, licencni ključ predajte kupcu privatno.
5. Testirajte izdavanje, ponovno aktiviranje, seat limit, HTTPS i 429 bez stvarnih korisničkih podataka.

## Financijski model
Saldo je u prototipu samo zbroj svih zaduženja minus sve uplate po članu. Nema alokacija uplata na pojedine obveze, modela preplata, automatizacije periodike ni bankovne potvrde. Taj jednostavni izračun ne predstavlja knjigovodstveno usklađivanje. Nema automatskog statusa "potvrđena bankovna uplata".

## Google Play
Za Android je postavljen targetSdk 36. Prije objave potrebno je provjeriti Play Billing/consumption-only model licenciranja, Data safety, privacy policy, pregledni demo pristup, izvoz/brisanje podataka, sadržaj i dozvole. Nije zatražena ni dobivena Googleova potvrda usklađenosti. Trenutačni prototip namjerno nema INTERNET permission; online aktivacija zahtijevat će novu iteraciju i transparentnu prijavu obrade podataka.

## Raspored sigurnosnog hardeninga
- šifriranje baze i backup-a, testovi oporavka i migracija;
- integracija Ed25519 licence u nativne klijente;
- revizija obračuna, tolerancije na duplice uvoze, idempotency;
- audit biblioteka i license compliance;
- stvarni emulator/device testovi i pixel-accurate vizualne usporedbe;
- odvojene produkcijske verzije Play i direktni APK ako to zahtijeva model naplate.
