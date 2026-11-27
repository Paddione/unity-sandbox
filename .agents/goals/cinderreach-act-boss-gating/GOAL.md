---
goal: cinderreach-act-boss-gating
slug: cinderreach-act-boss-gating
health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06]
---

# Goal: cinderreach-act-boss-gating — Akt-Bosse erscheinen erst nach allen übrigen Akt-Quests

Angelegt: 2026-11-27. Herkunft: Eigner-Anfrage (setgoal-Runde 2026-11-27, alle Empfehlungen angenommen; Auftrag „anlegen und umsetzen“).

## Objective

The Kilnmother (Akt I, H4), The Glasswright (Akt II, H8) und Thewes (Akt III, H12) erscheinen erst, wenn alle übrigen
Haupt- und Nebenquests ihres Akts `Done` sind. Beim Spawn läuft sofort eine Cutscene: Fernschnitt zur Arena, die Kamera
zeigt den Boss und jede Phase-1-Fähigkeit ohne Erklärtext, danach zurück zum Helden. Autoplay und Koop-Paarlauf spielen
alle Akt-Quests vor dem jeweiligen Boss-Kill.

## Erfolgskriterien

1. Pflichtmenge je Akt = alle H*/N*-Quests des Akts außer Boss-Quest und Quests, die erst nach dem Boss-Kill möglich sind
   (Prereq-Kette über die Boss-Quest, Gates auf den Boss-Zustand). Aus dem Katalog abgeleitet, keine Handliste.
   EditMode-Test belegt die Menge je Akt.
2. Vor Erfüllung der Pflichtmenge existiert kein aktiver Akt-Boss. Beim Abschluss der letzten Pflichtquest erscheint er
   genau einmal pro Lauf. Nach Laden eines Saves mit erfüllter Pflichtmenge steht er ohne erneute Cutscene, sofern sie
   gespielt wurde.
3. Spawn-Cutscene (`CutsceneDirector`): Fernschnitt zur Arena unabhängig vom Ort des Helden, Letterbox + Namensbanner,
   Boss während aller Showcase-Casts im View-Frustum (PlayMode-Test via `Camera.WorldToViewportPoint`), jede
   Phase-1-Fähigkeit einmal, danach Kamera zurück zum Helden, Skip funktioniert.
4. Keine Fähigkeits-Captions mehr (Spawn-Cutscene und Phase-2-Showcase). Test belegt: kein Caption-Text gerendert.
5. Thewes' H11-Auftritt (`StoryDirector.MaybeSpawnThewesAtH11`) ist durch das neue Gate ersetzt, `Akt3FinaleTests`
   umgestellt.
6. Kein Deadlock: keine Pflichtquest hängt an einem Gate/Catch-up, das erst der Boss-Kill auslöst. Alte Saves (v16)
   bleiben spielbar, Zustand wird aus dem Queststand abgeleitet (keine neue Save-Version).
7. Koop: Queststand des Hosts entscheidet den Spawn für die Gruppe, Cutscene läuft bei allen, Kill-Buchung unverändert.
8. Autoplay: Solo und Koop-Host im Quest-Modus, `-autoplay-no-quests` entfällt, Ergebnis-JSON belegt je Akt alle
   Pflichtquests `Done` vor dem Boss-Kill (nie per Catch-up), Thewes-Kill im Pflichtphasen-Vertrag.
   `bash scripts/run-autoplay.sh <out> -seed 0` Exit 0 mit allen Phasen, `scripts/run-coop-pair.sh` Exit 0.
9. Grün: EditMode + PlayMode (`bash scripts/run-unity-tests.sh`), `VerifyDiabloZone`, `check-meta`, Quest-Coverage
   0 uncovered, `python3 scripts/agent-workspace.py check` still; Windows-Shotcheck zeigt die Spawn-Cutscene je Akt-Boss
   mit Boss im Bild.

## Nicht-Ziele

- Hollow Abbot bleibt (Arena-Intro, Kill öffnet den Kiln); er verliert nur die Fähigkeits-Captions.
- Kein Boss-Balancing, keine neuen Fähigkeiten, keine neuen Quests, keine Drop-/Loot-Änderung.
- Kein Umbau der Story-Kette über das Entfernen deadlockender Gates/Catch-ups hinaus.

## Abhängigkeiten und Randbedingungen

- Start nach Landung von `cinderreach-akt3-finale` S4 (`892299c`, erfüllt). Parallel läuft `cinderreach-sprache-voice` S2
  (Dialog-/Loc-Dateien): Loc-Änderungen klein halten, vor `land` rebasen.
- Perf-Nachweis nur per Windows-Shotcheck; Linux-FPS-Gate ist Hinweis.
- Szenen nur über Builder neu bauen, Churn (`TimeManager.asset` u. a.) revertieren.
- Präsentationscasts bleiben harmlos (Telegraph/VFX, nie Schaden), Bosse halten während der Cutscene (`HoldBosses`).
