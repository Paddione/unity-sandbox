# Fortschritt

Prozent: 100 %

## Erledigt

- Das Asset-Inventar erfasst 469 Pfade aus 62 Einstiegspunkten einschließlich transitiver Abhängigkeiten und Laufzeitressourcen. Die Herkunft und Einsatzorte stehen in `docs/cinderreach-asset-inventory.tsv`.
- Die einheitliche Ranger-Kleidung ist zurückgenommen (2026-11-24, Nutzerwunsch: Klassen müssen unterscheidbar sein). Jede Klasse trägt wieder ihr Klassen- und Geschlechtsmodell, den gelben Brustschutz trägt nur der Fenwalker. Alle 16 sichtbaren Uniques haben eigene Geometrie und werden anhand ihrer Item-ID an der passenden Körperstelle dargestellt.
- Der Unity-Check in `KayKitAssets.Verify` validiert die Modellabdeckung, Slots, Geometrie und Materialien. Der zusätzliche G-DIABLO02-Check liegt in Dotfiles auf `master` (Commit `c68917a9`). Alle Dotfiles-Suites bestanden.
- Der Linux-Player baute erfolgreich. Der Equipment-Shotcheck erzeugte 19 Aufnahmen für die drei Klassen und 16 Uniques in `docs/cinderreach-equipment-shots/`.
- Asset-Metadatenprüfung, EditMode 496/496 und PlayMode 287/287 bestanden. PlayMode deckt Anlegen, Wechseln, Ablegen und die serverseitige Veröffentlichung der sichtbaren Item-ID ab. Der bestehende Nahkampftest prüft horizontale Bewegung, da sein Testaufbau keinen Boden hat.

## Nächste Schritte

Keine.

## Offene Punkte

Keine.
