# Goal: sloplike-act-four

health_goals: none (Inhaltsausbau, belegt durch Autoplay und PlayMode)

## Objective
Akt IV ist spielbar: eine neue Zone von Jensen Huang, eigene Quests, Jensen als Finalboss (Menschform, Verwandlung in das Lederjacken-Monster). Nach seinem Sieg folgt die Schlusspointe: SI war „Still Indians“.

## Ist-Befunde
- B1 Heute drei Akte, `ActProgress`/`ActBossGate` kennen Akt I–III; Thewes ist Finalboss (H12), danach Entscheidung H13.
- B2 Jensen-Modelle entstehen im Goal `cinderreach-hero-boss-redesign` (Kriterium 3/6).

## Entscheidungen
- D1 (Eigner 2026-11-28) Akt IV als eigenes Goal jetzt; Jensen ist Finalboss, Pointe nach seinem Sieg.
- D2 Satire-Leitplanke (Eigenentscheidung 2026-11-28): Die Pointen „Actually Indians“/„Still Indians“ zielen auf Firmen, die menschliche Klickarbeit als KI verkaufen; die Arbeiter erscheinen als normale, sympathische Leute, keine Akzent-Witze, keine ethnischen Stereotype. Reale Personen nur als erkennbare Parodie, keine erfundenen Tatsachenbehauptungen; erkennbare Abbilder realer Personen sind bei öffentlicher oder kommerzieller Verbreitung rechtlich heikel (Persönlichkeitsrecht).
- D3 (Eigenentscheidung) Zone, Gegner und Quests nutzen die vorhandenen Systeme (Layout-Builder, Encounter, `ActBossGate`); Save-Migration nur falls neue Felder nötig.

## Erfolgskriterien
1. Akt IV ist nach Akt III erreichbar und hat eigene Zone und Quests — Nachweis: PlayMode-Test, Autoplay bis Jensen.
2. Jensen-Kampf mit Verwandlung im Spawn-Intro und Schlusspointe „Still Indians“ — Nachweis: PlayMode-Test, Windows-Shots, Voice-QA.
3. Spiel endet mit neuem Abschluss — Nachweis: Autoplay `-seed 0` Exit 0 bis `complete`.

## Nicht-Ziele
- Neues Bosssystem oder neue Arena-Mechaniken.

## Abhängigkeiten
- `cinderreach-hero-boss-redesign`, `sloplike-story-rewrite`.
