---
goal: cinderreach-deutsch-story
slug: cinderreach-deutsch-story
health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06]
plan_refs: []
---

# Cinderreach – deutsche Fassung und Aktgeschichte

## Objective

Cinderreach wird vollständig deutschsprachig. Akt I und Akt II bekommen je eine Hauptaufgabenkette mit Nebenaufgaben, die zusammen eine eigenständige, nicht generische Gesamtgeschichte erzählen. Die Geschichte wird über NPC-Dialoge, ein Questlog und Zielmarker vermittelt. Ausgangslage: [CINDERREACH.md](../CINDERREACH.md).

## Erfolgskriterien

1. Lokalisierung: eigene Stringtabelle (`Loc.T(key)`), Deutsch als ausgelieferte Sprache, Englisch als Fallback. Jeder spielersichtbare Text läuft über die Tabelle: Menüs, HUD, Tooltips, Items/Affixe/Uniques, Skills, Klassen, Gegner/Bosse, Overlays, Account-/Koop-/Fehlertexte, Waypoints, Händler/Schmiede/Truhe, Auftragsbrett. Ein EditMode-Test findet 0 fehlende Keys und 0 spielersichtbare englische Literale. Ausnahmen (Logs, Netz-/Save-IDs, CLI-Flags) stehen auf einer expliziten Allowlist.
2. Eigennamen eingedeutscht (Orte, Zonen, Klassen, Gegner, Bosse, Uniques). Der Produktname „Cinderreach“ bleibt. Die Namensliste steht im Glossar der Story-Bibel, `IdentityTests` sperrt die alten englischen Anzeigenamen.
3. Story-Bibel `docs/story-bible.md`: Weltprämisse, zentraler Konflikt, Antagonisten mit Motiv, Rolle jeder Zone und jedes Bosses in der Geschichte, Bogen über Akt I → Akt II mit Wendepunkt und ein offener Ausblick nach dem Glasmacher.
4. Pro Akt eine Hauptaufgabenkette, die die bestehenden Bosskill-Tore (`ActProgress`) erzählerisch begleitet, ohne sie zu ersetzen, plus 2–3 optionale Nebenaufgaben mit eigener Lore und Belohnung. Zieltypen über Bosskills hinaus (Fundstücke, Gespräche, Orte, Kampfbedingungen).
5. Präsentation: benannte NPCs in der Siedlung (bestehender Händler, Schmied und Auftragsbrett als Figuren plus 1–2 neue NPCs) mit Dialogfenster, Questlog (Taste J), Zielhinweis im HUD und Zwischentexte bei Bosskills und Aktwechsel. Keine Vertonung.
6. Persistenz und Koop: Queststand im Save (Migration v12 → v13 mit Backup, `SaveMigrationTests` auf `CurrentVersion`). Im Koop gilt der Host-Stand, Clients buchen ihn in ihren eigenen Save wie heute die Bosskills. Der dedizierte Server läuft ohne UI-Abhängigkeit weiter.
7. Nachweis: EditMode/PlayMode voll grün, `SandboxBuilder.VerifyDiabloZone` grün, Session-Autoplay (G-DIABLO06) durchläuft den Story-Pfad, Koop-Trio-Lauf (`COOP_CLIENTS=2`) mit synchronem Queststand, Windows-Shotcheck mit Screenshots von Dialog, Questlog, deutschen Menüs und Tooltips. Belege in `docs/story-run-log.md`. `CINDERREACH.md` nachgezogen, Goal danach entfernt.

## Qualitätsanspruch Story (verbindlich)

- Die Geschichte wächst aus dem, was das Spiel schon hat: Asche, Laternen, Ofen, Glas, Beinhaus, Abt, Ofenmutter, Glasmacher. Bosse sind Figuren mit Motiv und Vorgeschichte.
- Keine Fantasy-Klischees als Tragwerk: kein „uraltes Böses erwacht“, kein „Auserwählter“, kein Prophezeiungs-Trope. Moralische Ambivalenz und mindestens eine echte Wendung pro Akt.
- NPCs haben eigene Stimme, Interessen und Widersprüche. Nebenaufgaben verändern den Blick auf die Hauptgeschichte.
- Deutsch als Originalsprache: natürliche Sprache, einheitlicher Ton (düster, knapp, konkret), konsistente Begriffe laut Glossar.
- Die Klassenrollen (Tank, Heiler, DPS) und die Gruppe sind erzählerisch begründet.

## Scope und Nicht-Ziele

Scope: Code unter `sandbox/Assets/Scripts/` (neu `Localization/`, `Story/`), Builder für NPC-Positionen, UI über `DiabloUIFactory`, Tests unter `sandbox/Assets/Tests`, Sprites nur über die Blender-Pipeline.

Nicht-Ziele: neue Zonen, Bosse oder Akte; Vertonung und Kamerafahrten; das Unity-Localization-Paket und eine Sprachwahl über den englischen Fallback hinaus; Umbenennung interner Bezeichner (`Diablo*`, Szenendateien, `classId`s, Enum-/Netz-Verträge); Story-Gating der Zonentore über die Bosskill-Logik hinaus.

## Randbedingungen

- Szenen nur über Builder regenerieren, LFS-Controller-Seiteneffekte vor dem Commit revertieren.
- `DiabloSkills.Skill` und Netz-Enums nur anhängen. Save-Felder additiv mit Migration.
- Keine Fremd-IP, kein Asset-Store-Kauf ohne Freigabe.
- Autoplay- und Shotcheck-Pfade dürfen nicht an Dialogen blockieren. Perf-Nachweis nur per Windows-Shotcheck.

## Stufen

S1 Lokalisierungs-Fundament + Lint-Test, S2 vollständige Übersetzung + Eigennamen, S3 Story-Bibel, S4 Quest-/Dialog-/Questlog-System + Save v13, S5 Akt-I-Inhalte, S6 Akt-II-Inhalte + Koop-Sync, S7 Nachweis.
