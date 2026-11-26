---
goal: cinderreach-class-kits-parity
slug: cinderreach-class-kits-parity
health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06, G-DIABLO07]
plan_refs: []
---

# Goal: cinderreach-class-kits-parity — Moorgänger und Schmuggler auf Laternenträgerin-Niveau

Angelegt: 2026-11-26. Herkunft: Eigner-Anfrage „rework schmuggler und moorgänger to have equally quality of skills as the lanternguy, design interesting skillcombos, adjust the unique items too“ (setgoal-Runde 2026-11-26, alle Empfehlungen übernommen). Ausgangslage: `.agents/goals/CINDERREACH.md`.

## Objective

Moorgänger (`fenwalker`) und Schmuggler (`rogue`) erhalten je 6 klasseneigene Skills auf Q/E/R/Space/F/G, mindestens 2 Setup→Payoff-Combos pro Klasse und mindestens einen Variant-Skill. Ihre Uniques werden auf die Combos ausgerichtet; je Klasse kommt ein neues Combo-Unique (Pendant zu `wick-of-the-halo`) hinzu.

## Erfolgskriterien

1. `ClassCatalog.Fenwalker` und `ClassCatalog.Rogue` führen je 6 klasseneigene Skills; `Fireball`/`Arrow` stehen in keinem Klassen-Kit. `DiabloSkills.Skill` nur angehängt (Netz-Int-Vertrag).
2. Je Klasse ≥ 2 Combos: Skill A setzt einen Zustand, Skill B zieht daraus einen messbaren Bonus. Je Combo ein EditMode-Formeltest.
3. `SkillVariantCatalog`: je Klasse ≥ 1 Variant-Skill mit 2 Varianten und echtem Trade-off; Sheet-Vorschau = Laufzeitwerte.
4. Uniques Lanzerwehr, Bollwerk, Erdbebenkrone, Nachtzahn, Schattenschritt, Scharfer Takt behalten ihre IDs, Effekte verstärken Combo/Variante. Je Klasse 1 neues Combo-Unique (Loot, Tooltip DE/EN, Test). Laterne-Uniques auf Gleichwertigkeit geprüft.
5. Jeder neue Skill hat Icon (SkillArt), VFX, Audio-Cue (Manifest) und DE/EN-Texte.
6. `SessionAutoplay` spielt beide Klassen mit Combos (Exit 0, alle Phasen); Windows-Shotcheck zeigt je Klasse einen Combo-Effekt und beide Varianten.
7. Alte Saves laden; Ränge auf entfallenen Skills werden als Skillpunkte erstattet (Test).
8. Koop: neue Skills und Combo-Zustände über den int-Skill-Pfad mit Host-Validierung; Koop-Test oder Autoplay grün.

## Scope

`DiabloSkills`, `ClassCatalog`, `SkillVariantCatalog`/`SkillVariantTargeting`, `CharacterSheet`, `BuildUniqueEffects`/`ItemDatabase`/Loot, Lokalisierung, SkillArt/VFX/Audio, `SessionAutoplay`, `DiabloShotCheck`, `PartyCompanion`, Tests.

## Nicht-Ziele

- Keine klassenübergreifenden Party-Combos.
- Kein Umbau der Laternenträgerin-Skills (nur Unique-Nachjustierung).
- Keine neuen Klassen, Item-Slots oder `GearUniqueEffects`-Änderungen.

## Randbedingungen

- Fenwalker bleibt Fallback für unbekannte IDs; Invariante „Fenwalker = Vor-Klassen-Kit“ entfällt bewusst.
- Enum-Werte und Unique-IDs nie verschieben/löschen. Rollen: Moorgänger Tank, Schmuggler DPS.
- Performance-Nachweis nur über Windows-Shotcheck. Abschluss per Worktree + `agent-workspace.py land`, Windows-Stage-Deploy, Server-Image-Pin in dotfiles bei Server-Codeänderung.

## Abhängigkeiten / Referenzen

- `.agents/goals/CINDERREACH.md` (Klassen und Skills, Uniques)
- health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06, G-DIABLO07]
