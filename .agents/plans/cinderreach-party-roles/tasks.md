---
goal: cinderreach-party-roles
slug: cinderreach-party-roles
branch: feature/cinderreach-party-roles-s<N>
repo_main: main
tickets: none
health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO06]
start_condition: cinderreach-second-class 100 % und cinderreach-melee-rework auf main (ffbcc0a) — beide erfüllt
plan_refs: [.agents/goals/cinderreach-party-roles/GOAL.md]
---

# Plan: cinderreach-party-roles — Tank/Heiler/Rogue, Threat, Solo-KI-Gruppe

Pfad: feature, 6 Stufen. Jede Stufe einzeln land-bar (eigener Worktree via
`python3 ~/scripts/agent-workspace.py create --repo unity-sandbox feature/cinderreach-party-roles-sN`,
Abschluss `python3 ~/scripts/agent-workspace.py land` im Worktree, `land: ff`).
Reihenfolge ist Abhängigkeitsreihenfolge; keine Stufe lässt Suiten, Verify oder Host-Start rot zurück.

## Goal

Siehe [GOAL.md](../../goals/cinderreach-party-roles/GOAL.md): drei Rollen (Tank/Heiler/Rogue),
Threat-Aggro, Heal-Spells, Basis-Skills für alle mit Klassen-Bonus ×1.5, Solo-KI füllt auf 3 auf,
Koop (max. 3) ersetzt KI rollengleich.

## Success Criteria

Die sechs Erfolgskriterien aus GOAL.md, je Stufe unten mit Test-/Lauf-Beleg verknüpft.
Gesamt-Gate S6: EditMode + PlayMode grün, `KayKitAssets.Verify` grün, Session-Autoplay mit
3er-Party Exit 0 / 0 Exceptions, Koop-Smoke (Spieler ersetzt KI) ohne Exception.

## Ausgangslage (Belege, HEAD `8d0965b`, 2026-11-18)

- Klassen: `sandbox/Assets/Scripts/Classes/ClassCatalog.cs` — nur `Fenwalker` (Knight, Q/E/R)
  und `Lanternbearer` (Mage, Q/E/R/Space); `ClassDefinition.cs` trägt `focusSkillFactor` /
  `strengthMeleeFactor`. Save-`classId` + Fenwalker-Fallback existieren (second-class S1).
- Skills: `DiabloSkills.cs:21` `enum Skill { Fireball=0, Arrow=1, Cleave=2, LanternBolt=3,
  Wisps=4, SearingHalo=5, VeilStep=6 }` — Werte sind Netz-Vertrag (int via `DiabloNetworkPlayer`),
  neue Skills nur anhängen. Schadensskalierung über `CharacterSheet.SkillMultiplier(skill,
  gearRanks, ActiveClass.focusSkillFactor)` (`DiabloSkills.cs:286`, `CharacterSheet.cs:121`).
- Gegner-Zielwahl: `EnemyAttack.AcquireHero` (ab Z. 264) wählt den nächsten lebenden Helden
  (Koop S2); Solo-Fallback `HeroLocator.Solo()`/`Hero()`. Kein Threat-System vorhanden.
- Heilung: `PlayerHealth.Heal(amount)` + `CampfireHeal` vorhanden; kein Ziel-/AoE-Heal-Skill.
  Schaden directional über `PlayerHealth.TakeDamage(amount, fromPos)` + `DiabloBlock` (melee-rework).
- Koop: host-autoritativ, `DiabloNetworkPlayer` + `Net/HitValidator` (Reichweiten-Check);
  Feind→Held-Schaden bleibt per-client HP (`EnemyAttack`-Kommentar). Party-Größe heute unbegrenzt/2-getestet.
- Autoplay: `SessionAutoplay.cs` + `scripts/run-autoplay.sh -class <id>`; Klassenvergleich
  second-class (Fenwalker vs. Lanternbearer ±25 %). Tests: 45 EditMode- + 40 PlayMode-Dateien.

## Entscheide (gelockt, Eigner 2026-11-18, setgoal-Runde)

