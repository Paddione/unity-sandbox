# Cinderreach – Story-Bibel

Stand: 2026-11-23 (Goal `cinderreach-deutsch-story`, S3; Folgezeilen, Zurufe und Sprachausgabe aus `cinderreach-story-voice`). Diese Datei ist die SSOT für Welt, Figuren,
Aufgaben und Begriffe. Spieltexte in `Localization/` und die Questdaten in `Story/` folgen ihr.
Änderungen an Namen erst hier, dann im Code.

## Prämisse in drei Sätzen

In Cinderreach trägt jeder Mensch eine **Glut** in sich, die beim Tod noch einige Tage im Körper bleibt.
Seit drei Generationen brennt der **Glutofen** die Toten der Gegend, und aus ihrer Asche schmilzt der
**Glasmacher** in der **Glasbrache** das **Glutglas**, das in den Laternen der **Laternenwacht** ohne Öl
leuchtet und den kalten Aschennebel des Moors fernhält. Niemand in der Wacht fragt, wessen Licht über
seiner Tür brennt, und genau davon erzählt das Spiel.

## Die Kette

Das Licht der Wacht entsteht in vier Gliedern. Jedes Glied ist eine Zone, jeder Boss hütet eines davon.

| Glied | Ort | Hüter | Was dort geschieht |
|---|---|---|---|
| Die Toten | Versunkenes Beinhaus | Der Hohle Abt | Die Brüder vom Stillen Grund waschen und verzeichnen die Toten und geben sie dem Ofen. |
| Das Feuer | Glutofen | Die Ofenmutter | Die Brennmeisterin brennt die Toten. Die Glut geht in die Asche über. |
| Das Glas | Glasbrache | Der Glasmacher | Asche und Sand werden zu Glutglas geschmolzen. |
| Das Licht | Laternenwacht | Ottilie Wachs | Die Lampenwärterin setzt das Glas in die Laternen. |

Historischer Kern: Glasmacher brauchten früher Pottasche aus verbrannten Wäldern. In Cinderreach ist
das Holz ausgegangen.

## Die Wahrheit hinter der Kette

Die Spieler entdecken sie in drei Schichten, je eine pro Wendepunkt.

1. **Der Ofen holt mehr, als stirbt.** (Wendung Akt I, Beinhaus.) Der Abt hat die Toten nicht aus Wahn
   zurückgehalten. Die Fuhrlisten des Ofens forderten Namen von Menschen, die noch lebten.
2. **Die Wacht hat sie geliefert.** (Wendung Akt I, Glutofen.) Das Rußbuch der Ofenmutter verzeichnet
   „Spenden“ aus der Laternenwacht: Kranke, Alte, Schuldner. Gegen Gold und Glas vermittelt hat sie
   Ilsabe Krumm, mit dem Siegel des Vogts.
3. **Die Hüllen wollen ihre Glut zurück.** (Wendung Akt II, Glasbrache.) Glutglas bewahrt die Glut nicht
   als Wärme, es hält sie fest. Der Körper, dem sie gehört, steht ohne sie wieder auf und wird zur Hülle.
   Die Hüllen im Moor kommen nicht trotz der Laternen. Sie kommen wegen der Laternen.

Damit kippt die Ausgangsfrage. Die Wacht dachte, ihr Licht schütze sie vor dem Moor. In Wahrheit ruft ihr
Licht das Moor herbei.

## Ton und Sprachregeln

- Düster, knapp, konkret. Kurze Sätze, Dinge statt Abstrakta: Karren, Ruß, Siegel, Docht, Scherbe.
- Niemand hält Monologe über das Böse. Figuren reden über Arbeit, Schulden, Wetter, Tote mit Namen.
- Keine Auserwählten, keine Prophezeiung, kein erwachendes Urböses. Alle Schuld ist menschengemacht und
  hat eine Rechnung.
- Ironie nur trocken und nur bei Ilsabe und dem Schmuggler.
- Anrede: Die Figuren duzen die Gruppe („ihr“). Der Vogt siezt schriftlich.
- UI-Texte sachlich und kurz, Imperativ ohne Ausrufezeichen („Truhe öffnen“, nicht „Öffne die Truhe!“).
- Zahlen mit deutschem Dezimalkomma („1,5 s“), Prozent mit Leerzeichen („25 %“).

### Ton-Fortschreibung (Glättungsregeln, `cinderreach-sprache-voice` S1)

