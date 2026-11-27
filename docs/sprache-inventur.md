# Sprache-Inventur (`cinderreach-sprache-voice` S1)

Stand: HEAD `6bf15a6`, gezählt am 2026-11-27 (frisch, nicht übernommen).
Quelle der Zählung: `E("…")`-Einträge in den Loc-Katalogen, Sprecherpositionen in
`sandbox/Assets/Scripts/Story/StoryDialogs.cs`/`sandbox/Assets/Scripts/Story/StoryBarks.cs`, `python3 scripts/check-voice-lines.py`, Clips unter
`sandbox/Assets/Resources/Voice/*.ogg`.

## Gesamt

| Gruppe | Anzahl |
|---|---|
| Vertonte Zeilen (Dialog + Barks) | **115** |
| Katalogzeilen Story (Titel, Ziele, Namen, UI-Chrome) | **123** |
| Textzeilen gesamt | **238** |
| Voice-Clips (`.ogg`) | **115** — Deckung 100 % |

`scripts/check-voice-lines.py` (nur gelesen, nichts geändert): `voice lines: 115, problems: 0`.

## Pro Datei

| Datei | Inhalt | Anzahl |
|---|---|---|
| `sandbox/Assets/Scripts/Localization/LocCatalog.Story.cs` | 11 `npc.*`, 9 `story.item.*`, 75 `quest.*` (Titel/Zusammenfassung/Ziele), 28 UI-Chrome (`story.log/tracker/reward/dialog/status/quest`) | 123 |
| `sandbox/Assets/Scripts/Localization/LocCatalog.Dialog.cs` | 107 `dlg.*` + 8 `bark.*`, jede mit `en`-Fallback | 115 |
| `sandbox/Assets/Scripts/Story/StoryDialogs.cs` | 41 Skripte → 107 Zeilenpositionen; `x\|nK`-Varianten: `h3.offer.6\|n2`, `h4.done.3\|n3`, `h5.offer.5\|n1`, `h9.talk.merle.4\|n4`, `h9.talk.merle.5\|!n4`, `h9.talk.ottilie.4\|n5` | 107 |
| `sandbox/Assets/Scripts/Story/StoryBarks.cs` | 8 Barks (je Boss Phase 2 + besiegt, plus Erzähler `h4.start`, `h7.done`) | 8 |
| `docs/audio/voices.json` | 8 Stimmen: `abbot`, `glasswright`, `ilsabe`, `jorin`, `kilnmother`, `merle`, `ottilie`, `narrator` | 8 |

Alle 115 Dialog-/Bark-Einträge haben einen `en`-Fallback (S2-Notiz: Glättung hält ihn sinngetreu; mit `-lang en` bleiben Clips stumm).

## Neu seit Baseline (Commit `6646cff`, Akt-I-Vertiefung S1)

28 `quest.*`-Einträge in `sandbox/Assets/Scripts/Localization/LocCatalog.Story.cs`: H14–H17 je Titel + Zusammenfassung + 2 Ziele (16),
N8–N11 je Titel + Zusammenfassung + 1 Ziel (12). Dazu 6 neue `story.item.*`-Namen
(`fuhrfetzen`, `siegelband`, `tafelbruch`, `grablicht`, `fuhrliste`, `russseite` — Bestand
3 plus 6 neue = 9; Pickup-Anzeige weiter `story.pickup` „Aufgehoben: {0}“).

## Noch offen (nicht zählen, sondern anmerken)

- **Akt-II-S1- und Akt-III-S1-Textstände fehlen** — ihre Neutexte kommen per Staffelung zuerst durch
  ihre Quest-Goals, dann hier durch S2 (erneutes Rendern sonst doppelt).
- **N8–N11-Folgezeilen fehlen:** Die Bibel (§Folgezeilen) verlangt je eine bedingte Zeile in
  H2-Angebot (N8), H3-Angebot (N9), H4-Abschluss (N10), H5-Angebot (N11). In `sandbox/Assets/Scripts/Story/StoryDialogs.cs`
  gibt es keine `|n8`…`|n11`-Flags und in `sandbox/Assets/Scripts/Localization/LocCatalog.Dialog.cs` keine zugehörigen Schlüssel —
  das ist S2-Arbeit nach dem jeweiligen Quest-S1-Textstand, kein S1-Defekt.
- **Keine `dlg.h14`–`dlg.h17` / `dlg.n8`–`dlg.n11`-Schlüssel:** H14–H17/N8–N11 haben Stand heute nur
  Titel/Ziele/Fundstücknamen; eigene Dialogskripte sind nicht vorgesehen (sie vertiefen Bestandsschritte).
- Voice-Seite: 115/115 Clips vorhanden, Gate grün — S2 rendert nach der Textglättung neu
  (`scripts/voice/render_voices.py` + `scripts/voice/qa_transcribe.py`); kein Clip wird in S1 angefasst.