1. Rollen-Mapping wie GOAL.md §1; Rogue als dritte Klasse neu (`rogue`), kein Umbau der Alt-IDs.
2. Threat-Tabelle pro Gegner statt Hart-Taunt/Nähe: Beiträge aus Schaden, Heilung, Stun;
   Tank-AoE-Stun mit hohem Multiplikator. Verworfene Alternativen: reiner Taunt-Timer (binär,
   nicht balancierbar) und Nähe-only (keine Tank-Kontrolle).
3. Heal cooldown-basiert (Ziel + AoE), kein Mana — kein neues Save-/HUD-Ressourcensystem.
4. Basis-Set für alle Klassen (Nahkampf/`DiabloMelee`, Arrow, Fireball, Basis-Heal),
   Spezial-Skills ×1.5 über Klassen-Faktoren — statt klassenexklusiver Sperren oder bonusfreier Gleichheit.
5. Solo-KI simpel (Folgen, Threat-/Leader-Ziel, Cooldown-Skills, kein Loot/Inventar) — keine
   Begleiter-Ausrüstung/-Dialoge (Scope-Schutz).
6. Party max. 3, Koop ersetzt KI rollengleich 1:1 — keine 4er-Gruppe (Threat/Heal/Zonen-Balance bleibt 3er-Vertrag).

## Approach

Daten zuerst (Rolle/Faktor im Katalog), dann Threat als eigene Komponente am Gegner (Zielwahl
konsultiert sie, Fallback = heutige Nächst-Held-Logik bei leerer Tabelle), dann Klassen-Skills
(Tank-AoE, Rogue-DPS, Heiler-Heal/Support) auf dem bestehenden `DiabloSkills`-Dispatch, dann
KI-Begleiter als dünne Driver auf denselben Skill-/Heal-Pfaden (`Wisp`-Muster: Kosmetik auf
Clients, Wirkung nur mit Autorität), zuletzt Koop-Ersatz + Autoplay-Partei als Beweis.
Kompatibilität: Enum nur anhängen, alte Saves → Fenwalker, Solo ohne Party verhält sich wie heute.

## Partial-Manifest (disjunkte target_files je Stufe)

| Stufe | Inhalt | target_files |
|---|---|---|
| S1 roles-data | Rogue-Katalog, Rolle/Faktoren, Basis-Set-Gate auf, ×1.5-Vertrag | `Scripts/Classes/ClassCatalog.cs`, `Scripts/Classes/ClassDefinition.cs`, `Scripts/DiabloSkills.cs` (nur Gate/Faktor), `Scripts/Progression/CharacterSheet.cs`, `Tests/EditMode/PartyRoleDataTests.cs` (neu), `Tests/EditMode/ClassCatalogTests.cs` |
| S2 threat+tank | Threat-Komponente, Zielwahl, Tank-AoE-Stun-Skill | `Scripts/Enemies/ThreatTable.cs` (neu), `Scripts/EnemyAttack.cs`, `Scripts/EnemyHealth.cs`, `Scripts/DiabloSkills.cs` (Tank-Skill), `Tests/EditMode/ThreatTableTests.cs` (neu), `Tests/PlayMode/TankAggroTests.cs` (neu) |
| S3 rogue+healer | Rogue Fern/Nah-DPS, Heiler Ziel-/AoE-Heal + Support | `Scripts/DiabloSkills.cs` (Skill-Dispatch), `Scripts/PlayerHealth.cs` (nur falls Heal-Seam fehlt), `Tests/EditMode/PartySkillFormulaTests.cs` (neu), `Tests/PlayMode/PartySkillTests.cs` (neu) |
| S4 solo-ai | KI-Begleiter, Auffüllung fehlender Rollen | `Scripts/Party/PartyCompanion.cs` (neu), `Scripts/Party/PartyRoster.cs` (neu), `Scripts/HeroLocator.cs`, `Tests/PlayMode/SoloPartyTests.cs` (neu) |
| S5 coop-party | Max-3-Gate, KI-Ersatz rollengleich, Netz-Heal/Threat | `Scripts/DiabloNetworkPlayer.cs`, `Scripts/Net/HitValidator.cs`, `Scripts/Party/PartyRoster.cs` (Netz-Hook), `Tests/PlayMode/CoopPartyTests.cs` (neu) |
| S6 proof | Autoplay-Party, Koop-Smoke, Shotcheck, Doku | `Scripts/SessionAutoplay.cs`, `Scripts/DiabloShotCheck.cs`, `docs/mechanics-questions.md`, `docs/decision-log.md`, `docs/dev-journal.md`, `docs/party-roles-run-log.md` (neu) |

