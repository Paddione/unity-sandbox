# Progress: cinderreach-deutsch-story

Prozent: 35 %

## Erledigt

- Goal angelegt (2026-11-23).
- S3 Story-Bibel `docs/story-bible.md`: Prämisse, Kette, drei Wendungen, Figuren, Glossar, Akt I/II mit Haupt- und Nebenaufgaben, Zwischentexte, Ausblick.
- S1 Lokalisierungs-Fundament: `Loc` (Deutsch Standard, `-lang en` Fallback, `Loc.F` mit deutscher Zahlenkultur, `Loc.Display` für Szenen-/Datennamen), `LocCatalog.*.cs` (Deutsch und Englisch je Zeile), `LocalizationTests` (Katalog konsistent, Schlüssel vorhanden, Lint gegen englische Literale mit Allowlist, alte englische Eigennamen gesperrt).
- S2 vollständige Übersetzung: alle 567 Lint-Funde plus zusätzlich gefundene Fragmente übersetzt oder begründet auf `loc-allowlist.txt`, `loc-pending.txt` leer (Obergrenze 0). Eigennamen laut Glossar, Item-/Affix-/Seltenheits-/Slot-Anzeige über den Katalog, Spielstand- und Netz-IDs unverändert. Werkzeuge `scripts/loc_lint.py`, `loc_apply.py`, `loc_sync.py`, `loc_skel.py`. Beleg: EditMode 488/488, PlayMode 280/284 → die vier Rest-Asserts angepasst, gefiltert 26/26.

## Nächste Schritte

- S4: Quest-/Dialog-/Fuhrbuch-System (Taste J), Save v13, Koop-Buchung.
- S5/S6: Akt-I- und Akt-II-Inhalte aus der Story-Bibel, NPCs Ottilie und Merle im Builder.
- S7: Nachweis (volle Suites, Autoplay, Koop-Trio, Windows-Shotcheck, `docs/story-run-log.md`).

## Offene Punkte

- S4–S7 offen. Die Steuerungshinweise nennen bereits „J Fuhrbuch“; S4 muss die Taste liefern.