Verständlich-modern heißt: nur verstaubte Wendungen ersetzen, alles andere bleibt. Glossar und Ton oben
sind bindend; diese Regeln sagen, wie weit eine Glättung gehen darf (S2 wendet sie Zeile für Zeile an).

1. **Nur Staub wischen.** Ersetzt wird, was ein heutiger Leser zweimal lesen muss: verdrehte Satzstellung,
   Kanzleipartizipien („die vom Ofen geforderten, noch lebenden Namen“), gestelzte Füllwörter („gar wohl“,
   „allhier“, „denn“ als Füllsel). Sinn, Satzlänge und Wortstellung bleiben sonst unangetastet.
2. **Glossar ist Gesetz.** Orte, Gegner, Klassen, Systeme und Story-Begriffe aus dem Glossar werden nie
   umschrieben oder „geglättet“ (kein „Brennofen“ für Glutofen, kein „Zombie“ für Hülle).
3. **Kurz vor schön.** Düster, knapp, konkret: ein Satz, ein Bild, ein Gegenstand. Abstrakta („die Last der
   Schuld“, „das Dunkel der Verzweiflung“) werden zu Dingen (Karren, Ruß, Siegel, Docht, Scherbe) oder
   gestrichen. Keine Monologe über das Böse; Figuren reden über Arbeit, Schulden, Wetter, Tote mit Namen.
4. **Stimmen bleiben Stimmen.** Ottilie zählt, Ilsabe rechnet, Jorin schweigt in kurzen Sätzen, Merle ist
   stumpf, der Erzähler liest Chronik. Ironie nur trocken und nur bei Ilsabe und dem Schmuggler.
5. **Anrede fest.** Die Figuren duzen die Gruppe („ihr“), der Vogt siezt schriftlich. Daran ändert keine
   Glättung etwas.
6. **UI und Zahlen fest.** UI-Texte sachlich, Imperativ ohne Ausrufezeichen. Deutsches Dezimalkomma,
   Prozent mit Leerzeichen.
7. **Kein Slang, keine Jetztzeit.** Verständlich heißt zeitlos-klar, nicht heutig: keine Anglizismen, keine
   Jugendsprache, keine Ironie-Anachronismen („down“, „Grind“, „okay“, „Deal“). Was 1990 und 2026 gleich
   klingt, ist richtig.
8. **Englisch folgt.** Die `en`-Fallbacks bleiben sinngetreu; Glättung ändert sie nur mit (S2-Notiz, kein
   eigener Umbau).

Beispiele (Illustration, keine Zeilenänderung):

| Regel | Vorher (verstaubt/abstrakt) | Nachher (geglättet) |
|---|---|---|
| 1 | „Höret, die Fuhre harret euer“ | „Die Fuhre wartet. Kommt.“ |
| 3 | „Die Dunkelheit der Verzweiflung senkt sich über die Wacht“ | „Sechsundvierzig. Fragt mich morgen wieder.“ |
| 4 | „Ich verkaufe nichts, was nicht vorher jemand hergegeben hat, versteht ihr, das ist nun einmal der ganze Handel“ | „Ich verkaufe nichts, was nicht vorher jemand hergegeben hat. Das ist der ganze Handel.“ |
| 7 (verboten) | „Der Ofen ist down, wir brauchen mehr Grind“ | Unzulässig — bleibt: „Der Ofen ist kalt. Ohne Tote brennt er nicht.“ |

## Die Gruppe

Die Wacht schickt keine Helden, sie schickt eine **Fuhre**: drei Leute, die man entbehren kann und die
sich auskennen. Die Rollen ergeben sich aus dem Weg.

| Klasse (intern) | Anzeigename | Rolle | Warum gerade sie |
|---|---|---|---|
| Fenwalker | **Moorgänger** | Tank | Führt seit Jahren die Totenkarren durch den Nebel, trägt den lehmverschmierten Mooreichenschild, den der Nebel nicht durchdringt. Geht vorn, weil vorn der Weg ist. |
| Lanternbearer | **Laternenträgerin** | Heilerin | Ottilies Gehilfin. Ihre Laterne heilt, weil das Glas Glut abgibt. Sie weiß nicht, woher die Glut kommt. Ab Akt II heilt sie mit dem Licht eines Menschen. |
| Rogue | **Schmuggler** | Schaden | Hat Asche am Ofen vorbei in die Glasbrache verschoben, um am Glas mitzuverdienen. Kennt Hintertüren und Preise. Ist der Einzige, der von Anfang an ahnt, dass die Mengen nicht stimmen. |