S1 zuerst. S2 braucht S1 (Tank-Rolle/Faktor). S3 braucht S1 (Rogue/Heiler-Rollen), S2 erst für
Threat-Beiträge der neuen Skills (Reihenfolge S2→S3 einhalten). S4 braucht S2+S3 (KI nutzt
Threat-Ziele + Heal). S5 braucht S4 (Ersatz-Logik). S6 zuletzt.

## Steps

### S1 — Rollen-Daten + Basis-Set
- `ClassCatalog.Rogue` (`rogue`, Anzeige „Rogue“, Fern/Nah-DPS) + Rolle (`Tank/Healer/Dps`)
  und Spezial-Faktor 1.5 in `ClassDefinition`; Fenwalker/Lanternbearer-Faktoren unverändert.
- Klassen-Gate in `DiabloSkills` so öffnen, dass Basis-Set (Fireball, Arrow, Basis-Heal-Platzhalter,
  `DiabloMelee`-Nahkampf) für jede Klasse wirkt; Spezial-Skills nur mit Klassen-Faktor ×1.5.
- Verify: `PartyRoleDataTests` (Katalog 3 Klassen, Fallback, Faktor-Mathe) + `ClassCatalogTests` grün;
  `bash scripts/run-unity-tests.sh`, `KayKitAssets.Verify`.

### S2 — Threat + Tank-AoE
- Neue `ThreatTable` am Gegner: `Add(source, amount)`, Verfall nur per Entscheid im Test gepinnt
  (Annahme: kein Decay in v1, damit deterministisch assertierbar); `EnemyAttack.AcquireHero`
  fragt höchstes Threat-Ziel, sonst heutige Nächst-Held-Logik.
- Tank-Spezial (neuer Enum-Wert angehängt): AoE um den Tank, Stun + hoher Threat-Multiplikator;
  Schaden/Heilung/Stun aller Quellen buchen Threat (Held→Gegner über bestehenden Hit-Pfad).
- Verify: `ThreatTableTests` (Buchung, Multiplikator, Zielwechsel) + `TankAggroTests`
  (Stun im Radius, Zielwechsel auf Tank, außerhalb unbetäubt) grün; Suiten + Verify grün.

### S3 — Rogue-DPS + Heiler-Skills
- Rogue-Spezial: je ein Fern- (Projektil über `DiabloProjectile`) und ein Nah-DPS-Skill
  (Kegel über `DiabloMelee`-Vertrag), Cooldowns/Schaden aus Bestands-Bändern ableiten, per Formel-Test pinnen.
- Lanternbearer-Spezial: Ziel-Heal (Reichweiten-Check wie `HitValidator`-Muster) + AoE-Gruppen-Heal
  um den Heiler; Support-Anteil = Heal-Threat-Buchung aus S2. Heal wirkt auf `PlayerHealth` von
  Spielern und KI identisch.
- Verify: `PartySkillFormulaTests` (Schaden/Heal ×1.5, Cooldowns) + `PartySkillTests`
  (Fern/Nah trifft, Ziel-Heal heilt Verbündeten, AoE heilt Gruppe, Feind wird nicht geheilt) grün.

### S4 — Solo-KI-Begleiter
- `PartyRoster` stellt beim Solo-Start die fehlenden Rollen als `PartyCompanion`
  (Follow-Offset, Ziel = Threat-Spitze/Leader-Ziel, Skills per Cooldown, Heal-Schwelle für Heiler-KI,
  Stirbt → Respawn am Camp wie `PlayerHealth.DieAndRespawn`-Muster, kein Loot/Inventar).
