---
goal: arpg-session-loop
slug: arpg-session-loop
health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO05]
plan_refs: [.agents/plans/arpg-session-loop/tasks.md]
---

# Goal: arpg-session-loop — Vom Zonen-Slice zur 20-Minuten-Session mit eigener Identität

Angelegt: 2026-11-17. Herkunft: Eigner-Anfrage „substantially improve our unity game“, geklärt per `setgoal`
(Q1 Spielschleife, Q2 eigene Identität, Q3 Nachweis headless + Shotcheck, Q4 `G-DIABLO01,02,03,05`).

## Objective

Das Co-op-ARPG in `unity-sandbox` (`DiabloZone.unity`, Click-to-Move-Nahkampf, Husks, Loot-Seltenheiten,
Inventar, XP, Q/E-Skills) wird von einem Zonen-Slice zu einer zusammenhängenden 15–20-Minuten-Solo-Session
mit Ziel, Fortschritt und Abschluss ausgebaut. Gleichzeitig bekommt es eine eigenständige Identität
(Arbeitstitel, Setting, Zonen- und Gegnernamen). Das Genre bleibt ARPG, Anlehnungen an ein bestimmtes
bestehendes Spiel entfallen.

## Erfolgskriterien

1. Identität: eigener Arbeitstitel (`PlayerSettings.productName`, Exe- und Build-Ordnername) und eigene
   Namen für alle Zonen, Orte und Gegner. Beleg: `grep -rniE "blood ?moor|rogue encampment|moor gate"`
   über `sandbox/Assets/{Scripts,Editor,Scenes,Tests}` und `scripts/` ohne Treffer. Setting-Absatz in
   `docs/one-page-gdd.md`.
2. Town-Loop: Hub mit mindestens einem Vendor (Verkaufen/Kaufen gegen Gold, das Gegner droppen) und
   Heilung. Beleg: PlayMode-Test Kauf/Verkauf + Gold-Bilanz grün.
3. Zone 2 + Boss: neue Zone über einen Übergang aus Zone 1 erreichbar, eigener Gegnertyp mit eigenem
   Angriffsmuster, Boss mit mindestens 2 Phasen und garantiertem Rare+-Drop. Der Boss-Kill beendet die
   Session mit Abschluss-Screen. Beleg: Verify-Methode für Zone 2, PlayMode-Test Boss-Kill → Abschluss.
4. Charakterfortschritt mit Wahl: Level-Up vergibt Punkte, die der Spieler auf Attribute oder Skill-Ränge
   verteilt. Mindestens 3 aktive Skills mit Cooldown auf Hotkeys (bestehend: Q Fireball, E Arrow).
   Beleg: EditMode-Tests Punktevergabe/Rang-Effekt, PlayMode-Test Skill-Einsatz.
5. Persistenz: Speichern/Laden von Level, XP, Punkten, Ausrüstung, Inventar, Gold und erreichter Zone
   (lokale JSON-Datei, Solo). Beleg: EditMode-Roundtrip-Test + PlayMode-Test „Reload setzt Zustand fort“.
6. Scripted Session-Durchlauf: automatisierter Durchlauf Town → Zone 1 → Zone 2 → Boss → Abschluss mit
   0 Exceptions im Player-Build, Laufprotokoll `docs/session-loop-run-log.md`.
7. Windows-Build per `scripts/stage-windows.sh` gestaged, Shotcheck-PNGs (`scripts/shotcheck-windows.sh`)
   für Town, Vendor-UI, Zone 2 und Boss gesichtet und im Run-Log verlinkt.
8. Regression: alle EditMode- und PlayMode-Suiten grün, `VerifyDiabloZone` (bzw. Nachfolger) grün,
   Host-Start-Lauf 0 Exceptions, `G-DIABLO01/02/03/05` in `/health` grün, `docs/decision-log.md` und
   `docs/dev-journal.md` fortgeschrieben.

## Nicht-Ziele

- Keine Co-op-Erweiterung (kein Inventar-/HP-/Save-/Gold-Sync, keine Client-Leichen, kein
  Cross-Machine-Relay). Bestehendes Co-op darf nicht brechen.
- Keine Präsentationsoffensive (Lighting-Overhaul, externe Audio-Assets, Pipeline-Wechsel). URP bleibt.
- Kein Menschen-Playtest als Kriterium (Eigner-Playtest ist Follow-up).
- Keine Nachbildung von Namen, Figuren, Karten oder Look eines bestehenden Spiels.
- Keine Umbenennung interner Code-Bezeichner (`Diablo*`-Klassen, Szenendateinamen). Annahme: nur
  spielersichtbare Strings und Build-Artefakte zählen zur Identität.
- Kein Steam-Build, keine Store- oder AI-Disclosure-Arbeit.

## Abhängigkeiten

- Vorgänger: `unity-diablo-feel-polish`, `unity-diablo-d2-combat-feel` (dieses Repo), dotfiles-Goals
  `unity-diablo-prototype` (100 %) und `diablo-unity-health-goals`.
- Health-Probe-Vertrag: dotfiles `scripts/health_probes/diablo_unity.py` (`G-DIABLO02` Szenen-/Asset-Vertrag,
  `G-DIABLO05` Windows-Stage-Ordner) wird bei Rename und neuen Prefabs im selben Durchlauf nachgezogen.
- health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO05]
