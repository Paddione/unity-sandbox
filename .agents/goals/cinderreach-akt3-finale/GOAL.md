---
goal: cinderreach-akt3-finale
slug: cinderreach-akt3-finale
health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06]
plan_refs: [.agents/plans/cinderreach-akt3-finale/tasks.md]
---

# Goal: cinderreach-akt3-finale — Akt III „Laternen-Finale“ (linearer Höhepunkt)

Angelegt: 2026-11-24. Herkunft: Eigner-Anfrage (setgoal-Runde 2026-11-24, Entscheid: Laternen-Finale linear; Persistenz + Ausführungsplan auf Eigner-Wunsch).

## Objective

In `unity-sandbox` den dritten finalen Akt als lineare Kette mit End-Entscheidung bauen (H10–H13 + N6–N7): Ottilie hält die Scherbe gegen ihre Laterne (Ausblick-Haken aus der Story-Bibel), die Fuhre entscheidet „zerschlagen oder halten“ — das Licht flackert im Takt eines Atems, die Hüllen am Moortor wollen nur nach Hause. Schauplätze: Laternenwacht + Moortor; kein neuer Ort, keine verzweigten Enden mit eigenem Boss.

## Erfolgskriterien

1. Neue Kette H10–H13 (z. B. H10 Letzte Nacht in der Wacht, H11 Glut-Rätsel am Moortor, H12 Finalboss aus Hüllen-Antagonist auf vorhandenem System, H13 Entscheidung mit je eigener Schlusszeile bei gleichem Durchlauf) + N6–N7 (z. B. Jorins Einschmelzen, Ilsabes letztes Angebot).
2. Genau eine offene, unbequeme Schlusszeile wird aufgelöst (kein verzweigter Content-Baum, kein zweites Ende mit eigenem Boss).
3. `ActProgress`-Erweiterung für Akt III (Tor-/Freischalt-Logik wie Abt→Kiln, Kilnmother→Brache), Save-Version +1 mit Migration; Koop-Buchung wie Bestand (Boss-Kill bucht Akt-Stand/Save bei Clients).
4. Neue Texte verständlich-modern (Glossar verbindlich); Vertonung neuer Zeilen über die Voice-Pipeline.
5. Belege: EditMode + PlayMode grün, `VerifyDiabloZone` + `KayKitAssets.Verify` grün, `check-meta.py` grün, Autoplay erreicht die `complete`-Phase mit 0 Exceptions.

## Scope

- `docs/story-bible.md` (neuer Akt-III-Abschnitt, ersetzt den Ausblick-Platzhalter)
- `sandbox/Assets/Scripts/Story/*`, `sandbox/Assets/Scripts/Zones/ActProgress*` (Akt-III-Fortschritt, Moortor-Logik)
- `sandbox/Assets/Scripts/Localization/LocCatalog.Story.cs` (+ Dialog für neue Zeilen)
- Neue Tests unter `sandbox/Assets/Tests/{EditMode,PlayMode}` (Kette, Entscheidung, Save-Migration, Koop-Buchung)

## Nicht-Ziele

- Keine 2-Enden-Verzweigung, kein neuer Schauplatz, kein neues Boss-System
- Kein Akt-I-/Akt-II-Eingriff (Goals `cinderreach-akt1-vertiefung`, `cinderreach-akt2-vertiefung`)
- Keine Bestands-Sprachglättung (Goal `cinderreach-sprache-voice`), keine Kindled-Neubalance

## Abhängigkeiten / Referenzen

- Story-SSOT `docs/story-bible.md` §Ausblick (Scherben-Schlusszeile als Haken)
- `cinderreach-akt1-vertiefung` + `cinderreach-akt2-vertiefung` (Haken H9 → H10 muss nach deren H9-Varianten noch passen; bei Konflikt führt Akt III)
- Ausführung: `.agents/plans/cinderreach-akt3-finale/tasks.md` (Stufen S1–S4, je eigener Worktree + `land: ff`)
- health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06]
