---
goal: cinderreach-act-two-opening
slug: cinderreach-act-two-opening
health_goals: [G-DIABLO02, G-DIABLO06]
plan_refs: [.agents/plans/cinderreach-act-two-opening/tasks.md]
---

# Goal: cinderreach-act-two-opening — Akt II, erste Zone als vertikaler Slice

Angelegt: 2026-11-22. Herkunft: Eigner-Anfrage „develop more content for our unity game“,
setgoal-Runde 2026-11-22 (Akt II erste Zone, vertikaler Slice 4–5 Stufen, health G-DIABLO02 + G-DIABLO06).
Ausgangslage: [CINDERREACH.md](../CINDERREACH.md) (`main` @ `bf6d5cd`).

## Objective

Akt II von Cinderreach mit einer ersten spielbaren Zone eröffnen: Der Kilnmother-Kill öffnet die
**Glass Barrens** (Zone 4, +600 m X) mit eigener Gegnerfamilie, Zonen-Boss und Wegpunkt. Solo,
Koop und Autoplay spielen sie vollständig durch.

## Erfolgskriterien

1. Zone 4 als Root in `DiabloZone.unity` bei +600 m X, nur über `BarrensBuilder` erzeugt;
   `SandboxBuilder.VerifyDiabloZone` ruft `BarrensBuilder.VerifyBarrens` und ist grün.
2. Akt-Kette: Kilnmother-Kill (Akt I abgeschlossen) öffnet das Kiln→Barrens-Tor (`ActProgress.Barrens`),
   `ActProgress.ZoneBossDefeated` hält den Zonen-Boss-Kill; Wegpunkt-Schrein „Glass Barrens“ mit attuned/sealed.
3. Gegnerfamilie: zwei neue Typen mit unterschiedlicher Kampfrolle (Schildträger mit Frontalblock,
   Flächenbeschwörer) plus Elite-Variante; Threat-Zielwahl, Telegraph, Netz-Replikation von HP/Phasen/Summons.
4. Zonen-Boss mit zwei Phasen und lesbaren Telegraphen, Bossbalken mit Phase, eigene Musik; auf Kindled
   eine eigene Phase-2-Mechanik.
5. Beute: Itemlevel-Bracket oberhalb des Kiln, ≥ 2 neue Uniques aus der Zone; Save v10 → v11
   (`actTwoBossDefeated`, Barrens-Freischaltung für Kilnmother-Bezwinger), `SaveMigrationTests` auf `CurrentVersion`.
6. `LayoutSeeder`: je 3 Varianten für Gruppen und Elites der Zone, Koop-Peers auf Variante 0.
7. Autoplay: neue Pflichtphasen nach `actboss` (`zone4`, `boss3`), Exit 0 für alle drei Klassen, Normal
   und Kindled, `-seed 0..2`; FPS-Gate grün (Linux 30 fps, Windows-Shotcheck 60 fps @1080p).
8. Koop: `scripts/run-coop-pair.sh` durch die neue Zone, Gruppenreise über das Tor, Boss-Kill auf Host und
   Client konsistent.
9. EditMode + PlayMode komplett grün inkl. neuer Tests (Akt-Kette, Gegner-KI, Boss-Phasen, Migration);
   `python3 scripts/check-meta.py` und `KayKitAssets.Verify` grün.
10. Doku: `docs/act-two-run-log.md`, `docs/dev-journal.md`, `docs/decision-log.md`; bei 100 % in
    CINDERREACH.md eingearbeitet, Goal und Plan entfernt.

## Nicht-Ziele

- Weitere Akt-II-Zonen, Akt-II-Endboss, neue Klasse, Endgame-Systeme, Steam-Build.
- Umbenennung interner `Diablo*`-Bezeichner; Fremd-IP; Asset-Store-Käufe (Art aus KayKit + Blender-Pipeline).
- Neue Aufträge auf dem Auftragsbrett für Zone 4.

## Abhängigkeiten / Referenzen

- `ActProgress`, `KilnBuilder`/`KilnLayout`, `ZoneGate`, `WaypointShrine`, `SaveGame`/`SaveMigrations` (v10),
  `SessionAutoplay`, `LayoutSeeder`, `EncounterLayout`, `MusicDirector`, `ZoneAtmosphere`, `ItemLevelRules`.
- dotfiles `scripts/health_probes/diablo_unity.py` (Szenen-/Asset-Vertrag, Autoplay-Phasen).
- Ausführung: `.agents/plans/cinderreach-act-two-opening/tasks.md` (S1–S5, je eigener Worktree + `land: ff`).
- health_goals: [G-DIABLO02, G-DIABLO06]
