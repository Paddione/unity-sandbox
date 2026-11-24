---
goal: cinderreach-akt1-vertiefung
slug: cinderreach-akt1-vertiefung
health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06]
plan_refs: [.agents/plans/cinderreach-akt1-vertiefung/tasks.md]
---

# Goal: cinderreach-akt1-vertiefung — Akt I „Wer das Licht bezahlt“ doppelt (Umfang + Tiefe + Rätsel)

Angelegt: 2026-11-24. Herkunft: Eigner-Anfrage „spielerische Tiefe der Quests erhöhen, auf Geschichte abstimmen, Akt I+II doppelt so lang, Akt III als Finale, Rätsel erlaubt, modernere Sprache“ (setgoal-Runde 2026-11-24, alle Punkte mit Empfehlung entschieden; Persistenz + Ausführungsplan auf Eigner-Wunsch).

## Objective

In `unity-sandbox` (Cinderreach, `sandbox/`, Unity 6 URP) Akt I von 8 Einträgen (H1–H5, N1–N3) auf ca. 16 Einträge erweitern — beides kombiniert (Eigner-Entscheid): neue Haupt-/Nebenaufgaben UND tiefere Einzelquests mit optionalen Zielen und gemischten Rätseln. Alle Quests bleiben eng an der Story-Bibel (Wendungen 1+2: Fuhrlisten fordern Lebende, Rußbuch mit Ilsabes Hand und Vogts Siegel).

## Erfolgskriterien

1. Ca. 16 Quest-Einträge im Fuhrbuch (Taste J); jede neue Aufgabe hat Auftraggeber, Ziele (Ereignis im Spiel) und Abschluss wie die Bestands-Tabelle in der Story-Bibel.
2. Anker bleiben erhalten: H2-Tor (Abt-Kill öffnet den Ofen), H4-Abschluss (Ofenmutter-Kill schließt Akt I, öffnet die Brache), H5 rein erzählerisch (Ottilie löscht die Laterne über Ilsabes Tür).
3. Spielerische Tiefe: jede erweiterte Quest hat mind. 1 optionales Ziel oder 1 Rätsel; mind. 2 Umgebungs-/Fundstück-Rätsel (Grabtafel-, Fuhrbrief-, Rußbuch-Kombination über `StoryPickup`/`IUsable`, T-Taste) und mind. 1 Dialog-/Entscheidungsrätsel (Ilsabe-/Vogt-Siegel-Strang mit Fuhrbuch-Folge).
4. Jede neue Nebenaufgabe ändert eine Zeile der Hauptkette per `StoryDialog.Visible` (`x|nK` / `x|!nK`), wie N1–N3 es tun.
5. Neue Texte bereits verständlich-modern (Glossar verbindlich, Ton düster-knapp bleibt); Vollglättung des Bestands in `cinderreach-sprache-voice`.
6. Queststand im Save (additiv + Migration), Koop per Host-Snapshot (Max-Merge) wie Bestand.
7. Belege: EditMode + PlayMode grün, `SandboxBuilder.VerifyDiabloZone` + `KayKitAssets.Verify` grün, `python3 scripts/check-meta.py` grün, Autoplay `-seed 0` Exit 0 durch alle Phasen bis `actboss` mit 0 Exceptions.

## Scope

- `docs/story-bible.md` (Akt-I-Tabellen H/N erweitern, Wendungen 1+2 schärfen)
- `sandbox/Assets/Scripts/Story/StoryModel.cs`, `StoryState.cs`, `StoryDirector.cs`, `StoryDialogs.cs`, `StoryBarks.cs`, `StoryPickup.cs`, `StoryNpc.cs`, `StoryQuestUI.cs`
- `sandbox/Assets/Scripts/Localization/LocCatalog.Story.cs` (+ `LocCatalog.Dialog.cs` für neue Zeilen)
- Neue Tests unter `sandbox/Assets/Tests/{EditMode,PlayMode}` (Questkette, Rätsel-Logik, Folgezeilen-Sichtbarkeit)

## Nicht-Ziele

- Kein neuer Schauplatz, keine neuen Gegner-Typen/Bosse, keine Zonen-Layout-Änderung
- Kein Akt-II-Eingriff (Goal `cinderreach-akt2-vertiefung`), kein Akt III (Goal `cinderreach-akt3-finale`)
- Keine Bestands-Dialogglättung über die neuen Zeilen hinaus (Goal `cinderreach-sprache-voice`)
- Kein neues Interaktionssystem (T-Taste/`IUsable` wiederverwenden), keine Kindled-Neubalance

## Abhängigkeiten / Referenzen

- `cinderreach-deutsch-story` (100 %): H1–H9/N1–N5, Fuhrbuch, Folgezeilen `x|nK`
- `cinderreach-story-voice` (100 %): Barks, Voice-Pipeline (`scripts/voice/`, `scripts/check-voice-lines.py`)
- Ausführung: `.agents/plans/cinderreach-akt1-vertiefung/tasks.md` (Stufen S1–S4, je eigener Worktree + `land: ff`)
- health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06]
