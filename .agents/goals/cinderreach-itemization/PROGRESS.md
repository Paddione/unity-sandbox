# Progress – cinderreach-itemization

Prozent: 33 %

## Erledigt

- Setgoal-Entscheidungen, Erfolgskriterien und Health-Gates sind im Goal festgehalten. Der [Implementierungsplan](../../plans/cinderreach-itemization/tasks.md) ist bidirektional verlinkt.
- S1 gibt Waffen, Rüstungen und Helmen Itemlevel anhand ihrer Dropquelle. Affix-Rolls und Schmiede verwenden dieselben Brackets. Neu erzeugte Uniques verwenden deren Obergrenzen.
- Save v10 ergänzt historische Gegenstände um stabile Instanz-IDs und erhält vorhandene Affixe, Unique-IDs und Schmiedebindungen.
- EditMode vor Umsetzung 311/313, genau die beiden neuen Assertions schlugen fehl. Nach Umsetzung 325/325, inklusive Quellstufe, Bracket-Grenzen, Schmiede und Save-Migration. PlayMode 187/187 und Asset-Metadatenprüfung grün.

- S2 verwendet ein registriertes Loot-Prefab und repliziert den vollständigen Gegenstandssnapshot. Der Host bestätigt Besitzwechsel und verwaltet die Inventar-, Ausrüstungs-, Lager- und Händleraktionen.
- S2 EditMode 332/332 und PlayMode 194/194. Die neuen Fälle prüfen Late Join mit exakten Rolls, Ablehnung bei voller Tasche, denselben Gegenstand nach Wegwerfen und Wiederaufheben sowie einen erneuten Verkauf derselben Instanz.

- S2 Linux-Player und Zwei-Prozess-Koop grün (Host 8 Phasen, Client Town/Fen bis zum Ossuary-Transfer, beide Exit 0 und keine Exceptions). Der Player protokolliert NGO-Warnungen beim Setzen des anfänglichen Loot-Snapshots vor Spawn.

## Nächste Schritte

- S3 auf aktuellem main beginnen. Krit-Wertung, Flächenskalierung und serverseitige Trefferauflösung integrieren.

## Offene Punkte

- Die für die Rogue-Modifier benötigten Kampffähigkeiten kommen aus dem laufenden Party-Roles-Vorhaben.
- S3 bis S6 und die abschließenden Health-Gates stehen aus.