Die Klassennamen bleiben im Grammatikgeschlecht fest (Moorgänger, Laternenträgerin, Schmuggler). Die
Laternenträgerin ist bewusst weiblich: Sie ist Ottilies Gehilfin und spricht in Akt II aus eigener Schuld.

## Figuren

### Ottilie Wachs – Lampenwärterin (neu, Laternenwacht)

Früher Novizin im Beinhaus, vor elf Jahren in die Wacht gegangen, weil sie „lieber Licht pflegt als
Leichen“. Zählt jeden Abend die Laternen und kennt jede beim Namen der Familie, über deren Tür sie hängt.
Fromm aus Gewohnheit, praktisch aus Not. Ihr Widerspruch: Sie hat das Beinhaus verlassen, weil sie dem Abt
nicht mehr glaubte, und schickt nun Fremde, um ihn zu töten. Am Ende von Akt I löscht sie die erste
Laterne mit eigener Hand.

Stimme: „Siebenundvierzig Laternen. Gestern achtundvierzig. Ich weiß, wessen Tür jetzt dunkel ist.“

### Ilsabe Krumm – Händlerin (bestehend, Laternenwacht)

Freundlich, schnell im Kopf, nie verlegen. Spricht in Verwaltungswörtern: „Abgabe“, „Spender“, „Ausgleich“.
Hat die Lieferungen an den Ofen vermittelt, nennt das „Fürsorge für die, die niemanden mehr haben“, und
glaubt das zur Hälfte. Bleibt bis zum Schluss ansprechbar und handelt weiter, auch nachdem die Gruppe alles
weiß. Gerade das soll unbequem sein.

Stimme: „Ich verkaufe nichts, was nicht vorher jemand hergegeben hat. Das ist der ganze Handel.“

### Jorin Hagedorn – Schmied (bestehend, Laternenwacht)

War Lehrling der Ofenmutter und hat die Lanzen der Aschenlanzer geschmiedet. Ist gegangen, als die ersten
Schlackenknechte noch atmeten. Wortkarg. Nennt sie nie „Ofenmutter“, nur „die Meisterin“. Bittet nicht um
Vergebung, sondern darum, seine Arbeit zurückzubekommen.

Stimme: „Jede dieser Spitzen hab ich gehärtet. Bringt sie mir. Ich will sie einschmelzen, bevor sie noch
wen findet.“

### Merle Sandkamp – Karrnerin (neu, ab Akt II in der Laternenwacht)

Fuhr Asche vom Ofen in die Glasbrache. Als die Feuer erloschen, kam sie mit einem leeren Karren und einem
Sack voll ungeschliffenem Glutglas in die Wacht, um es zu verkaufen. Ihr Bruder Tamm ist in der Brache
geblieben und wurde zum Scherbenrücken. Sie will ihn nicht retten. Sie will, dass er aufhört.

Stimme: „In der Brache wächst einem das Glas auf der Brust, wenn man zu lange in eine Richtung schaut.
Tamm hat immer nach Hause geschaut.“

### Vogt Anselm Dreher – Auftragsbrett (nur schriftlich)

Tritt nie auf. Seine Anschläge am Auftragsbrett sind höflich, gesiezt und sauber gesiegelt. Ab dem Ende von
Akt I liegt dasselbe Siegel im Rußbuch. Das Brett bleibt danach in Betrieb.

### Die Bosse

**Der Hohle Abt** (Versunkenes Beinhaus). Oberster der Brüder vom Stillen Grund. Fand in den Fuhrlisten
Namen von Lebenden und hörte auf, Tote herauszugeben. Als die Wacht Plünderer schickte, gab er seine
eigene Glut, um die Gebeine seiner Brüder als Knochenwächter aufzustellen. Seitdem ist er hohl: Er spricht
noch, aber er erinnert sich nur an Listen. Kein Wahnsinniger, sondern ein Verwalter, der zu spät Nein
gesagt hat. In Phase 2 ruft er Hüllen, weil er die Toten, die er schützen wollte, nur noch als Waffe kennt.
Letzte Worte: „Lest die Namen. Lest sie laut.“

