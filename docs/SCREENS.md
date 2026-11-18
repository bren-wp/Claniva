# Popis ekrana i vizualni QA plan

## Kanonski moduli
Početak/splash; jezik i tema; izbor online/offline aktivacije; online unos ključa; offline zahtjev; uvoz potpisanog odgovora; status licence; nova organizacija; nadzorna ploča; odabir organizacije; popis članova; novi/uredi član; detalji člana; roditelji/skrbnici; grupe; planovi članarina; nova/uredi članarina; zaduženja; pregled uplata; nova uplata; detalji uplate; dugovanja; uplatnice HUB-3; EPC QR; bankovni izvodi; uvoz izvoda; usklađivanje; izvještaji; izvoz u PDF/CSV; lokalni podsjetnici; postavke; PIN/biometrija; sigurnosne kopije; uvoz podataka; izvoz podataka; dnevnik promjena; brisanje podataka; pomoć; informacije o aplikaciji.

## Paritet
Sve funkcionalne cjeline moraju postojati na Windowsu i Androidu, s identičnim:
- nazivima i ponašanjem polja i akcija
- pravilima validacije i obračuna iznosa u centima
- statusnim stanjima i pravilima filtriranja
- porukama o uspjehu, pogrešci i praznim stanjima
- bojama, logotipom, ikonografijom, tipografskom hijerarhijom i tokenima

Ne znači isti broj piksela ili identičan raspored na desktopu i mobitelu; prilagodljive rasporede dokumentirati zasebnim referencama.

## Dokazni postupak prije tvrdnje "pixel-perfect"
Za svaki ekran i stanje (normalno, prazno, validacijska greška, popunjeno, modalni dijalog):
1. zabilježiti rezoluciju, DPI, font scale, OS/theme i verziju;
2. snimiti stvarni render na emulatoru ili fizičkom uređaju;
3. spremiti očekivani referentni prikaz istih dimenzija i skale;
4. izračunati per-pixel RGB/MAE i SSIM, uz maskiranje samo stvarno dinamičkih elemenata;
5. ručno pregledati najveća odstupanja, kontrast, fokus i touch targets;
6. blokirati izdanje dok kritični zasloni ne prođu dogovorene pragove.

**Status:** SVG datoteke branding/preview-*.svg su konceptualne ilustracije, a ne potvrđene snimke izvršavanja. Projekt još nema empirijski potvrđen 1:1 rezultat. Evidencija se vodi u qa/screen-matrix.csv.

## Windows Demo

Za svaki Windows ekran DEMO mora imati zasebnu referentnu PNG sliku, jasno vidljivu DEMO oznaku, izmišljene članove i uplate te raditi bez ključa. Popis izvora mora zasebno razlikovati Windows full i Windows demo. Trenutačna matrica pokriva 600 kombinacija (40 ekrana × 5 stanja × 3 varijante). One su nedokazane sve dok stvarni screenshotovi ne prođu usporedbu.


## Prioritized native form QA — 16. 11. 2026.

The Windows payment dialog now avoids a fixed 260px member selector and
supports type-to-select within a bounded dropdown. Android charge/payment
dialogs use a bounded searchable member picker, verify that the chosen
member exists, and adjust for the soft keyboard. Android member inputs
fill their dialog's width and limit detail history rendering to 50 entries.

These are **implemented layout changes**, not approved 1:1 comparisons.
The inventory remains 600 states with no approved baseline/capture pair
certified pixel-perfect. Next captures should cover Windows Full/DEMO
payment and member dialogs at 560px, 700px and default desktop widths;
Android dialogs at 360dp, 412dp and tablet size with keyboard open,
validation error, empty list, and 50+ members.

## Stvarne native snimke i ikonografija — 16. 11. 2026.

Iz CI artefakta Windows 2022 (`windows-startup-evidence-windows-2022`)
potvrđeno je 10/10 stvarnih PNG snimki Full WinUI modula. Snimke imaju
status `captured-unverified`; nemaju odobrene referentne PNG slike, pa
**nisu** pixel-perfect potvrda.

