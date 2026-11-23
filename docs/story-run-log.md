# Cinderreach – Story-Laufprotokoll (cinderreach-deutsch-story S7)

Stand: 2026-11-23, Branch `feature/cinderreach-deutsch-story`.

## Belege

| Nachweis | Ergebnis |
|---|---|
| Kompilierlauf (`scripts/unity-warmup.sh`) | grün (Session 1, Skripte gebunden) |
| EditMode voll | 496/496 bestanden, 0 fehlgeschlagen |
| PlayMode voll | 284/284 bestanden, 0 fehlgeschlagen |
| `SandboxBuilder.VerifyDiabloZone` | OK (Szene mit Ottilie, Merle und 5 Fundstücken) |
| Neue Story-Tests (`StoryStateTests`) | Katalog vollständig (alle Quest-/Dialogtexte), H1-Fluss, Catch-up bei Aktende, Talk-Plan, Save-/Snapshot-Roundtrip ohne Rückschritt, Kill-Target-Normalisierung, Fuhrbuch-Zähler |
| Save-Migration v12 → v13 | `SaveMigrationTests.V12_GetsEmptyStoryState`: migriert, leere Story-Listen, danach Catch-up aus dem Aktstand |
| Lokalisierungs-Lint | 0 fehlende Keys, 0 ungelistete spielersichtbare Literale (`loc-pending.txt` leer) |
| Session-Autoplay (G-DIABLO06) | grün (Exit 0): Route durch Akt I und II inkl. Ofenmutter und Glasmacher, deutsche Abschlusszeile „Zeit 130 s • Erschlagen 27 • Gold 153 • Bester Fund: Seltener Hort des Abts“, keine Blockade an Dialogen |
| Koop-Trio (`COOP_CLIENTS=2`) | grün (Exit 0, alle Phasen town→boss3, 0 Exceptions). Alle drei Saves v13 mit identischem Queststand: h1–h4 erledigt (Status 4, Zähler voll), h5/n1/n2/n3/h6 verfügbar — Host-Snapshot und Max-Merge synchron |
| Windows-Shotcheck | 60/60 Screenshots erzeugt. Neu: `26_story_dialog.png` (Ottilie Wachs, H1-Angebot), `27_story_dialog_2.png`, `28_story_questlog.png` (Fuhrbuch mit H1 und HUD-Zielzeile). Deutsche UI belegt u. a. über `19_vendor_open.png` (Ilsabe Krumm • Handel, deutsche Itemnamen und Tooltips). Exit 2 des Skripts: Timeout beim Beenden nach vollständiger Shot-Liste |
