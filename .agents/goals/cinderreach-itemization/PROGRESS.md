# Progress – cinderreach-itemization

Prozent: 83 %

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

- S4 Neun Rollen-Uniques gelandet (Basis `c94259d`): Matrix `(PartyRole, Slot)` in `ItemDatabase` — Fenwalker `bulwark-carrier` (Rüstung, Block→Threat) + `seismic-crown` (Helm, Slam-Stun Primärziel +2 s), Lanternbearer `mercy-shroud` (Rüstung, Zielheal→Schild 5 HP/4 s) + `circle-of-dawn` (Helm, Gruppenheal-Zusatzimpuls schwächster Verbündeter), Rogue `shadowstride-vest` (Rüstung, Flurry→+30 % Tempo/3 s) + `keen-cadence` (Helm, erster Crit−2 s Rest-Cooldown) + Waffen-Bestand (`lancer-ward`, `wick-of-the-halo`, Rogue-ID mit Pierce+1). Rolls Top-Bracket, frei tragbar, Unknown-ID-Reject host-seitig, Save/Stash-Roundtrip, Schmiede-Regel erhalten.
- S4 EditMode 355/355, PlayMode 245/246 (BuildUnique-Suiten erweitert: Matrix→Slot/Drop-Pfad/Top-Tier/Tragbarkeit/Roundtrip/Schmiede/Rate, je Effekt Equip/Trigger/Ablegen/Reload/2-Träger/Dauer). `check-meta.py` OK. Fremdbefund: `DiabloMeleeReworkTests.Thrust_OutreachesHuskMelee` („Thrust must not move the hero“, 1,03 m Drift in 1 Frame) schlägt auch auf sauberem `main` (5eb0f32, ohne S4) fehl — Vorbefund aus dem Party-Roles-S5-Fenster, kein S4-Regress; Rebase-Konflikt `DiabloSkills.cs` (S5-`RelayHeal` vs. S4-Schild/Impuls) so gelöst, dass beides läuft und der Dawn-Bonus mit-relayed wird.

## Nächste Schritte

- S5 Gegenstandsgrafik/Inventar/Händler gelandet: `DiabloMenuArt.ArtKeyFor` + `ItemIcon(ItemInstance)` mit explizitem Map-Eintrag je auslieferbarer Basis (19, inkl. Starter-/Klassenwaffen und Heiltrank) und je Matrix-/Legacy-Unique (15 IDs); Bögen/Laternen bekommen eigene prozedurale Silhouetten. `LootDropView` (NEU) zeigt Boden-Sprite + Raritätsglanz aus `DiabloPalette.Rarity` (Magic blau/Rare gelb/Unique orange), rein aus dem S2-Snapshot abgeleitet (Host=Client=LateJoin). `ItemDetailsView` (NEU) ist die EINE Karte für Inventar UND Händler (Name/Rarität/Slot → Itemlevel/Altbestand → Rolls → +/-Deltas → Unique-Effekt; leere/untragbare Slots erklären Zustand+Aktion). Equip/Sell/Discard laufen sichtbar getrennt über Instanz-ID mit Notice-Feedback (volle Tasche/Ablehnung) und sofortigem Refresh; Discard spawnt dieselbe Instanz, Re-Pickup behält Rolls.
- S5 EditMode 358/358 (inkl. Art-Coverage: jede Basis + jede Matrix-/Legacy-ID löst expliziten Key, kein Fallback-Unknown; Distinktions-Test). PlayMode 252/253 — einziger Fail der bekannte Vorbefund `DiabloMeleeReworkTests.Thrust_OutreachesHuskMelee` (schlägt auch auf sauberem main, kein S5-Regress); neu `ItemPresentationTests` 5/5 (Ground-Sprite+Glow je Rarität, Snapshot-Parität, Karten-Hierarchie/Deltas/Unique-Text, Equip→Discard→Re-Pickup mit Rolls-Erhalt, Volltasche/Ablehnung) und Vendor-S5 2/2 (SellById+Notice, Broke-Buy-Notice); Vendor-Filterlauf 10/10 (ItemPresentation+VendorLoop). `check-meta.py` OK (neue .cs je mit .meta).
- S5-Shots (Linux-Player `-shotcheck`, je 960×540 + 1920×1080, RC 0): `artifacts/shots/s5-{960x540,1920x1080}/{35_s5_ground_rarities,36_s5_inventory_details,37_s5_vendor}.png` (sha16: 960er 76103aa4/9e5d9b10/3bf77515, 1920er dd7c2783/5475412f/d4c6f616). Sichtbefund: Boden-Drops je Rarität mit eigener Silhouette (Bogen/Laterne/Rüstung, Billboard) + Glanz; Tasche „Bag 3 of 20" mit Raritätsrahmen + Detailkarte (Name/Rarität/Slot → Itemlevel → Rolls → vs.-equipped-Deltas → Unique-Effekt) + getrennte Buttons „Equip selected"/„Discard selected"; Händler mit „Gold: 200", 6 Buy-Rows mit Preisen + Potion + getrennten Sell-Rows. Nebenbefund gefixt: Händler-Spalten/Row-Container hatten keine VerticalLayoutGroups (Rows überlappten).

## Offene Punkte

- Die für die Rogue-Modifier benötigten Kampffähigkeiten kommen aus dem laufenden Party-Roles-Vorhaben.
- S4 bis S6 und die abschließenden Health-Gates stehen aus.