**Die Ofenmutter** (Glutofen). Brennmeisterin in dritter Generation. Als das Beinhaus keine Toten mehr
schickte, ließ sie die Feuer nicht ausgehen, weil sie wusste, was in der Wacht passiert, wenn die Laternen
erlöschen. Sie nahm, was man ihr brachte, und als das nicht reichte, ihre eigenen Knechte, die seitdem
glühen und bersten. Sie hält sich nicht für grausam, sondern für die Einzige, die rechnen kann.
Satz zur Gruppe: „Ich habe niemanden geholt. Sie wurden mir gebracht. Von euch.“

**Der Glasmacher** (Glasbrache). Erfand das Glutglas aus Trauer: Er wollte seine Frau Wendeline nicht
verlieren und schmolz ihre Asche in ein Prisma, das noch warm war, als er es in den Händen hielt. Er weiß
seit vierzig Jahren, dass die Glut im Glas nicht ruht, und hält das für Gnade: Niemand im Glas wird
vergessen. Sein Prisma ist der Helm „Prisma des Glasmachers“. Wer es trägt, trägt Wendeline.
Satz zur Gruppe: „Ihr nennt es Gefangenschaft. Ich nenne es: nicht loslassen.“

## Glossar (verbindlich)

Interne Bezeichner bleiben englisch. Diese Tabelle regelt ausschließlich Anzeigenamen.

### Orte

| Intern / alt | Deutsch |
|---|---|
| Cinderreach | Cinderreach (Produkt- und Landschaftsname, unverändert) |
| Lantern Hold | Laternenwacht |
| Ashen Fen | Aschenmoor |
| Fen Gate | Moortor |
| Sunken Ossuary | Versunkenes Beinhaus |
| Cinder Kiln | Glutofen |
| Glass Barrens | Glasbrache |
| Waypoint / Shrine | Wegschrein |
| Risk Shrine | Wagnisschrein |

### Gegner

| Intern / alt | Deutsch |
|---|---|
| Husk | Hülle |
| Fen Brute | Moorschläger |
| Ash Elite | Aschenhüne |
| Bone Warden | Knochenwächter |
| The Hollow Abbot | Der Hohle Abt |
| Cinder Thrall | Schlackenknecht |
| Ash Lancer | Aschenlanzer |
| Kiln Elite | Ofenschinder |
| Kiln Warden | Ofenwächter |
| Bone Warden Elite | Ältester vom Stillen Grund |
| Shrine Elite | Schreinhüter |
| The Kilnmother | Die Ofenmutter |
| Shardback | Scherbenrücken |
| Dune Augur | Dünenseher |
| Glass Elite | Glasvogt |
| Glass Pillar | Glassäule |
| The Glasswright | Der Glasmacher |

### Klassen und Fähigkeiten

| Intern / alt | Deutsch |
|---|---|
| Fenwalker | Moorgänger |
| Lanternbearer | Laternenträgerin |
| Rogue | Schmuggler |
| Tank / Healer / DPS | Schild / Heilung / Schaden |
| Fireball | Feuerball |
| Bow / Arrow | Bogen |
| Cleave | Spalten |
| Seismic Slam | Erdstoß |
| Lantern Bolt | Laternenblitz |
| Wisps | Irrlichter |
| Searing Halo | Sengender Kranz |
| Veil Step | Schleierschritt |
| Mending Light | Heilendes Licht |
| Renewing Circle | Erneuernder Kreis |
| Quick Shot | Schnellschuss |
| Blade Flurry | Klingenwirbel |
| Mire Hook / Bog Charge / Mire Ground / Iron Vow | Moorhaken / Moorsturm / Sumpfgrund / Eisenschwur |
| Smoke Pot / Shadow Roll / Venom Vial / Powder Keg | Rauchtopf / Schattenrolle / Giftphiole / Pulverfass |
| Mired / Blinded / Venom | verschlammt / geblendet / Gift |
| Shatter / Fen Shackle / Retribution | Zerschmettern / Moorfessel / Vergeltung |
| Venom Burn / Ambush / Throat Cut / Spark Shot | Giftbrand / Hinterhalt / Kehlenschnitt / Zündschuss |
| Strike / Thrust / Block | Schlag / Stoß / Block |

### Spielsysteme

