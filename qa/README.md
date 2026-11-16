# Vizualni QA — Članarina

Manifest pokriva 40 ekrana, Windows, Android i zasebnu Windows DEMO izvedbu i pet obveznih stanja (zadano, prazno, popunjeno, validacijska pogreška, dijalog): 600 pojedinačnih provjera. Sve su zasad označene **unverified** jer stvarne referentne snimke svakog ekrana još nisu dostupne. SVG marketinške ilustracije nisu prihvatljive kao dokaz pixel-perfect rezultata.

Za svaku kombinaciju potrebni su stvarni screenshot i odobren referentni PNG istih dimenzija, OS-a, DPI-ja, jezika, theme i font scale. Datoteke su na putanjama qa/baselines/{platform}/{screen}/{state}.png i qa/captures/... .

Provjera manifesta: python3 scripts/visual_audit.py --validate-manifest

Potpuna provjera: python3 scripts/visual_audit.py --audit-all (mora pasti ako bilo koji PNG nedostaje ili se razlikuje makar jedan kanal jednoga piksela).

Provjera konkretnih slika: python3 scripts/visual_audit.py --reference ref.png --actual capture.png --diff diff.png

Potreban je Python Pillow paket za obradu PNG slika. Kod ne mijenja reference niti izmišlja rezultate. Dizajn na desktopu i mobitelu ima zasebne odobrene reference: identičnost značajki i stilova ne znači isti fizički raspored na različitim veličinama zaslona.

Demo je treći profil sa zasebnim referencama za svaki zaslon i stanje, jer sadrži DEMO oznaku i izmišljene podatke. Nije dopušteno koristiti screenshot pune aplikacije kao referencu za Demo.


### Aktualni screenshot QA

Android emulator CI mora generirati `screens/360x640/`,
`screens/412x915/` i `screens/800x1280/`, sa svih 10
navigacijskih zaslona, `member-new-dialog.png`,
`member-validation-error.png` i `coverage.json`.
`--strict` blokira izvješće ako snimanje nekoga od njih ne uspije.
Windows Full/DEMO nastavljaju izvoziti svoje navigacijske PNG-ove
po OS profilu. `captured-unverified` ne znači `pixel-perfect`.
