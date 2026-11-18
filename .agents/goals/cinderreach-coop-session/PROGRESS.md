# Progress: cinderreach-coop-session

Prozent: 15 %

## Erledigt

- 2026-11-17: Goal angelegt (Folge-Goal nach `arpg-session-loop`).
- 2026-11-17: Plan `.agents/plans/cinderreach-coop-session/tasks.md` geschrieben (Stufen, Statustabelle, Verify).
- 2026-11-18: S1 join landed. `-host [-port N]` / `-join ip[:port]` (`Net/JoinArgs.cs`, P2-Flags bleiben gültig),
  Co-op-Fenster `JoinPanel` (F7, Button in der Klassenwahl), `-save-file` für getrennte Saves, `CoopAutoplay`
  (Client folgt dem Host, greift per ServerRpc an, Ergebnis mit `role`), `scripts/run-coop-pair.sh`.
  Belege: EditMode 210/210, PlayMode 84/84, `VerifyDiabloZone`, `check-meta`; Paarlauf Exit 0: Client
  `town,zone1` 0 Exceptions (104 s), Host 8/8 Phasen 0 Exceptions (179 s); Solo-Autoplay `-seed 0` 8/8, 36,8 fps.

## Nächste Schritte

1. S2 enemies: host-autoritative Gegner, Boss-Replikation, `HitValidator`.

## Offene Punkte

- Kriterien 1–7 offen bis S2–S6.
- Befund außerhalb des Goals: Solo-Autoplay ohne `-seed` (Seed aus vorhandenem Save, 1112106611, Fen-Variante
  `husks=1 elite=0 brute=2 props=2`) blieb nach 11 Kills bis Budgetende ohne Treffer hängen. Der Nightly testet nur
  Seeds 0–2 und diese Husk-Variante nie.