| Intern / alt | Deutsch |
|---|---|
| Normal / Kindled | Normal / Entfacht |
| Magic / Rare / Unique | Magisch / Selten / Einzigartig |
| Item level | Gegenstandsstufe |
| Affix | Eigenschaft |
| Vendor | Händlerin |
| Smith / Salvage / Reforge | Schmiede / Verwerten / Umschmieden |
| Stash | Truhe |
| Contract Board / Hunt / Recovery | Auftragsbrett / Jagd / Bergung |
| Quest log | Fuhrbuch |
| Main task / Side task | Hauptaufgabe / Nebenaufgabe |
| Gold | Gold |
| Health potion | Heiltrank |
| Strength / Vitality / Focus | Stärke / Vitalität / Fokus |
| Level / Skill point | Stufe / Fertigkeitspunkt |
| Act I / Act II | Akt I / Akt II |

### Einzigartige Gegenstände

| Intern / alt | Deutsch |
|---|---|
| Lantern of the Hold | Laterne der Wacht |
| lancer-ward | Lanzenbann |
| wick-of-the-halo | Docht des Kranzes |
| Shardback Aegis | Scherbenpanzer |
| Glasswright Prism | Prisma des Glasmachers |
| Emberbrand Blade | Glutmalklinge |
| Cryptwarden Plate | Panzer vom Stillen Grund |
| Seer's Circlet | Reif des Sehers |
| nightfang-edge | Nachtzahn |
| Rogue's Keepsake | Andenken des Schmugglers |
| bulwark-carrier | Karrenbollwerk |
| seismic-crown | Erdstoßkrone |
| mercy-shroud | Gnadentuch |
| circle-of-dawn | Reif der Frühe |
| shadowstride-vest | Schattenschrittweste |
| keen-cadence | Scharfer Takt |
| moorvogt-anchor | Anker des Moorvogts |
| smugglers-fuse | Schmugglerlunte |

Weitere Gegenstände (Basistypen, Affixe, Werte) stehen in `LocCatalog.Item.cs`, `LocCatalog.Affix.cs` und `LocCatalog.Stat.cs`.

### Story-Begriffe

| Begriff | Bedeutung |
|---|---|
| Glut | Lebensfunke jedes Menschen, bleibt nach dem Tod einige Tage im Körper |
| Glutglas | Glas aus Totenasche, leuchtet ohne Öl |
| Aschennebel | Kalter Nebel des Moors, in dem Hüllen wandern |
| Fuhre | Die Gruppe der Spieler; ursprünglich ein Totentransport |
| Fuhrliste | Liste der Toten, die das Beinhaus dem Ofen schuldet |
| Rußbuch | Brennbuch der Ofenmutter mit allen Namen und Spendern |
| Brüder vom Stillen Grund | Orden des Beinhauses |
| Spende | Ilsabes Wort für einen Menschen, den die Wacht an den Ofen gab |

## Akt I – „Wer das Licht bezahlt“

Ausgangslage: Seit vierzig Tagen kommt kein Totenkarren mehr aus dem Beinhaus. Der Ofen hat nichts zu
brennen, die Brache kein Glas, und in der Laternenwacht ist die erste Laterne erloschen.

### Hauptaufgaben

| Nr | Titel | Auftraggeber | Ziele (Ereignis im Spiel) | Abschluss |
|---|---|---|---|---|
| H1 | Die dunkle Tür | Ottilie | Mit Ottilie sprechen. Den umgestürzten Totenkarren im Aschenmoor finden (Fundstück „Fuhrbrief“ nahe der Kiste). Den Moorschläger erschlagen, der ihn bewacht. | Fuhrbrief bei Ottilie abgeben. Sie liest: Der Karren war leer. |
| H2 | Die Siegel vom Stillen Grund | Ottilie | Das Versunkene Beinhaus betreten. Knochenwächter zerschlagen (4). Den Hohlen Abt bezwingen. | Das Brevier des Abts wird beim Kill gefunden. |
| H3 | Lest die Namen | Ottilie | Das Brevier zu Ottilie bringen. Mit Jorin über den Ofen sprechen. Den Glutofen betreten. | Ottilie erkennt in den Fuhrlisten Namen aus der Wacht, die noch lebten. Wendung 1. |
| H4 | Asche schuldet niemand | Ottilie | Die Ofenmutter bezwingen. Das Rußbuch aufheben (fällt beim Kill). | Rußbuch zu Ottilie bringen. Wendung 2: Ilsabes Hand, Vogts Siegel. |
| H5 | Siebenundvierzig | Ottilie | In die Laternenwacht zurückkehren und mit Ottilie sprechen. | Ottilie löscht die Laterne über Ilsabes Tür. Akt I abgeschlossen. |
| H14 | Der zweite Karren | Ottilie | Zwei Fuhrfetzen am zweiten Karren im Aschenmoor finden (Rätsel R1). Den Moorschläger erschlagen, der ihn bewacht. | Fetzen bei Ottilie abgeben. Die Schrift ist jünger als der erste Fuhrbrief — und das Siegelband fehlt. |
| H15 | Die siebte Tafel | Ottilie | Drei Tafelbruchstücke im Versunkenen Beinhaus bergen (Rätsel R2). Mit Ottilie sprechen. | Die siebte Tafel nennt keinen Toten, sondern eine Schuld. Ottilie kennt den Namen. |
| H16 | Jorins Liste | Jorin | Die Fuhrliste im Glutofen finden. Zwei Aschenlanzer erschlagen, die sie bewachen. | Liste zu Jorin bringen. Er kennt die Namen darauf — es sind seine Lieferungen. |
| H17 | Das zweite Siegel | Ottilie | Mit Ilsabe sprechen. Mit Ottilie sprechen (Rätsel R3: wem sie glaubt). | Ottilie hält zwei Siegel nebeneinander: das des Vogts und Ilsabes Hand. Sie entscheidet. |