- `HeroLocator` kennt Party-Mitglieder (Gegner-/Heal-Ziele), ohne Solo-Semantik zu brechen.
- Verify: `SoloPartyTests` (2 KI spawnen, Rollen korrekt, Husk-Kampf mit Heal überlebt,
  kein Begleiter ohne fehlende Rolle) + Autoplay-Smoke solo grün.

### S5 — Koop-Party (max. 3, Ersatz 1:1)
- Join-Gate: vierter Beitritt wird abgelehnt/geloggt; beitretender Spieler verdrängt die KI
  seiner Rolle (`PartyRoster`-Netz-Hook), Verlassen spawnt die KI zurück.
- Heal/Threat serverseitig über `DiabloNetworkPlayer`-RPC + `HitValidator` (Reichweite, kein
  Feind-Heal, kein Fremd-Threat); KI auf Clients nur Kosmetik (Wisp-Muster).
- Verify: `CoopPartyTests` + `scripts/run-coop-pair.sh`-Smoke (Host Tank + Client Rogue,
  KI-Heiler bleibt, 0 Exceptions) grün; Suiten + Verify grün.

### S6 — Beweis + Doku
- `SessionAutoplay`-Party-Profil: Solo-3er-Gruppe spielt Session-Schleife, Ergebnis trägt
  Rollen/Kills/Heals; Abnahme Exit 0, 0 Exceptions, `G-DIABLO06`-Lauf frisch.
- Klassen-Clearzeit Rogue/Heiler-Gruppe innerhalb ±25 % der Solo-Baseline (second-class-Band),
  sonst Zahlen in S2/S3-Werten nachziehen, nicht am Tank drehen.
- Shotcheck: Tank-AoE-Stun, Rogue-Fern/Nah, Heiler-AoE-Heal als PNGs; `docs/party-roles-run-log.md`.
- Doku: neue Frage in `docs/mechanics-questions.md`, Entscheid in `docs/decision-log.md`,
  `docs/dev-journal.md`; GOAL/PROGRESS auf Belege fortschreiben.

## Validation Plan

- Je Stufe: `bash scripts/run-unity-tests.sh` (EditMode + PlayMode), `python3 scripts/check-meta.py`,
  Unity-Verify aus `repo.yaml:verify` (`KayKitAssets.Verify`), `python3 ~/scripts/agent-workspace.py check` (still).
- Höchstrisiko-Check: S2-Zielwechsel-Test — Threat muss die bestehende faire Zielwahl
  (`AcquireHero`, Encounter-/Zonen-Guards in `EnemyAttack.Update`) schlagen, ohne Solo-Verhalten zu ändern;
  Regression darüber fängt der unveränderte Solo-Pfad (leere Tabelle = alte Logik).
- S4/S5: Autoplay (`scripts/run-autoplay.sh`) + Koop-Paar (`scripts/run-coop-pair.sh`) als E2E-Beleg;
  Headless-Grenzen (echter 3-Spieler-Test) als manueller Playtest im Run-Log benennen, nicht als bestanden behaupten.
- Abschluss: alle GOAL-Erfolgskriterien mit Test-/Log-Beleg in `PROGRESS.md`, erst dann 100 % + `Offene Punkte: Keine.`

## Risks / Open Questions

- Risiken: Threat bricht faire Encounter-Guards (Plaza/Zonen in `EnemyAttack`) — Mitigation S2-Fallback + Bestandstests unverändert grün. KI-Kosten pro Frame — `HeroLocator`-Frame-Cache-Muster wiederverwenden, keine `GameObject.Find` in `Update`.
- Rollback: Jede Stufe eigener Branch/Worktree; Stufe rot = nicht landen, `main` bleibt spielbar. Enum-Anhang ist additiv, Alt-Clients brechen nicht still (int-Vertrag dokumentiert).
- Open Questions: Keine für S1–S3 (Eigner-Entscheide oben). Rogue-Modell (KayKit-Bestand vs. Recolor)
  entscheidet S1 am vorhandenen `ClassAppearance`-Pfad; kein neuer Asset-Kauf vorgesehen (Annahme).
