# Fortschritt

Prozent: 100 %

## Erledigt

- Goal angelegt (2026-11-25).
- K1/K2: `DiabloInventoryUI` zeigt auf einer Seite links den Live-3D-Helden (`HeroPaperdollPreview`, RenderTexture-Klon auf eigener Bühne, per Ziehen drehbar) mit den Plätzen Helm, Rüstung und Waffe und rechts das Taschenraster. Der Klon spiegelt das Equipment des Helden über `HeroEquipmentAppearance.Initialize(owner, source)`. PlayMode `Paperdoll_RendersLiveHeroAndMirrorsWornUnique` belegt, dass ein angelegtes Unique am Modell erscheint und beim Ablegen verschwindet.
- K3: `ItemTooltip` + `ItemCompare`: Umschalt zeigt die angelegte Karte daneben, grünes ▲ / rotes ▼ mit Delta, gleiche Werte ohne Pfeil, leerer Platz komplett grün, Tränke ohne Vergleich (EditMode `ItemCompareTests`, 6 Tests).
- K4/K5: Drag & Drop Tasche → Platz/Held, Platz → Tasche, falscher Platz rot und ohne Statewechsel, Rechtsklick tauscht bzw. trinkt (PlayMode `InventoryPaperdollPlayTests`, 4 Tests, inkl. echter `ExecuteEvents`-Drag-/Drop-/Klick-Events).
- K6: alle Swaps laufen über `EquipIndex`/`UnequipSlot` und damit über `DiabloNetworkPlayer.RequestItemAction`; Tränke im Netz über `DrinkPotion` in `ItemAction`.
- K7: Truhe, Händler (Bestand und Tasche) und Schmied hängen `ItemTooltip.AttachHover` an ihre Zeilen.
- K8: Upgrade-Pfeil an Taschenzellen, Rarity-Rahmen, Slot-Tönung beim Ziehen, Werte-Seite mit Klassenbanner, Pause-Menü mit Porträt, Heldenname und Tastenhinweis. Zeilenumbrüche aus dem LocCatalog (`\\n`) werden zentral in echte Umbrüche gewandelt (vorher erschien „Kampfwerte\nSCH“ wörtlich).
- Nebenbefund behoben: `Instantiate` des Helden machte leere Slots am Original zu leeren `ItemInstance` (`CloneHeroVisual` normalisiert jetzt). Vorbestehender Rot-Test `HeroEquipmentAppearanceTests` (männlicher Rogue ohne Ersatzmodell seit `e137330`) korrigiert.
- K9/K11 (Teil): EditMode 572/572, PlayMode 345/345 (`bash scripts/run-unity-tests.sh`, 2026-11-25), `check-meta` OK.
- K10: Windows-GPU-Shotcheck 1920x1080 (Ordner `cinderreach-inventory-shots` unter `docs`: `23_inventory_open`, `24_inventory_compare` mit ▲/▼, `20_vendor_compare` mit leerem Platz, `31_pause_menu`). Windows-Autoplay: 12 Phasen, 0 Exceptions, fps-gate PASS, schlechtestes Fenster 113,1 fps (Schwelle 60).

- Gelandet auf `main` als `6ab5a17` (Fast-Forward, gepusht; `check` und `verify` grün).
- K11: On-Demand-Autoplay Run `45684409673` (Normal, Kindled, Lanternbearer) grün; `/health` 2026-11-25: `G-DIABLO01`, `G-DIABLO02`, `G-DIABLO03` (EditMode 572/572, PlayMode 345/345) und `G-DIABLO06` achieved.

## Nächste Schritte

Keine.

## Offene Punkte

Keine.
