# Goal: sloplike-rebrand

health_probe: {"id":"G-SLOPLIKE01","title":"Spieltitel ist Sloplike","cmd":"grep -c '^  productName: Sloplike$' ~/unity-sandbox/sandbox/ProjectSettings/ProjectSettings.asset | grep -qx 1 && echo 0 || echo 1","target":0}

## Objective
Das Spiel heißt für Spieler überall „Sloplike“: Fenster- und Produktname, Startbildschirm, Menüs, Ladebildschirme, Spieltexte (DE/EN) und Build-Ausgabe. Der Ton wechselt von düsterer Moor-Fantasy zu einer Feier von Memekultur und der sinnlosen Jagd nach Superintelligenz.

## Ist-Befunde
- B1 `sandbox/ProjectSettings/ProjectSettings.asset` Zeile 16: `productName: Cinderreach`.
- B2 21 Dateien unter `sandbox/Assets/Scripts` enthalten „Cinderreach“ (`grep -rlI Cinderreach sandbox/Assets/Scripts | wc -l`).
- B3 Windows-Deploy installiert nach `Games\cinderreach` (`stage-windows.sh`).

## Entscheidungen
- D1 (Eigner 2026-11-28) Neuer Titel „Sloplike“.
- D2 (Eigenentscheidung) Nur spielersichtbare Namen ändern; interne Bezeichner (Klassen, Namespaces, Repo, Goal-Slugs, `Diablo*`-Klassen, Szenendateien) bleiben, weil Umbenennen dort nur Bruchrisiko bringt. Windows-Installationsordner wird `Games\sloplike`, der alte Ordner bleibt bis zur Eigner-Freigabe unangetastet.

## Erfolgskriterien
1. Kein spielersichtbarer Text nennt mehr „Cinderreach“ — Nachweis: EditMode-Test über `LocCatalog` (DE+EN) und UI-Texte findet 0 Treffer; `productName: Sloplike`.
2. Startbildschirm und Hauptmenü zeigen den neuen Titel — Nachweis: Windows-Shot unter `docs/sloplike/`.
3. Spiel läuft unverändert — Nachweis: EditMode/PlayMode grün, Autoplay `-seed 0` Exit 0 bis `complete`.

## Nicht-Ziele
- Repo-, Namespace- oder Dateiumbenennungen.
- Neue Story-Inhalte (Goal `sloplike-story-rewrite`).

## Abhängigkeiten
- Keine.
