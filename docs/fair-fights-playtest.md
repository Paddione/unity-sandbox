# Spieltest — Faire Kämpfe in Akt I (manuell, Windows)

Goal-Cut K4: je ein Lauf mit Fenwalker und Lanternbearer, Normal, reguläre
Ausrüstung aus dem Spielverlauf (Fen → Ossuary natürlich durchspielen,
Level ca. 5–8, kein God-Mode, kein getweaktes Gear). Build per
`scripts/stage-windows.sh` stagen, auf Windows laufen lassen.

## Lauf A — Fenwalker

1. Ossuary: auf ca. 7 m an einen Bone Warden heran, stehen bleiben.
   - Erwartet: orangefarbene Kugel über dem Warden wächst ca. 0,6 s (Wind-up),
     dann fliegt der Feuerball los.
2. Einen Treffer nehmen: HP sinken um 2 (Normal, ohne Crit).
3. Nächsten Schuss: nach dem Abschuss seitlich ausweichen — kein Treffer.
4. Hinter Sarkophag/Säule stellen, Schuss abwarten — kein Treffer (Deckung).
5. Durch das Ossuary-Tor in den Fen wechseln: kein Warden folgt, kein
   weiterer Beschuss. Zurückkehren: Wardens stehen wieder auf Posten.
6. Nach dem Abbot durch das Kiln-Tor: 5 s stehen bleiben, orientieren —
   kein Angriff. Nach Norden vorrücken — erster Kampf beginnt.

## Lauf B — Lanternbearer

Gleiche Schritte 1–6 mit der zweiten Klasse (Wisps/Halo-Nutzung egal,
nur Wahrnehmung und Ausweichen zählen).

## Verdikt (bitte ausfüllen)

- Warden-Schuss rechtzeitig erkennbar (Fenwalker ja/nein, Lanternbearer ja/nein)?
- Kiln-Einstieg erlaubt bewusste Annäherung (ja/nein)?
- Auffälligkeiten (Schaden, Deckung, Rückzug)?

Automatisiert bereits belegt: EditMode- und PlayMode-Suiten
(`WardenLeashTests`, `WardenShotTests`, `KilnArrivalTests`,
`FairFightsConfigTests`) sowie der Akt-I-Autoplay unter G-DIABLO06.
