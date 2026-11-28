# Goal: cinderreach-zone-expansion

health_goals: [G-DIABLO01, G-DIABLO02]
plan_refs: [.agents/plans/cinderreach-zone-expansion/tasks.md]

## Objective
Alle spielbaren Gebiete erhalten viermal so viel Grundfläche. In den bisher leeren und den neu erschlossenen Kampfabschnitten treffen Spieler auf Gegner ihrer Zone.

## Ist-Befunde
- B1 Laut .agents/goals/CINDERREACH.md liegt Lantern Hold im Ashen Fen. Sunken Ossuary, Cinder Kiln und Glass Barrens liegen in derselben Szene.
- B2 SandboxBuilder.BuildDiabloZone erzeugt die Spielszene einschließlich der Koop-Startpositionen.

## Entscheidungen
- D1 Grundfläche vervierfachen, beide horizontalen Abmessungen verdoppeln.
- D2 Gegnergruppen auf die Kampfabschnitte verteilen. Händlerplätze und Ankunftspunkte sicher halten.

## Erfolgskriterien
1. Jedes Gebiet hat doppelte horizontale Ausdehnung in beiden Achsen. Nachweis durch Geometrieprüfung der Spielszene.
2. Die erweiterten Kampfgebiete enthalten zonentypische Gegner, sichere Ankunftsbereiche sind frei. Nachweis durch Prüfung der Gegnerpositionen.
3. Die generierte Spielszene ist ladbar, Übergänge und Zonenkarte berücksichtigen die neuen Grenzen. Nachweis durch Unity-Szenenprüfung und Tests.

## Nicht-Ziele
- Neue Gegnertypen und zusätzliche Zonen.

## Abhängigkeiten
- Unity 6000.0.85f2.