Pregled je otkrio pogrešno mapiranje Segoe MDL2 glifova. Zaduženja su sada
označena kalendarskim simbolom (`E8BF`), evidentirane uplate platnom
karticom (`E8C7`), a bankovni izvodi simbolom banke (`E825`).
Automatizirane galerije prikupljaju zasebno 10 modula Windows Full/DEMO
te 10 Android modula, uz izvještaj o nedostajućim kadrovima.


## Iteracija: vizualna dosljednost nativnih zaslona (16. 11. 2026.)

Nakon spojenog PR-a #9, izdanje `v0.2.108-dev` u CI-ju je snimilo:
- 10/10 postojećih Android navigacijskih modula pri profilu 412×915 i
  dva stanja dijaloga novog člana;
- 10/10 postojećih Windows Full te 10/10 Windows DEMO navigacijskih modula.

Pregled tih PNG-ova pokazao je Androidove defaultne ljubičasto-sive
površine dijaloga. Nova iteracija prisilno koristi bijelu površinu,
tamnoplavi naslov i robusnu ČLANARINA shemu boja na svim trenutačnim
dijalozima članova, uplata, padajućih izbornika i listova modula.
Android detalji članova sada koriste ista polja, nazive i redoslijed
kao WinUI 3, a Windows izvođenje koristi `hr-HR` jezični profil
neovisno o jeziku CI računala.

Android runtime snimke svih 10 navigacijskih modula i validacijskog
dijaloga obvezne su u tri fizička profila:
`360×640 @160 dpi`, `412×915 @160 dpi`, `800×1280 @160 dpi`.
Na ekstremno uskom `180 dp` profilu dodatno se provjerava preživljavanje
procesa, ali ne postoji odobren 1:1 baseline.

Ovi su profili CI dokazi stvarnog renderiranja, **ne vizualne reference**.
Niti jedna od 600 kombinacija ne smije dobiti oznaku 1:1 bez odobrenog
referentnog PNG-a istog OS-a/DPI-ja, kontroliranih fontova i uspješne
usporedbe svakog piksela. Pojedini moduli iz manifesta još su planirani,
a ne implementirani, i moraju ostati označeni kao `unverified`.

## Strukturna korekcija prema postojećim konceptima — 18. 11. 2026.

Pregledane su **stvarne** CI snimke iz izdanja `v0.2.119-dev`
(Windows Full/DEMO i Android telefon 360/412 dp te tablet 800 dp).
Nadzorna ploča bila je značajno različita od `branding/preview-windows.svg`
i `branding/preview-android.svg`: pet primarnih KPI kartica,
nedostajući statusni krug i graf uplata, nepovezan popis članova na Windows
početnoj stranici. Nije dostatna promjena boja ili fontova.

Izmjene ove iteracije:
- četiri glavne KPI kartice (Članovi, Zaduženo, Evidentirano, Dugovanja);
- kružni **Status članarina** s realnim omjerom pokrivenih zaduženja
  pojedinih članova; preplate su zasebno prikazane;
- dinamički prikaz **Uplate po mjesecima** na temelju stvarnih
  evidentiranih uplata tijekom posljednjih šest mjeseci;
- ujednačen, responzivan raspored Windows i Android panela, a ne
  fiksni konceptualni SVG; bez lažnih postotaka i izmišljenih uplata;
- preostali financijski moduli i dijalozi i dalje zahtijevaju stvarnu
  funkcionalnu te odobrenu vizualnu 1:1 verifikaciju.

**Granica tvrdnje:** konceptualni SVG nije odobren pixel-perfect PNG.
Snimke se razlikuju prema platformi, DPI-ju, fontu, OS-u i datasetu.
Dok vlasnik dizajna ne odobri točne referentne rastere, sva stanja ostaju
`unverified` u `qa/visual-manifest.json`. Dodavanje pravog grafikona
i kružnog prikaza jest funkcionalna i strukturna ispravka, ali nije
dokaz nulte RGB razlike na svim zaslonima.
