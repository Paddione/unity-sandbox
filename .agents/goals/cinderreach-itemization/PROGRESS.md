# Progress – cinderreach-itemization

Prozent: 50 %

## Erledigt

- Setgoal-Entscheidungen, Erfolgskriterien und Health-Gates sind im Goal festgehalten. Der [Implementierungsplan](../../plans/cinderreach-itemization/tasks.md) ist bidirektional verlinkt.
- S1 gibt Waffen, Rüstungen und Helmen Itemlevel anhand ihrer Dropquelle. Affix-Rolls und Schmiede verwenden dieselben Brackets. Neu erzeugte Uniques verwenden deren Obergrenzen.
- Save v10 ergänzt historische Gegenstände um stabile Instanz-IDs und erhält vorhandene Affixe, Unique-IDs und Schmiedebindungen.
- EditMode vor Umsetzung 311/313, genau die beiden neuen Assertions schlugen fehl. Nach Umsetzung 325/325, inklusive Quellstufe, Bracket-Grenzen, Schmiede und Save-Migration. PlayMode 187/187 und Asset-Metadatenprüfung grün.

- S2 verwendet ein registriertes Loot-Prefab und repliziert den vollständigen Gegenstandssnapshot. Der Host bestätigt Besitzwechsel und verwaltet die Inventar-, Ausrüstungs-, Lager- und Händleraktionen.
- S2 EditMode 332/332 und PlayMode 194/194. Die neuen Fälle prüfen Late Join mit exakten Rolls, Ablehnung bei voller Tasche, denselben Gegenstand nach Wegwerfen und Wiederaufheben sowie einen erneuten Verkauf derselben Instanz.

- S2 Linux-Player und Zwei-Prozess-Koop grün (Host 8 Phasen, Client Town/Fen bis zum Ossuary-Transfer, beide Exit 0 und keine Exceptions). Der Player protokolliert NGO-Warnungen beim Setzen des anfänglichen Loot-Snapshots vor Spawn.

- S3 Krit/Fläche/Host-Validierung gelandet (Branch `feature/cinderreach-itemization`, Basis `bf9db19`): `GetCrit` = 5 % Basis + Legacy-`Crit` + `CritRating`/10 (Cap 75 %, 2× beibehalten); `ScaleArea` = `r·sqrt(1+inc/100)` (Cap 100 %, Treffer + VFX teilen sich den Radius für Cleave/Halo/Slam/Flurry/Renew/Feuerball-Explosion); 1 Wurf je Direkttreffer, Ticks/Thorns/Heal nie Crit (`HaloZone.Tick` mit `canCrit:false`); Koop-Intent (Skill+Anker/Ausrichtung, nie Schaden) mit Host-Berechnung aus S2-Ausrüstung + Crit-Flag-Replikation; Tooltip mit Flächenprozent.
- S3 EditMode 340/340, PlayMode 213/213 (14/14 `ItemCombatStatsTests`: Formel/Cap, 2×, Server-Crit vs. Eingabe-Cap, sqrt+Cap, Slot-Spannen, Tooltip, skalierte Kanten innen/außen, Blast skaliert/Arrow unskaliert, Gruppenheil-Ring, Ticks/Thorns/Heal nie Crit, Threat nach Crit, Intent-Gate, Host-Execute+Cooldown). `check-meta.py` OK.

## Nächste Schritte

- S4 auf aktuellem main beginnen (9er Rollen-Unique-Matrix; Rogue-Nachweise brauchen Party-Roles-Skills vom laufenden Party-Roles-Vorhaben).

## Offene Punkte

- Die für die Rogue-Modifier benötigten Kampffähigkeiten kommen aus dem laufenden Party-Roles-Vorhaben.
- S4 bis S6 und die abschließenden Health-Gates stehen aus.
