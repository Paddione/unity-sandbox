# Cinderreach – Questinventar

Quelle für Änderungen an Questregeln ist `StoryCatalog.All` in `sandbox/Assets/Scripts/Story/StoryModel.cs`. Titel, Orte im Auftragstext und Zieltexte stammen aus `LocCatalog.Story.cs`. Nach Änderungen `python3 scripts/quest_inventory.py` ausführen.

Bei den Aktbossen kommt die Freigabe durch `ActBossGate.RequiredQuests` hinzu. Die Karte bestimmt konkrete NPC-, Fund- und Gegnerpositionen aus der laufenden Szene.

## Akt 1

### H1 · Die dunkle Tür

Hauptquest · Questgeber Ottilie · Annahme bei Ottilie

- Voraussetzungen: keine
- Questbelohnung: keine
- Ziele und Schauplätze:
  - Den Fuhrbrief am Totenkarren im Aschenmoor finden
  - Den Moorschläger am Karren erschlagen
- Abschluss: Abgabe bei Ottilie
- Nachträglicher Abschluss über KilnOpen

### H2 · Die Siegel vom Stillen Grund

Hauptquest · Questgeber Ottilie · Annahme bei Ottilie

- Voraussetzungen: H1 mindestens Done
- Questbelohnung: keine
- Ziele und Schauplätze:
  - Das Versunkene Beinhaus betreten
  - Knochenwächter zerschlagen (×4)
  - Den Hohlen Abt bezwingen
- Abschluss: Abgabe bei Ottilie
- Nachträglicher Abschluss über KilnOpen

### H3 · Lest die Namen

Hauptquest · Questgeber Ottilie · Annahme bei Ottilie

- Voraussetzungen: H2 mindestens Done
- Questbelohnung: keine
- Ziele und Schauplätze:
  - Mit Jorin Hagedorn an der Schmiede sprechen
  - Den Glutofen betreten
- Abschluss: automatisch nach den Zielen
- Nachträglicher Abschluss über ActOneComplete

### H4 · Asche schuldet niemand

Hauptquest · Questgeber Ottilie · Annahme automatisch

- Voraussetzungen: H3 mindestens Done
- Questbelohnung: keine
- Ziele und Schauplätze:
  - Die Ofenmutter bezwingen
- Abschluss: Abgabe bei Ottilie
- Nachträglicher Abschluss über ActOneComplete

### H5 · Siebenundvierzig

Hauptquest · Questgeber Ottilie · Annahme bei Ottilie

- Voraussetzungen: H4 mindestens Done
- Questbelohnung: keine
- Ziele und Schauplätze:
  - Mit Ilsabe Krumm an ihrem Stand sprechen
- Abschluss: Abgabe bei Ottilie

### N1 · Ilsabes Lieferbuch

Nebenquest · Questgeber Ilsabe · Annahme bei Ilsabe

- Voraussetzungen: H1 mindestens Active
- Questbelohnung: 150 Gold
- Ziele und Schauplätze:
  - Ilsabes Lieferbuch bei der Kiste im Aschenmoor bergen
- Abschluss: Abgabe bei Ilsabe

### N2 · Die stillen Brüder

Nebenquest · Questgeber Ottilie · Annahme bei Ottilie

- Voraussetzungen: H2 mindestens Active
- Questbelohnung: RareHelm
- Ziele und Schauplätze:
  - Grabtafeln im Beinhaus lesen (×3)
- Abschluss: Abgabe bei Ottilie

### N3 · Hagedorns Spitzen

Nebenquest · Questgeber Jorin · Annahme bei Jorin

- Voraussetzungen: KilnOpen
- Questbelohnung: SmithKit, 50 Gold
- Ziele und Schauplätze:
  - Aschenlanzer im Glutofen erschlagen (×6)
- Abschluss: Abgabe bei Jorin

### H14 · Der zweite Karren

Hauptquest · Questgeber Ottilie · Annahme bei Ottilie

- Voraussetzungen: H1 mindestens Active
- Questbelohnung: keine
- Ziele und Schauplätze:
  - Fuhrfetzen am zweiten Karren im Aschenmoor finden (×2)
  - Den Moorschläger am zweiten Karren erschlagen
- Abschluss: Abgabe bei Ottilie
- Nachträglicher Abschluss über KilnOpen

### H15 · Die siebte Tafel

Hauptquest · Questgeber Ottilie · Annahme bei Ottilie

