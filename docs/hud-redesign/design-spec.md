# Design-Spec: HUD im Spiel (lokale Ablage des Canvas-Artboards)

Quelle: Canvas https://claude.ai/artifact/JHgZfCWrHt6N8P1cdHwR4G, Artboard
„HUD im Spiel“ (`Main.dc.html`, 1920×1080). Das Artboard ist außerhalb von
Claude.ai nicht abrufbar — **diese Datei ist die lokale Source of Truth** für
das Design. Umsetzung: Goal `cinderreach-hud-redesign` (100 %, gelandet
`7176e5a`), Belege `HudThemeTests`, `HudLayoutPlayTests`,
`PartyFramesPlayTests`, Shots `hud_solo.jpg`, `hud_party.jpg`,
`hud_target_elite.jpg`, `compare.jpg` (alle in diesem Ordner).

## Grundbausteine (gelten auch für Inventar + Karte)

- Fonts (OFL, `sandbox/Assets/Resources/Fonts/`): **Cinzel Bold** als
  `DisplayFont` (Überschriften, Zahlen), **Alegreya Sans Regular/Bold** als
  `BodyFont` (Fließtext). Legacy-`Text` bleibt, kein TMP-Umstieg.
- Farben (`DiabloPalette.cs`): Panel #14101A (α 0,9), PanelEdge #3A2E22,
  TrimGold #F2B134, Parchment #E8D9BE, Blood #B31217, Schild #8CB7E0,
  Cooldown-Abdunklung (0.04, 0.035, 0.063, 0.78), Magisch-Text #6E9BF2,
  Effekt-Orange #FF9A4D. Bausteine in `HudChrome.cs` (`Panel` mit 1-px-Bronzerand
  + optionaler Gold-Oberkante, `Badge`, `Bar`); raycast-frei außer explizit.
- HD-Scaler 1920×1080, Match 0,5.

## Unterer Block (Mitte/unten)

- **Lebenskugel** 176 px mit 5-px-Bronzerand + Gold-Ring, Glanzfleck oben links,
  Zahl in Cinzel 26 px + Unterzeile „LEBEN“. Schild-Chip über der Kugel,
  sichtbar wenn Schild > 0.
- **Skillplatte** (`HudChrome.Panel` mit Gold-Oberkante, Breite aus Slotanzahl):
  Slots 72 px, Taste oben links (Gold, Bold), Skillname darunter (13 px,
  ParchmentDim), Cooldown als radialer Sweep + Restsekunden groß in Cinzel,
  Akzentrand für den Primärskill in Klassenfarbe. Flaschenslot als letzter Slot
  der Leiste (Cooldown-Sweep wie Skills).
- **EP-Leiste** 8 px innerhalb der Skillplatte unter den Slots, links `Stufe N`,
  rechts `x / y EP`.
- **Goldkarte** rechts der Skillleiste: Label `GOLD`, Betrag Cinzel 22 in
  #FACC47, darunter Punkte-Hinweis. Daneben 2×2 **Menütasten** (I Tasche,
  C Figur, M Karte, Esc Pause) als echte Buttons ≥ 44 px.

## Oberer Block

- **Zielbalken** oben Mitte: Name Cinzel 22 px in Tier-Farbe, Plakette rechts
  daneben (`Elite`/`Boss`/`Aktboss`, nicht bei Normal), Balken 600×18 mit
  Prozentzahl mittig, Statuszeile darunter (verschlammt/vergiftet, sonst leer).
  Bossbalken im selben Stil.
- **Minimap** oben rechts: runder Rahmen (Kreismaske, Bronzering + Goldlinie),
  Größe 220 px, Zonenname in Cinzel versal darunter. Implementierung
  `Map/MinimapHud.cs` (28 m, kameragleich gedreht, Randpfeile).
- **Fuhrbuch-Panel** 300 px breit unter der Minimap: Kopf „FUHRBUCH“ +
  Tastenhinweis J, Inhalt bis 4 Aufgaben mit Zielen, Zählern und Abgabe-NPC.

## Gruppenrahmen (oben links)

- Eigener Rahmen: Portrait-Kreis in Klassenfarbe mit Klassenportrait, Name,
  Klasse, `St. N`, Lebensbalken + Schildanteil. Darunter je Mitspieler ein
  kleiner Rahmen (Name, Klasse · Stufe, Lebensbalken aus repliziertem
  `hpState`), max. 4, Tote gedimmt. Solo nur eigener Rahmen. Refresh 10 Hz,
  keine Stencil-Masken. Replikation: `heroLevel`-NetworkVariable
  (`DiabloNetworkPlayer.cs`).
