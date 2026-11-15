# Vizualni QA — Članarina

Manifest pokriva 40 ekrana, dvije nativne aplikacije i pet obveznih stanja (zadano, prazno, popunjeno, validacijska pogreška, dijalog): 400 pojedinačnih provjera. Sve su zasad označene **unverified** jer stvarne referentne snimke svakog ekrana još nisu dostupne. SVG marketinške ilustracije nisu prihvatljive kao dokaz pixel-perfect rezultata.

Za svaku kombinaciju potrebni su stvarni screenshot i odobren referentni PNG istih dimenzija, OS-a, DPI-ja, jezika, theme i font scale. Datoteke su na putanjama qa/baselines/{platform}/{screen}/{state}.png i qa/captures/... .

Provjera manifesta: python3 scripts/visual_audit.py --validate-manifest

Potpuna provjera: python3 scripts/visual_audit.py --audit-all (mora pasti ako bilo koji PNG nedostaje ili se razlikuje makar jedan kanal jednoga piksela).

Provjera konkretnih slika: python3 scripts/visual_audit.py --reference ref.png --actual capture.png --diff diff.png

Potreban je Python Pillow paket za obradu PNG slika. Kod ne mijenja reference niti izmišlja rezultate. Dizajn na desktopu i mobitelu ima zasebne odobrene reference: identičnost značajki i stilova ne znači isti fizički raspored na različitim veličinama zaslona.