- Voraussetzungen: H2 mindestens Active
- Questbelohnung: keine
- Ziele und Schauplätze:
  - Tafelbruchstücke im Beinhaus bergen (×3)
  - Mit Ottilie Wachs sprechen
- Abschluss: automatisch nach den Zielen
- Nachträglicher Abschluss über KilnOpen

### H16 · Jorins Liste

Hauptquest · Questgeber Jorin · Annahme bei Jorin

- Voraussetzungen: H3 mindestens Active, KilnOpen
- Questbelohnung: keine
- Ziele und Schauplätze:
  - Die Fuhrliste im Glutofen finden
  - Aschenlanzer im Glutofen erschlagen (×2)
- Abschluss: Abgabe bei Jorin
- Nachträglicher Abschluss über ActOneComplete

### H17 · Das zweite Siegel

Hauptquest · Questgeber Ottilie · Annahme bei Ottilie

- Voraussetzungen: H4 mindestens Done
- Questbelohnung: keine
- Ziele und Schauplätze:
  - Mit Ilsabe Krumm sprechen
  - Mit Ottilie Wachs sprechen
- Abschluss: automatisch nach den Zielen

### N8 · Vogts Bote

Nebenquest · Questgeber Ottilie · Annahme bei Ottilie

- Voraussetzungen: H1 mindestens Active
- Questbelohnung: 100 Gold
- Ziele und Schauplätze:
  - Das Siegelband am zweiten Karren bergen
- Abschluss: Abgabe bei Ottilie

### N9 · Thewes' Licht

Nebenquest · Questgeber Ottilie · Annahme bei Ottilie

- Voraussetzungen: H2 mindestens Active
- Questbelohnung: 60 Gold
- Ziele und Schauplätze:
  - Thewes' Grablicht im Beinhaus bergen
- Abschluss: Abgabe bei Ottilie

### N10 · Schlackeglut

Nebenquest · Questgeber Jorin · Annahme bei Jorin

- Voraussetzungen: KilnOpen
- Questbelohnung: 80 Gold
- Ziele und Schauplätze:
  - Schlackenknechte im Glutofen erschlagen (×4)
- Abschluss: Abgabe bei Jorin

### N11 · Ilsabes Preis

Nebenquest · Questgeber Ilsabe · Annahme bei Ilsabe

- Voraussetzungen: H3 mindestens Active, KilnOpen
- Questbelohnung: 120 Gold
- Ziele und Schauplätze:
  - Die lose Rußseite im Glutofen bergen
- Abschluss: Abgabe bei Ilsabe

## Akt 2

### H6 · Die letzte Fuhre

Hauptquest · Questgeber Merle · Annahme bei Merle

- Voraussetzungen: ActOneComplete
- Questbelohnung: keine
- Ziele und Schauplätze:
  - Die Glasbrache betreten
- Abschluss: automatisch nach den Zielen
- Nachträglicher Abschluss über ZoneBossDefeated

### H7 · Scherben lesen

Hauptquest · Questgeber Merle · Annahme automatisch

- Voraussetzungen: H6 mindestens Done
- Questbelohnung: keine
- Ziele und Schauplätze:
  - Dünenseher erschlagen (×3)
- Abschluss: automatisch nach den Zielen
- Nachträglicher Abschluss über ZoneBossDefeated

### H8 · Nicht loslassen

Hauptquest · Questgeber Merle · Annahme automatisch

- Voraussetzungen: H7 mindestens Done
- Questbelohnung: keine
- Ziele und Schauplätze:
  - Den Glasmacher bezwingen
- Abschluss: automatisch nach den Zielen
- Nachträglicher Abschluss über ZoneBossDefeated

### H9 · Wessen Licht

Hauptquest · Questgeber Ottilie · Annahme automatisch

- Voraussetzungen: H8 mindestens Done
- Questbelohnung: keine
- Ziele und Schauplätze:
  - Mit Merle Sandkamp sprechen
  - Mit Ottilie Wachs sprechen
- Abschluss: automatisch nach den Zielen

### N4 · Tamm

Nebenquest · Questgeber Merle · Annahme bei Merle

- Voraussetzungen: H6 mindestens Active
- Questbelohnung: 250 Gold
- Ziele und Schauplätze:
  - Einen Glasvogt in der Glasbrache bezwingen
- Abschluss: Abgabe bei Merle

### N5 · Was die Seher sahen

Nebenquest · Questgeber Ilsabe · Annahme bei Ilsabe

- Voraussetzungen: H5 mindestens Done, ActOneComplete
- Questbelohnung: SeerCirclet
- Ziele und Schauplätze:
  - Dünenseher in der Glasbrache erschlagen (×4)