H2 begleitet das bestehende Tor: Der Abt-Kill öffnet weiterhin den Ofen. H4 begleitet den Kilnmother-Kill,
der weiterhin Akt I abschließt und die Brache öffnet. H5 ist rein erzählerisch und sperrt nichts.
H14–H17 sind interne Kennungen (das Fuhrbuch zeigt nur Titel); sie vertiefen je einen Bestandsschritt,
ohne die Anker zu verschieben.

### Nebenaufgaben

| Nr | Titel | Auftraggeber | Ziele | Belohnung | Was sie an der Hauptgeschichte ändert |
|---|---|---|---|---|---|
| N1 | Ilsabes Lieferbuch | Ilsabe | Den Moorvorrat im Aschenmoor bergen (die Kiste). Das Lieferbuch zu Ilsabe bringen. | 150 Gold | Das Lieferbuch zeigt Mengen, die nie ein Totenkarren gefahren hat. Wer N1 erledigt hat, erkennt in H4 Ilsabes Handschrift wieder; der Abschluss-Dialog von H4 hat eine eigene Zeile. |
| N2 | Die stillen Brüder | Ottilie | Im Beinhaus drei Grabtafeln lesen (Fundstücke an den Sarkophagen). | 1 Fertigkeitsrang-Gegenstand (Selten) | Die Knochenwächter haben Namen. Der Abt hat seine Brüder nicht missbraucht, sie haben ihm ihre Gebeine angeboten. Im H3-Angebot erinnert sich Ottilie an Thewes und an ihre eigene Rolle. Im Kampf ruft der Abt seine Brüder bei Namen. |
| N3 | Hagedorns Spitzen | Jorin | Sechs Aschenlanzer im Glutofen erschlagen. Zu Jorin zurückkehren. | Ein kostenloses Umschmieden | Jorin gesteht, dass er wusste, wofür die Lanzen gedacht waren: nicht gegen Plünderer, sondern gegen Knechte, die fliehen wollten. Im H4-Abschluss erzählt Ottilie, dass Jorin die Spitzen eingeschmolzen hat. |
| N8 | Vogts Bote | Ottilie | Das Siegelband am zweiten Karren im Aschenmoor bergen (gehört zu Rätsel R1). | 100 Gold | Das Band trägt das Siegel des Vogts. Wer N8 erledigt hat, hört im H2-Angebot Ottilies Satz dazu — der Vogt wusste vom zweiten Karren. |
| N9 | Thewes' Licht | Ottilie | Das Grablicht am Sarkophag des Thewes im Beinhaus bergen (gehört zu Rätsel R2). | 60 Gold | Thewes hat sein Licht nie gelöscht. Wer N9 erledigt hat, hört im H3-Angebot, dass Ottilie seinen Namen selbst auf die Liste gesetzt hat. |
| N10 | Schlackeglut | Jorin | Vier Schlackenknechte (Cinder Thralls) im Glutofen erschlagen. | 80 Gold | Die Knechte trugen noch Glut in den Händen — sie wollten nichts bewachen, sie wollten tragen. Wer N10 erledigt hat, hört im H4-Abschluss Ottilies Satz dazu: Die Wacht hat sie geschickt. |
| N11 | Ilsabes Preis | Ilsabe | Eine lose Rußseite im Glutofen bergen. | 120 Gold | Die Seite nennt einen Preis pro Name. Wer N11 erledigt hat, hört im H5-Angebot Ilsabes Satz dazu — bevor Ottilie die Laterne löscht. |

