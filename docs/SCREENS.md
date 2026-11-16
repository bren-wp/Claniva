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