- Abschluss: Abgabe bei Ilsabe

### H18 · Was im Glas liegt

Hauptquest · Questgeber Merle · Annahme automatisch

- Voraussetzungen: H7 mindestens Active
- Questbelohnung: keine
- Ziele und Schauplätze:
  - Sehgläser der Dünenseher bergen (×2)
  - Mit Merle sprechen
- Abschluss: automatisch nach den Zielen
- Nachträglicher Abschluss über ZoneBossDefeated

### H19 · Die Ordnung der Scherben

Hauptquest · Questgeber Merle · Annahme automatisch

- Voraussetzungen: H8 mindestens Done
- Questbelohnung: keine
- Ziele und Schauplätze:
  - Glasscherben an den Glassäulen bergen (×3)
- Abschluss: automatisch nach den Zielen
- Nachträglicher Abschluss über ZoneBossDefeated

### N12 · Tamms Rad

Nebenquest · Questgeber Merle · Annahme bei Merle

- Voraussetzungen: N4 mindestens Done
- Questbelohnung: 150 Gold
- Ziele und Schauplätze:
  - Das Speichenrad in der Glasbrache bergen
  - Zu Merle bringen
- Abschluss: Abgabe bei Merle

### N13 · Das fünfte Glas

Nebenquest · Questgeber Ilsabe · Annahme bei Ilsabe

- Voraussetzungen: N5 mindestens Done
- Questbelohnung: 200 Gold
- Ziele und Schauplätze:
  - Das fünfte Sehglas bergen
- Abschluss: Abgabe bei Ilsabe

### N14 · Tamms Scherbe

Nebenquest · Questgeber Merle · Annahme bei Merle

- Voraussetzungen: H18 mindestens Active
- Questbelohnung: 100 Gold
- Ziele und Schauplätze:
  - Tamms Scherbe an den Glassäulen bergen
- Abschluss: Abgabe bei Merle

### N15 · Die nächste Spende

Nebenquest · Questgeber Ilsabe · Annahme bei Ilsabe

- Voraussetzungen: N5 mindestens Done
- Questbelohnung: 150 Gold
- Ziele und Schauplätze:
  - Die Spendenseite in der Glasbrache bergen
- Abschluss: Abgabe bei Ilsabe

## Akt 3

### H10 · Die letzte Nacht in der Wacht

Hauptquest · Questgeber Ottilie · Annahme bei Ottilie

- Voraussetzungen: H9 mindestens Done
- Questbelohnung: keine
- Ziele und Schauplätze:
  - Mit Ottilie sprechen
  - Mit Merle sprechen
  - Mit Ilsabe sprechen
  - Mit Jorin sprechen
- Abschluss: Abgabe bei Ottilie

### N6 · Jorins Einschmelzen

Nebenquest · Questgeber Jorin · Annahme bei Jorin

- Voraussetzungen: H10 mindestens Active
- Questbelohnung: SmithKit
- Ziele und Schauplätze:
  - Lanzenspitzen an der Schmiede abgeben (×4)
- Abschluss: Abgabe bei Jorin

### N7 · Ilsabes letztes Angebot

Nebenquest · Questgeber Ilsabe · Annahme bei Ilsabe

- Voraussetzungen: H10 mindestens Active
- Questbelohnung: 200 Gold
- Ziele und Schauplätze:
  - Die letzte Spendenseite zu Ilsabe bringen
- Abschluss: Abgabe bei Ilsabe

### H11 · Glut am Moortor

Hauptquest · Questgeber Merle · Annahme automatisch

- Voraussetzungen: H10 mindestens Done
- Questbelohnung: keine
- Ziele und Schauplätze:
  - Drei Glutspuren am Moortor deuten (×3)
- Abschluss: automatisch nach den Zielen

### H12 · Die Heimkehr

Hauptquest · Questgeber Ottilie · Annahme automatisch

- Voraussetzungen: H11 mindestens Done
- Questbelohnung: keine
- Ziele und Schauplätze:
  - Thewes, den Heimkehrer, am Moortor bezwingen
- Abschluss: automatisch nach den Zielen

### H13 · Zerschlagen oder halten

Hauptquest · Questgeber Ottilie · Annahme automatisch

- Voraussetzungen: H12 mindestens Done
- Questbelohnung: keine
- Ziele und Schauplätze:
  - In die Wacht zurückkehren und entscheiden
- Abschluss: automatisch nach den Zielen