### Rätsel-Definitionen (Akt I)

Alle Rätsel nutzen Bestandssysteme: Fundstücke über `StoryPickup`/`IUsable` mit der Benutzen-Taste (T),
Gespräche über `StoryDialog` mit Folgezeilen-Mechanik (`x|nK`). Kein neues Interaktionssystem.

| Nr | Name | Typ | Ort (Quests) | Regel |
|---|---|---|---|---|
| R1 | Zusammengehörige Fetzen | Umgebung/Fundstück | Aschenmoor (H14 + N8) | Zwei Fuhrfetzen (H14) und das Siegelband (N8) gehören aneinander. Erst wenn alle drei Stücke geborgen sind, liest sich im Fuhrbuch der ganze Satz: Der zweite Karren fuhr nicht für die Toten. |
| R2 | Die siebte Tafel | Umgebung/Fundstück | Beinhaus (H15 + N9) | Drei Tafelbruchstücke (H15) ergeben erst mit Thewes' Grablicht (N9) eine lesbare Tafel: Sie nennt eine Schuld, keinen Toten. |
| R3 | Das zweite Siegel | Dialog/Entscheidung | Laternenwacht (H17, mit Folge in H5) | Zwei Gespräche (Ilsabe, dann Ottilie): Ilsabe erklärt das Vogtsiegel, Ottilie wägt ab. Die Entscheidung steht im H5-Abschluss — mit eigener Zeile je nachdem, ob N11 (Ilsabes Preis) bekannt ist. |

## Akt II – „Was im Glas wohnt“

Ausgangslage: Die Feuer im Ofen sind erloschen. In der Wacht brennen noch siebenundvierzig Laternen, ohne
Nachschub. Merle Sandkamp kommt mit einem leeren Karren aus der Glasbrache.

### Hauptaufgaben

| Nr | Titel | Auftraggeber | Ziele | Abschluss |
|---|---|---|---|---|
| H6 | Die letzte Fuhre | Merle | Mit Merle sprechen. Die Glasbrache betreten. | Merle erzählt vom Glasmacher und von Tamm. |
| H7 | Scherben lesen | Merle | Dünenseher erschlagen (3). Ihre Sehgläser zeigen die Laternenwacht von außen: Hüllen, die zum Licht ziehen. | Wendung 3 deutet sich an. |
| H8 | Nicht loslassen | Merle | Den Glasmacher bezwingen. Das Prisma des Glasmachers tragen oder aufheben. | Der Glasmacher spricht von Wendeline und der Glut im Glas. |
| H9 | Wessen Licht | Ottilie und Merle | In die Laternenwacht zurückkehren und mit beiden sprechen. | Ottilie hält eine Scherbe gegen ihre Laterne. Offenes Ende (siehe Ausblick). |

### Nebenaufgaben

| Nr | Titel | Auftraggeber | Ziele | Belohnung | Was sie an der Hauptgeschichte ändert |
|---|---|---|---|---|---|
| N4 | Tamm | Merle | Einen Glasvogt in der Glasbrache bezwingen. Seine Karrnermarke zu Merle bringen. | 250 Gold | Tamm war einer der Glasvögte. Merle legt die Marke nicht weg, sondern in ihren Sack mit dem Glutglas. In H9 sagt sie dann, dass sie das Glas nicht mehr verkauft. Ohne N4 verkauft sie es weiter („Irgendwer kauft immer Licht“). |
| N5 | Was die Seher sahen | Ilsabe | Zwei Sehgläser behalten statt sie Merle zu geben (vier Dünenseher erschlagen). Zu Ilsabe bringen. | 1 einzigartiger Gegenstand aus Ilsabes Bestand | Ilsabe will die Sehgläser, um zu wissen, wann die Hüllen kommen. Sie plant schon die nächste Spende. Die Wacht hat aus Akt I nichts gelernt. In H9 spricht Ottilie es aus: „Sie wird ihnen etwas anbieten. Jemanden.“ |

## Ausblick nach Akt II

Kein Akt III in diesem Goal. Das Ende bleibt offen und unbequem:

> Ottilie hält die Scherbe gegen ihre Laterne. Das Licht darin flackert im Takt eines Atems.
> „Wenn wir sie zerschlagen, ist es dunkel. Wenn nicht, kommen sie weiter. Sie wollen nur nach Hause.“
> Hinter dem Moortor stehen Hüllen im Nebel und sehen herüber. Keine kommt näher. Noch nicht.

Die spätere Entscheidung (Laternen brechen oder halten) ist der Haken für Akt III und wird hier nicht
aufgelöst.

## Folgezeilen der Nebenaufgaben

Dialogzeilen mit Sprecher `x|nK` erscheinen nur, wenn Nebenaufgabe `nK` erledigt ist, `x|!nK` nur, wenn nicht
(`StoryDialog.Visible`). So ändert jede Nebenaufgabe eine Zeile der Hauptkette: N1 → H5-Angebot, N2 → H3-Angebot,
N3 → H4-Abschluss, N4 → H9 Merle (zwei Varianten), N5 → H9 Ottilie,
N8 → H2-Angebot, N9 → H3-Angebot, N10 → H4-Abschluss, N11 → H5-Angebot.

## Zurufe

Kurze vertonte Zeilen außerhalb des Dialogfensters, als Untertitel über der Aktionsleiste (`StoryBarks`).

| Anlass | Sprecher | Zeile |
|---|---|---|
| Abt Phase 2 | Der Hohle Abt | „Bruder Benno. Bruder Kaspar. Bruder Thewes. Steht auf. Die Liste ist noch nicht zu Ende.“ |
| Abt besiegt | Der Hohle Abt | „Lest die Namen. Lest sie laut.“ |
| Ofenmutter Phase 2 | Die Ofenmutter | „Zwölf Körbe Asche für eine Laterne im Monat. Rechnet nach. Ich habe es jeden Tag getan.“ |
| Ofenmutter besiegt | Die Ofenmutter | „Ich habe niemanden geholt. Sie wurden mir gebracht. Von euch.“ |
| Glasmacher Phase 2 | Der Glasmacher | „Wendeline, hab keine Angst. Sie wollen dich nur ansehen. Ich halte dich.“ |
| Glasmacher besiegt | Der Glasmacher | „Ihr nennt es Gefangenschaft. Ich nenne es: nicht loslassen.“ |
| H4 beginnt (Glutofen betreten) | Erzähler | „Hinter dem Tor ist die Luft so heiß, dass der Ruß nicht fällt. Irgendwo zählt eine Stimme Körbe.“ |
| H7 erledigt (Wendung 3) | Erzähler | „Im letzten Sehglas liegt die Laternenwacht, von außen gesehen. Um ihre Lichter steht der Nebel voller Gestalten, alle mit dem Gesicht zur Tür.“ |

## Sprachausgabe

Alle Dialog- und Zurufzeilen sind deutsch vertont, Erzählzeilen von einer Erzählerstimme. Jede Figur hat eine
synthetische Stimme (keine reale Person), definiert in `docs/audio/voices.json`: Ottilie (leiser, müder Alt),
Ilsabe (heller, schneller Händlerinnenton), Jorin (rauer Bass, wortkarg), Merle (heiser, erschöpft), Erzähler
(ruhiger Bariton), Abt (hohl, flüsternd), Ofenmutter (hart, rauchig), Glasmacher (sanft, brüchig). Textänderung an
einer Zeile macht ihren Clip ungültig (`scripts/check-voice-lines.py`), neu rendern mit
`scripts/voice/render_voices.py`. Mit `-lang en` bleiben die Clips stumm.

## Zwischentexte

| Anlass | Titel | Text |
|---|---|---|
| Abt besiegt | DER STILLE GRUND SCHWEIGT | Der Abt fällt wie ein leerer Mantel. In seinem Brevier stehen Namen, und neben manchen steht kein Todestag. |
| Ofenmutter besiegt (Akt I) | AKT I ABGESCHLOSSEN | Zum ersten Mal seit drei Generationen kühlt der Ofen aus. Hinter ihm liegt die Glasbrache offen. |
| Glasmacher besiegt | DIE GLASBRACHE IST STILL | Das Prisma ist warm. Es wird nicht kälter. |
| Akt II beginnt (Merle erscheint) | AKT II – WAS IM GLAS WOHNT | Ein leerer Karren rollt durch das Moortor. Die Frau darauf hat seit Tagen niemanden gesehen, der noch glüht. |
