---
goal: cinderreach-party-roles
slug: cinderreach-party-roles
health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO06]
plan_refs: [.agents/plans/cinderreach-party-roles/tasks.md]
---

# Goal: cinderreach-party-roles — Gruppen-Spiel mit Tank, Heiler, Rogue und Solo-KI

Angelegt: 2026-11-18. Herkunft: Eigner-Anfrage „Group-Play with Healing spells, aggro and a third class“ (setgoal-Runde 2026-11-18, alle Punkte mit Empfehlung entschieden; Persistenz + Ausführungsplan auf Eigner-Wunsch).

## Objective

In `unity-sandbox` (`sandbox/`, Unity 6 URP, Cinderreach) Gruppen-Spiel mit drei Rollen einführen: Fenwalker wird Tank (Knight), Lanternbearer wird Heiler/Support, neue dritte Klasse Rogue wird Fern-/Nahkampf-DPS. Solo wird die Gruppe per KI-Begleitern auf drei Rollen aufgefüllt, im Koop ersetzen Spieler die KI derselben Rolle 1:1.

## Entschiedene Mechaniken (Eigner, 2026-11-18)

1. **Rollen:** `fenwalker`=Tank, `lanternbearer`=Heiler/Support, neu `rogue`=Ranged/Melee-DPS. Bestehende IDs, Modelle und Saves bleiben gültig (Fenwalker-Fallback für unbekannte IDs).
2. **Aggro:** Threat-Tabelle pro Gegner — Schaden, Heilung und Stun erzeugen Threat; Tank-AoE mit Stun und hohem Threat-Multiplikator zieht Aggro. Zielwahl ersetzt die reine Nächst-Held-Logik in `EnemyAttack.AcquireHero`.
3. **Heilung:** Ziel-Heal + AoE-Gruppen-Heal, Cooldown-basiert (kein Mana), wirkt auf Koop-Spieler und KI-Begleiter gleich (über `PlayerHealth.Heal`).
4. **Basis für alle:** Jede Klasse kann das Basis-Set (Nahkampf, Arrow, Fireball, einfacher Heal); Klassen-Spezial-Skills skalieren ×1.5 (Schaden/Heilung/Threat) über die Klassen-Faktoren in `ClassDefinition`.
5. **Solo-KI:** Automatisch auf 3er-Gruppe auffüllen (fehlende Rollen Tank/Heiler/DPS), simpel: folgen, Ziel aus Threat/Leader übernehmen, Skills per Cooldown, kein eigenes Loot/Inventar.
6. **Gruppe:** Max. 3 (je 1× Tank/Heiler/DPS), host-autoritativ (NGO); beitretende Spieler ersetzen die KI derselben Rolle 1:1.

## Erfolgskriterien

1. Tank: AoE-Fähigkeit betäubt Gegner im Radius und erzeugt höchsten Threat — PlayMode-Test belegt Gegner-Zielwechsel auf den Tank; Gegner außerhalb bleibt unbetäubt.
2. Rogue: Fern- und Nahkampf-DPS-Fähigkeiten treffen und schaden; Heiler: Ziel-Heal heilt verletzten Verbündeten, AoE-Heal die Gruppe — Tests grün, Heilung auf Spieler und KI identisch.
3. Basis-Test: Jede Klasse wirkt die Basis-Skills; Spezial-Skill der eigenen Klasse schadet/heilt messbar stärker (Faktor-Test ×1.5 grün).
4. Solo: Start ohne Koop spawnt zwei KI-Begleiter der fehlenden Rollen; die Gruppe besteht eigenständig einen Husk-Kampf inkl. Heal (PlayMode-/Autoplay-Beleg).
5. Koop: Beitretender Spieler ersetzt KI gleicher Rolle (Gruppe bleibt ≤ 3); Threat-/Heal-Pfad läuft über `DiabloNetworkPlayer`/`HitValidator`, keine Solo-Sonderlogik (Test/Smoke grün).
6. Alt-Saves laden weiter, `DiabloSkills.Skill`-Enum-Werte nur angehängt (Netz-int stabil); EditMode-/PlayMode-Suiten, `KayKitAssets.Verify` (`repo.yaml:verify`), `python3 scripts/check-meta.py`, `python3 ~/scripts/agent-workspace.py check` grün; Eintrag in `docs/mechanics-questions.md` + `docs/decision-log.md`.

## Scope

- `sandbox/Assets/Scripts/Classes/ClassCatalog.cs`, `ClassDefinition.cs` (Rogue, Rollen, Faktoren)
- `sandbox/Assets/Scripts/DiabloSkills.cs` (Tank-AoE/Stun, Rogue-Skills, Heal-/Support-Skills, Basis-Set ohne Klassen-Gate)
- Gegner-Threat/Zielwahl: `EnemyAttack.cs` (`AcquireHero`), `EnemyHealth.cs`, neue Threat-Komponente
- KI-Begleiter (neu) + Gruppen-Auffüllung am Helden-/Spawn-Pfad, `HeroLocator.cs`
- Koop: `DiabloNetworkPlayer.cs`, `Net/HitValidator.cs` (Heal-/Threat-Validierung, Rollen-Ersatz)
- `SessionAutoplay.cs` (Party-Profil) + Tests unter `sandbox/Assets/Tests/{EditMode,PlayMode}`

## Nicht-Ziele

- Kein Mana-System, kein Begleiter-Inventar/-Dialog, keine Gruppe > 3, kein PvP/Respec
- Keine neue Health-Probe, kein Final-Art, keine Save-Migration über den Fenwalker-Fallback hinaus
- Keine Solo-only-Abkürzung im Schadens-/Heal-Pfad

## Abhängigkeiten / Referenzen

- `cinderreach-second-class` (100 %): `ClassCatalog`/`ClassDefinition`, `DiabloSkills.Skill` 0–6, Save-`classId`
- `cinderreach-melee-rework` (100 %, `ffbcc0a` auf `main`): ortsfester Kegel, Stoß, Block — Basis-Nahkampf-Vertrag
- Koop-Basis: `DiabloNetworkPlayer`, `Net/HitValidator`, `EnemyAttack.AcquireHero` (nächster lebender Held, S2-Koop-Zielwahl)
- Heal-Basis: `PlayerHealth.Heal`, `CampfireHeal`; Ziel-Basis: `HeroLocator`
- Ausführung: `.agents/plans/cinderreach-party-roles/tasks.md` (Stufen S1–S6, je eigener Worktree + `land: ff`)
- health_goals: [G-DIABLO01, G-DIABLO02, G-DIABLO03, G-DIABLO06]
