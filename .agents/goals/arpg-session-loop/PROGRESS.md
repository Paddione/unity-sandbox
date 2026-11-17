# Progress: arpg-session-loop

Prozent: 10 %

## Erledigt

- 2026-11-17: Eigner-Runde beantwortet (Spielschleife, eigene Identität, headless + Shotcheck,
  `G-DIABLO01,02,03,05`).
- 2026-11-17: Goal angelegt, Plan `.agents/plans/arpg-session-loop/tasks.md` (Stufen S1–S6) geschrieben.
- 2026-11-17: Eigner bestätigt Arbeitstitel „Cinderreach“ und die Namensliste. Plan um Vorbedingung
  (Fremd-Änderungen HD-UI + MenuArt), Session-Protokoll, Statustabelle und Definition of Done ergänzt.
- 2026-11-17: Vorbedingung erfüllt: HD-UI + MenuArt-Pack aus dem Hauptcheckout gelandet (Startsperre gelöst).

## Erledigt

- 2026-11-17: S1 Identität gelandet (unity-sandbox-Anteil; dotfiles-Probe läuft separat über den Orchestrator).
  Belege: EditMode 58/58, PlayMode 59/59, `VerifyDiabloZone` grün („ashen fen + loot + netcode present“),
  `KayKitAssets.Verify` grün, `check-meta.py` ohne Befund.
  `grep -rniE "blood ?moor|rogue encampment|moor gate" sandbox scripts docs` trifft nur noch bewusste
  Ausnahmen: Historie (`dev-journal.md`, `decision-log.md`, `mechanics-questions.md` bleiben unverändert),
  Asset-Pfade (`Assets/Art/BloodMoor`, `docs/world-art/bake_moor_ground.py` — GUID-gebunden) und die
  Regex-Definition in `IdentityTests.cs` selbst. Laufzeit-Strings/Builder/Tests sind leer.
  Fix nebenbei: `DiabloAudio.EnsureBuilt` baut Clips neu, wenn Szenen-Ladungen sie zerstört haben
  (latenter Bug, machte `AudioClips_AreValidMono22050` reihenfolgeabhängig rot).

## Nächste Schritte

1. S2 Gold + Save nach Session-Protokoll umsetzen. dotfiles-Probe (`diablo_unity.py` auf `cinderreach`) separat landen.

## Offene Punkte

- Keine.
