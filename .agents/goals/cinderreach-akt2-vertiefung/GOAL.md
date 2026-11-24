---
goal: cinderreach-akt2-vertiefung
slug: cinderreach-akt2-vertiefung
health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06]
plan_refs: [.agents/plans/cinderreach-akt2-vertiefung/tasks.md]
---

# Goal: cinderreach-akt2-vertiefung — Akt II „Was im Glas wohnt“ doppelt (Umfang + Tiefe + Rätsel)

Angelegt: 2026-11-24. Herkunft: Eigner-Anfrage wie in `cinderreach-akt1-vertiefung` (setgoal-Runde 2026-11-24; Persistenz + Ausführungsplan auf Eigner-Wunsch).

## Objective

In `unity-sandbox` Akt II von 6 Einträgen (H6–H9, N4–N5) auf ca. 12 Einträge erweitern — beides kombiniert (Eigner-Entscheid): neue Aufgaben UND tiefere Einzelquests mit gemischten Rätseln. Roter Faden bleibt Wendung 3 (Glut im Glas hält die Toten fest und ruft die Hüllen herbei); Tamm-Strang (N4) und Ilsabe-Sehglas-Strang (N5) werden je um mind. 1 Folgequest verlängert.

## Erfolgskriterien

1. Ca. 12 Quest-Einträge im Fuhrbuch; Anker bleiben: H6-Ankunft (Merle), H8-Glasmacher-Kill mit Prisma-Drop, H9-Rückkehr (Ottilie + Merle, offene Scherben-Zeile als Haken für Akt III).
2. Mind. 2 Umgebungs-/Fundstück-Rätsel (z. B. Sehglas-Kombination, Glassäulen-/Scherben-Ordnung über `StoryPickup`/`IUsable`) und mind. 1 Dialog-/Entscheidungsrätsel (Sehgläser behalten vs. abliefern, mit H9-Folgezeile).
3. Neue `x|nK`-Varianten für H9 (Merle verkauft Glas weiter vs. nicht; Ottilie-Satz zu Ilsabe „Sie wird ihnen etwas anbieten. Jemanden.“ bei N5-Folge).
4. Neue Texte bereits verständlich-modern (Glossar verbindlich); Bestands-Glättung in `cinderreach-sprache-voice`.
5. Queststand im Save (additiv + Migration), Koop per Host-Snapshot (Max-Merge).
6. Belege: EditMode + PlayMode grün, `VerifyDiabloZone` + `KayKitAssets.Verify` grün, `check-meta.py` grün, Autoplay `-seed 0` Exit 0 durch alle Phasen bis `boss3` mit 0 Exceptions.

## Scope

- `docs/story-bible.md` (Akt-II-Tabellen H6–H9/N4–N5 erweitern, Wendung 3 schärfen)
- `sandbox/Assets/Scripts/Story/*` (Model/State/Director/Dialogs/Barks/Pickup wie Goal Akt I)
- `sandbox/Assets/Scripts/Localization/LocCatalog.Story.cs` (+ Dialog für neue Zeilen)
- Neue Tests unter `sandbox/Assets/Tests/{EditMode,PlayMode}`

## Nicht-Ziele

- Kein Akt-I-Eingriff (Goal `cinderreach-akt1-vertiefung`), kein Akt III (Goal `cinderreach-akt3-finale`)
- Keine neuen Zonen-Layouts jenseits der 3 Barrens-Varianten, keine neuen Gegner-Typen
- Keine Bestands-Dialogglättung (Goal `cinderreach-sprache-voice`), keine Item-/Bracket-Neubalance (Uniques Aegis/Prisma unangetastet)

## Abhängigkeiten / Referenzen

- `cinderreach-deutsch-story` (100 %), `cinderreach-story-voice` (100 %)
- `cinderreach-akt2-opening` (100 %, `c89dc40`): Barrens, Shardback/Augur/Elite, Glasswright, Bracket 4, Save v11
- Ausführung: `.agents/plans/cinderreach-akt2-vertiefung/tasks.md` (Stufen S1–S3, je eigener Worktree + `land: ff`)
- health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06]
