# Cinderreach: Gruppendialoge, Quest-Tracking, Minimap und Zonenkarte

health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO06]

Ausgangslage: [`.agents/goals/CINDERREACH.md`](../CINDERREACH.md) (Stand `main` @ `b8ae9a6`).

## Objective

Im Koop erleben alle Spieler die Story gemeinsam: Spricht ein Held mit einem Story-NPC, sehen und hören alle
anderen denselben Dialog Zeile für Zeile, und Boss- und Erzählzurufe des Hosts erreichen jeden Client.
Das Quest-Tracking zeigt alle verfolgten Aufgaben mit Zielen, Zählern und Weg zum nächsten Ort.
Eine Minimap und eine Zonenkarte (M) zeigen Questziele, Abgaben, Angebote, Tore, Wegschreine, Gefährten und Gegner.

## Erfolgskriterien

1. Geteilte Dialoge: Öffnen, Weiterblättern und Schließen eines NPC-Dialogs gehen über den Host an alle
   anderen Spieler (Dialog-ID, sichtbare Zeilen, aktuelle Zeile). Der Host verwirft ungültige Nachrichten.
   Zuhörer sehen „<Held> spricht mit <NPC>“, hören die Stimme, werden nicht blockiert, buchen nichts und können
   ausblenden. Ein eigener Dialog hat Vorrang. Beleg: EditMode-Protokoll- und Fenstertests, PlayMode NGO Host+Client.
2. Zurufe: Jeder vom Host angenommene Bark wird an alle Clients weitergegeben, ohne Doppelungen (20-s-Sperre).
3. Tracking: Alle aktiven und abgabebereiten Aufgaben sind verfolgt, bis der Spieler sie im Fuhrbuch abschaltet
   (Story-Flag `untrack:<id>`, ohne Save-Migration, nicht im Koop-Snapshot). HUD-Tracker mit bis zu 4 Aufgaben,
   allen Zielen samt Zähler und Haken, Abgabe-NPC und Entfernung in Metern bzw. Zielzone. Fortschritts-Toast je Zählerschritt.
   Fuhrbuch mit Angeboten (Auftraggeber), Verfolgen-Schalter und Zielzone.
4. Zielorte: Gespräch → NPC, Fund → verbleibende Fundstücke, Kill → bis zu 3 nächste lebende Ziele, Zone → Zone,
   Abgabe → Auftraggeber, Angebot → Auftraggeber. Orte in anderen Zonen laufen über das Tor zur nächsten Zone.
5. Karten: Minimap oben rechts (28-m-Fenster, kameragleich gedreht, Rand-Pfeile für Ziele außer Sicht) und Zonenkarte
   auf M mit Legende, verfolgten Aufgaben und ◀/▶ über freigeschaltete Zonen. Zonenbild einmalig per Top-down-Render,
   in Batchmode ohne Grafik ein neutraler Grund.
6. Abnahme: EditMode und PlayMode komplett grün, `scripts/check-meta.py` OK. Autoplay normal DONE ohne Exceptions,
   Koop-Paar Exit 0. Windows-Shots von Minimap und Zonenkarte.

## Nicht-Ziele

Kein Nebel des Krieges, keine Kartenmarkierungen durch Spieler, keine Wegfindung/Pfadlinien, keine Abstimmung
„Dialog gemeinsam überspringen“. Keine neuen Quests oder Dialogzeilen. Keine Save-Migration.
