/// <summary>
/// Story dialog lines (cinderreach-deutsch-story S5/S6). German is the original,
/// English a faithful fallback. Line keys follow <see cref="StoryDialog.Key"/>;
/// voices and facts follow docs/story-bible.md.
/// </summary>
public static partial class LocCatalog
{
    static readonly Entry[] Dialog =
    {
        // ---- H1 Die dunkle Tür ----
        E("dlg.h1.offer.1", "Siebenundvierzig Laternen. Gestern waren es achtundvierzig. Die dunkle hängt über der Tür von Witwe Rösch. Sie hat es noch nicht gemerkt, sie schläft.", "Forty-seven lanterns. Yesterday there were forty-eight. The dark one hangs over widow Rösch's door. She hasn't noticed yet, she's asleep."),
        E("dlg.h1.offer.2", "Seit vierzig Tagen kommt kein Karren aus dem Beinhaus. Ohne Tote brennt der Ofen nicht. Ohne Ofen kein Glas. Ohne Glas …", "No cart has come from the ossuary in forty days. Without the dead the kiln doesn't burn. Without the kiln, no glass. Without glass …"),
        E("dlg.h1.offer.3", "Der letzte Karren steckt im Moor, bei der alten Kiste. Ein Moorschläger hat sich dort eingenistet.", "The last cart is stuck in the fen, by the old chest. A fen brute has settled in there."),
        E("dlg.h1.offer.4", "An jedem Karren hängt ein Fuhrbrief. Bringt ihn mir. Ich will wissen, wen sie geladen hatten.", "Every cart carries a cart letter. Bring it to me. I want to know who they had loaded."),
        E("dlg.h1.hint.1", "Der Fuhrbrief hängt am Karren, bei der Kiste im Moor. Und der Moorschläger wird nicht vergessen, dass ihr da wart.", "The cart letter hangs on the cart, by the chest in the fen. And the brute won't forget you were there."),
        E("dlg.h1.done.1", "Zeigt her.", "Show me."),
        E("dlg.h1.done.2", "Ottilie liest. Dann dreht sie den Brief um, als stünde auf der Rückseite etwas anderes.", "Ottilie reads. Then she turns the letter over, as if the back might say something else."),
        E("dlg.h1.done.3", "Zwölf Namen. Gestempelt, gezählt, verladen. Und der Karren war leer, sagt ihr?", "Twelve names. Stamped, counted, loaded. And the cart was empty, you say?"),
        E("dlg.h1.done.4", "Dann ist er nie beladen worden, und jemand hat trotzdem unterschrieben.", "Then it was never loaded, and someone signed for it anyway."),
        E("dlg.h1.done.5", "Das Siegel ist vom Abt. Ich war Novizin bei ihm, elf Jahre lang. Ich hätte nie gedacht, dass er lügt.", "The seal is the abbot's. I was his novice for eleven years. I never thought he would lie."),

        // ---- H2 Die Siegel vom Stillen Grund ----
        E("dlg.h2.offer.1", "Das Versunkene Beinhaus liegt hinter dem Moortor. Die Brüder vom Stillen Grund waschen dort die Toten, schreiben sie ein und geben sie dem Ofen. Seit vierzig Tagen geben sie nichts mehr.", "The Sunken Ossuary lies beyond the Fen Gate. The Brothers of the Still Ground wash the dead there, enter their names and give them to the kiln. For forty days they have given nothing."),
        E("dlg.h2.offer.2", "Man sagt, der Abt habe die Gebeine seiner Brüder aufstehen lassen. Wer nachsehen wollte, kam nicht zurück.", "They say the abbot made his brothers' bones stand up. Whoever went to look did not come back."),
        E("dlg.h2.offer.3", "Geht hinunter. Findet heraus, was er hütet. Und wenn er euch nicht durchlässt …", "Go down. Find out what he guards. And if he won't let you through …"),
        E("dlg.h2.offer.4", "… dann tut, wofür die Wacht euch bezahlt. Ich bete für ihn. Das kostet nichts.", "… then do what the Hold pays you for. I'll pray for him. That costs nothing."),
        E("dlg.h2.hint.1", "Hinter dem Moortor. Die Knochenwächter tragen noch die Kutten der Brüder. Erschlagt sie trotzdem.", "Beyond the Fen Gate. The bone wardens still wear the brothers' habits. Strike them down anyway."),
        E("dlg.h2.done.1", "Ihr riecht nach Kalk und Weihrauch. Er ist tot?", "You smell of lime and incense. He's dead?"),
        E("dlg.h2.done.2", "Gebt mir, was er bei sich trug. Nicht hier. Kommt wieder, wenn ich es gelesen habe.", "Give me what he carried. Not here. Come back when I've read it."),

        // ---- H3 Lest die Namen (Wendung 1) ----
        E("dlg.h3.offer.1", "Ottilie hat das Brevier des Abts auf den Knien. Neben ihr brennt keine Kerze.", "Ottilie holds the abbot's breviary on her knees. No candle burns beside her."),
        E("dlg.h3.offer.2", "Er hat nicht gehortet. Er hat gezählt. Hier, die Fuhrlisten des Ofens: Jeden Monat mehr Namen, als im ganzen Moor gestorben sind.", "He wasn't hoarding. He was counting. Here, the kiln's cart lists: every month more names than died in the whole fen."),
        E("dlg.h3.offer.3", "Hinter manchen Namen steht kein Todestag. Hinter einem steht ‚hustet noch‘.", "Some names have no date of death. One says ‘still coughing’."),
        E("dlg.h3.offer.4", "Er hat aufgehört, Tote herauszugeben, weil der Ofen Lebende bestellt hat. Und ihr habt ihn dafür …", "He stopped giving up the dead because the kiln was ordering the living. And you killed him for …"),
        E("dlg.h3.offer.5", "Nein. Ich habe euch geschickt.", "No. I sent you."),
        E("dlg.h3.offer.6", "Jorin Hagedorn war Lehrling am Ofen. Redet mit ihm, bevor ihr hingeht. Er sagt nicht viel, aber er lügt nicht.", "Jorin Hagedorn was an apprentice at the kiln. Talk to him before you go. He doesn't say much, but he doesn't lie."),
        E("dlg.h3.hint.1", "Redet mit Jorin an der Schmiede. Dann in den Glutofen. Der Weg führt durch das Beinhaus.", "Talk to Jorin at the smithy. Then into the Cinder Kiln. The way leads through the ossuary."),
        E("dlg.h3.talk.jorin.1", "Ottilie schickt euch. Hab's mir gedacht.", "Ottilie sent you. Figured."),
        E("dlg.h3.talk.jorin.2", "Die Meisterin. Ihr nennt sie Ofenmutter. Wir haben sie nie so genannt, wenn sie zugehört hat.", "The master. You call her the Kilnmother. We never called her that when she could hear."),
        E("dlg.h3.talk.jorin.3", "Ich hab die Lanzen ihrer Wächter geschmiedet. Gegen Plünderer, hieß es. Gegen Plünderer braucht man keine Widerhaken.", "I forged her wardens' lances. Against looters, they said. You don't need barbs against looters."),
        E("dlg.h3.talk.jorin.4", "Ihre Knechte glühen. Nicht von der Arbeit. Wenn sie fallen, platzen sie. Bleibt nicht stehen, wenn einer fällt.", "Her thralls glow. Not from the work. When they fall, they burst. Don't stand still when one falls."),
        E("dlg.h3.talk.jorin.5", "Und sie wird euch vorrechnen, wie viele Laternen noch brennen. Sie hat immer richtig gerechnet. Das war das Schlimme.", "And she'll do the sums for you, how many lanterns still burn. Her sums were always right. That was the worst of it."),

        // ---- H4 Asche schuldet niemand ----
        E("dlg.h4.hint.1", "Der Ofen brennt noch. Solange er brennt, hört sie nicht auf.", "The kiln still burns. As long as it burns, she won't stop."),
        E("dlg.h4.done.1", "Das ist ihr Brennbuch? Schwer. Ruß an jeder Seite.", "That's her firing book? Heavy. Soot on every page."),
        E("dlg.h4.done.2", "Gebt mir einen Abend damit.", "Give me an evening with it."),

        // ---- H5 Siebenundvierzig (Wendung 2) ----
        E("dlg.h5.offer.1", "Ottilie steht vor der Laterne über Ilsabes Tür. Das Rußbuch liegt aufgeschlagen auf der Schwelle.", "Ottilie stands under the lantern above Ilsabe's door. The soot book lies open on the threshold."),
        E("dlg.h5.offer.2", "Spenden. So heißt das hier. ‚Spende Krumm, zwei Personen, Ausgleich in Glas.‘", "Donations. That's the word here. ‘Donation Krumm, two persons, settled in glass.’"),
        E("dlg.h5.offer.3", "Der alte Feddersen, der nicht mehr laufen konnte. Die Kinder vom Hirten, nach dem Fieber. Alle hier drin. Jeder mit einem Preis.", "Old Feddersen, who couldn't walk any more. The shepherd's children, after the fever. All in here. Each with a price."),
        E("dlg.h5.offer.4", "Und unten auf jeder Seite das Siegel des Vogts.", "And at the foot of every page, the reeve's seal."),
        E("dlg.h5.offer.5", "Ihr kennt die Handschrift, nicht wahr? Aus ihrem Lieferbuch. Ihr habt nichts gesagt.", "You know the handwriting, don't you? From her ledger. You said nothing."),
        E("dlg.h5.offer.6", "Geht zu ihr. Sie soll es euch selbst sagen. Mir würde sie nur erklären, warum es nötig war.", "Go to her. Let her tell you herself. To me she'd only explain why it was necessary."),
        E("dlg.h5.hint.1", "Ilsabe ist an ihrem Stand. Sie ist immer an ihrem Stand.", "Ilsabe is at her stall. She's always at her stall."),
        E("dlg.h5.talk.ilsabe.1", "Ah. Ihr habt das Buch. Dann sparen wir uns das Vorspiel.", "Ah. You have the book. Then let's skip the preamble."),
        E("dlg.h5.talk.ilsabe.2", "Ja, ich habe vermittelt. Leute, die niemanden mehr hatten. Die den Winter nicht überstanden hätten. Die Wacht hätte ihn ohne Licht auch nicht überstanden.", "Yes, I arranged it. People who had no one left. Who wouldn't have survived the winter. The Hold wouldn't have survived it without light either."),
        E("dlg.h5.talk.ilsabe.3", "Der Vogt hat gesiegelt, ich habe gerechnet, die Meisterin hat gebrannt. Und ihr habt jede Nacht unter einer Laterne geschlafen. Sagt mir, wer von uns das nicht wusste.", "The reeve sealed, I counted, the master burned. And you slept under a lantern every night. Tell me which of us didn't know."),
        E("dlg.h5.talk.ilsabe.4", "Ich verkaufe nichts, was nicht vorher jemand hergegeben hat. Das ist der ganze Handel. Wollt ihr trotzdem etwas kaufen?", "I sell nothing that someone didn't give up first. That's the whole trade. Do you still want to buy something?"),
        E("dlg.h5.done.1", "Sie hat es zugegeben. Natürlich. Sie schämt sich nicht, sie rechnet.", "She admitted it. Of course she did. She isn't ashamed, she counts."),
        E("dlg.h5.done.2", "Ottilie nimmt die Kappe von der Laterne über Ilsabes Tür und drückt den Docht mit zwei Fingern aus. Es zischt nicht. Glutglas zischt nicht.", "Ottilie lifts the cap off the lantern over Ilsabe's door and pinches the wick out with two fingers. It doesn't hiss. Emberglass doesn't hiss."),
        E("dlg.h5.done.3", "Sechsundvierzig.", "Forty-six."),
        E("dlg.h5.done.4", "Morgen werden sie mich fragen, warum es dunkel ist. Ich werde es ihnen sagen. Und dann werden sie fragen, wann wieder Glas kommt.", "Tomorrow they'll ask me why it's dark. I'll tell them. And then they'll ask when the glass comes back."),

        // ---- H6–H9 Was im Glas wohnt (Wendung 3) ----
        E("dlg.h6.offer.1", "Ein Karren steht am Moortor, leer bis auf einen Sack. Die Frau auf dem Bock hat Glasstaub in den Wimpern.", "A cart stands at the Fen Gate, empty but for one sack. The woman on the box has glass dust in her lashes."),
        E("dlg.h6.offer.2", "Merle Sandkamp. Ich fahre Asche, vom Ofen in die Brache. Fuhr. Der Ofen ist kalt. Wart ihr das?", "Merle Sandkamp. I haul ash, from the kiln to the barrens. Hauled. The kiln is cold. Was that you?"),
        E("dlg.h6.offer.3", "Gut. Dann wisst ihr schon das meiste. Den Rest wisst ihr nicht.", "Good. Then you know most of it. You don't know the rest."),
        E("dlg.h6.offer.4", "Der Glasmacher sitzt in der Glasbrache und schmilzt, was wir bringen. Er hat nie gefragt, was in den Säcken war. Ich habe nie gefragt, warum er nicht fragt.", "The Glasswright sits in the Glass Barrens and melts what we bring. He never asked what was in the sacks. I never asked why he didn't ask."),
        E("dlg.h6.offer.5", "In der Brache wächst einem das Glas auf der Brust, wenn man zu lange in eine Richtung schaut. Mein Bruder Tamm hat immer nach Hause geschaut. Er ist noch dort.", "In the barrens, glass grows on your chest if you look one way too long. My brother Tamm always looked toward home. He's still out there."),
        E("dlg.h6.offer.6", "Geht hin. Ich komme nicht mit. Ich habe meinen Teil gefahren.", "Go. I'm not coming. I've driven my share."),
        E("dlg.h6.hint.1", "Die Brache liegt hinter dem Ofen, durch das Tor südlich der Kuppeln.", "The barrens lie past the kiln, through the gate south of the domes."),
        E("dlg.h7.hint.1", "Die Seher lesen in Scherben. Nehmt ihnen die Gläser ab und schaut selbst hinein. Mir haben sie nie gezeigt, was ich sehen wollte.", "The augurs read shards. Take their glasses and look for yourselves. They never showed me what I wanted to see."),
        E("dlg.h8.hint.1", "Er sitzt in der Mitte, wo das Glas am dicksten ist. Er wird reden. Lasst ihn reden, er hatte lange niemanden.", "He sits in the middle, where the glass is thickest. He'll talk. Let him, he's had no one for a long time."),
        E("dlg.h9.talk.merle.1", "Ihr habt das Prisma. Es ist warm, nicht wahr? Er hat gesagt, das sei seine Frau. Wendeline.", "You have the prism. It's warm, isn't it? He said that was his wife. Wendeline."),
        E("dlg.h9.talk.merle.2", "Die Sehgläser haben euch die Wacht gezeigt. Von außen. Die Hüllen im Nebel, alle mit dem Gesicht zum Licht.", "The seeing glasses showed you the Hold. From outside. The husks in the fog, all facing the light."),
        E("dlg.h9.talk.merle.3", "Sie kommen nicht trotz der Laternen. Sie kommen, weil ihre Glut darin steckt. Sie wollen nach Hause.", "They don't come despite the lanterns. They come because their ember is inside them. They want to go home."),
        E("dlg.h9.talk.merle.4", "Ich verkaufe das Glas nicht mehr. Sagt Ottilie, was ihr gesehen habt. Sie ist die Einzige, die die Laternen zählt.", "I don't sell the glass any more. Tell Ottilie what you saw. She's the only one who counts the lanterns."),
        E("dlg.h9.talk.ottilie.1", "Ottilie hält eine Scherbe gegen ihre Laterne. Das Licht darin flackert im Takt eines Atems.", "Ottilie holds a shard against her lantern. The light inside flickers like breathing."),
        E("dlg.h9.talk.ottilie.2", "Sechsundvierzig. Und jede ist jemand.", "Forty-six. And every one is someone."),
        E("dlg.h9.talk.ottilie.3", "Wenn wir sie zerschlagen, ist es dunkel. Wenn nicht, kommen sie weiter. Sie wollen nur nach Hause.", "If we smash them, it's dark. If we don't, they keep coming. They only want to go home."),
        E("dlg.h9.talk.ottilie.4", "Hinter dem Moortor stehen Hüllen im Nebel und sehen herüber. Keine kommt näher. Noch nicht.", "Beyond the Fen Gate, husks stand in the fog and look across. None comes closer. Not yet."),
        E("dlg.h9.talk.ottilie.5", "Geht schlafen. Ich zähle noch einmal.", "Go and sleep. I'll count once more."),

        // ---- N1 Ilsabes Lieferbuch ----
        E("dlg.n1.offer.1", "Ihr geht ins Moor? Dann tut mir einen Gefallen. Gegen Bezahlung, natürlich.", "Going into the fen? Then do me a favour. Paid, of course."),
        E("dlg.n1.offer.2", "Bei der alten Kiste liegt mein Moorvorrat, in Wachstuch gewickelt. Darin ein Lieferbuch. Nichts Wertvolles. Nur Zahlen.", "By the old chest lies my fen cache, wrapped in oilcloth. A ledger inside. Nothing valuable. Just numbers."),
        E("dlg.n1.offer.3", "Bringt es mir ungelesen. Hundertfünfzig Gold. Für Ungelesenes zahle ich gut.", "Bring it to me unread. A hundred and fifty gold. I pay well for unread."),
        E("dlg.n1.hint.1", "Das Lieferbuch. Wachstuch, bei der Kiste. Ungelesen, sagte ich.", "The ledger. Oilcloth, by the chest. Unread, I said."),
        E("dlg.n1.done.1", "Danke. Das Wachstuch ist aufgeschnitten. Na ja.", "Thank you. The oilcloth has been cut open. Oh well."),
        E("dlg.n1.done.2", "Ihr habt die Spalte mit den Mengen gesehen und gedacht: So viele Tote gibt es im ganzen Moor nicht. Stimmt. Merkt euch, dass ihr das gedacht habt.", "You saw the column of amounts and thought: there aren't that many dead in the whole fen. True. Remember that you thought it."),
        E("dlg.n1.done.3", "Hundertfünfzig. Gezählt.", "A hundred and fifty. Counted."),

        // ---- N2 Die stillen Brüder ----
        E("dlg.n2.offer.1", "An den Sarkophagen im Beinhaus hängen Grabtafeln. Die Brüder schreiben ihren Namen darauf, bevor sie sterben, nicht danach.", "Grave tablets hang on the sarcophagi in the ossuary. The brothers write their names on them before they die, not after."),
        E("dlg.n2.offer.2", "Lest mir drei davon. Ich will wissen, wer dort unten noch steht.", "Read me three of them. I want to know who is still standing down there."),
        E("dlg.n2.hint.1", "Die Tafeln hängen an den Sarkophagen. Drei reichen. Es gibt mehr.", "The tablets hang on the sarcophagi. Three will do. There are more."),
        E("dlg.n2.done.1", "Bruder Benno. Bruder Kaspar. Bruder Thewes. Thewes hat mir das Lesen beigebracht.", "Brother Benno. Brother Kaspar. Brother Thewes. Thewes taught me to read."),
        E("dlg.n2.done.2", "Darunter steht etwas, in ihrer eigenen Hand: ‚Meine Gebeine dem Grund, solange der Grund sie braucht.‘", "Below it, in their own hand: ‘My bones to the ground, as long as the ground needs them.’"),
        E("dlg.n2.done.3", "Er hat sie nicht gezwungen. Sie haben sich ihm angeboten. Nehmt das. Thewes hätte gewollt, dass es jemand trägt, der noch geht.", "He didn't force them. They offered themselves to him. Take this. Thewes would have wanted it worn by someone who still walks."),

        // ---- N3 Hagedorns Spitzen ----
        E("dlg.n3.offer.1", "Der Ofen ist offen, hab ich gehört.", "The kiln is open, I hear."),
        E("dlg.n3.offer.2", "Die Aschenlanzer tragen meine Spitzen. Jede hab ich gehärtet. Bringt mir sechs zurück. Ich will sie einschmelzen, bevor sie noch wen finden.", "The ash lancers carry my points. I hardened every one. Bring me six back. I want them melted down before they find anyone else."),
        E("dlg.n3.hint.1", "Sechs Spitzen. Die Lanzer stürmen in gerader Linie. Geht zur Seite.", "Six points. The lancers charge in a straight line. Step aside."),
        E("dlg.n3.done.1", "Sechs.", "Six."),
        E("dlg.n3.done.2", "Gegen Plünderer, hab ich gesagt. Das war gelogen. Die waren gegen Knechte, die weglaufen wollten. Die Widerhaken sollten nicht töten. Nur festhalten.", "Against looters, I said. That was a lie. They were for thralls who tried to run. The barbs weren't meant to kill. Only to hold."),
        E("dlg.n3.done.3", "Nehmt das Material. Das nächste Umschmieden zahle ich. Mehr hab ich nicht.", "Take the material. I'll pay for the next reforge. That's all I have."),

        // ---- N4 Tamm ----
        E("dlg.n4.offer.1", "Tamm trägt eine Karrnermarke. Blech, mit einer Drei drauf. Er war der dritte Fahrer.", "Tamm wears a carter's tag. Tin, with a three on it. He was the third driver."),
        E("dlg.n4.offer.2", "Er ist jetzt einer von denen, die das Glas vorn tragen und die anderen anführen.", "He's one of those now who wear the glass in front and lead the others."),
        E("dlg.n4.offer.3", "Ich will ihn nicht zurück. Ich will, dass er aufhört. Bringt mir die Marke.", "I don't want him back. I want him to stop. Bring me the tag."),
        E("dlg.n4.hint.1", "Ein Glasvogt. Die Marke hängt ihm am Hals, wenn noch ein Hals da ist.", "A glass reeve. The tag hangs at his neck, if there's still a neck."),
        E("dlg.n4.done.1", "Merle nimmt die Marke, wischt den Staub mit dem Daumen ab und legt sie in den Sack zu dem Glutglas.", "Merle takes the tag, wipes the dust off with her thumb and puts it in the sack with the emberglass."),
        E("dlg.n4.done.2", "Da drin ist er nicht allein.", "He isn't alone in there."),
        E("dlg.n4.done.3", "Nehmt das Gold. Für die Rückfahrt brauche ich es nicht mehr.", "Take the gold. I won't need it for the drive back."),

        // ---- N5 Was die Seher sahen ----
        E("dlg.n5.offer.1", "Merle erzählt, die Seher in der Brache schauen in Gläser und sehen, was kommt.", "Merle says the augurs in the barrens look into glasses and see what's coming."),
        E("dlg.n5.offer.2", "Ich hätte gern zwei davon. Merle muss das nicht wissen. Wer weiß, wann die Hüllen kommen, weiß, wann die nächste Abgabe fällig ist.", "I'd like two of them. Merle needn't know. Whoever knows when the husks come knows when the next levy is due."),
        E("dlg.n5.offer.3", "Schaut nicht so. Irgendwer muss vorausdenken. Vier Seher sollten reichen, damit zwei Gläser heil bleiben.", "Don't look at me like that. Someone has to think ahead. Four augurs should leave two glasses whole."),
        E("dlg.n5.hint.1", "Vier Seher. Zwei heile Gläser. Ich habe etwas Schönes für euch zurückgelegt.", "Four augurs. Two whole glasses. I've put something nice aside for you."),
        E("dlg.n5.done.1", "Wunderbar. Seht ihr? Drei Hüllen im Nebel, links vom Moortor. Morgen vier.", "Wonderful. You see? Three husks in the fog, left of the Fen Gate. Four tomorrow."),
        E("dlg.n5.done.2", "Nehmt den Reif. Er hat einem Seher gehört, der zu viel gesehen hat. Ich habe ihn günstig bekommen.", "Take the circlet. It belonged to an augur who saw too much. I got it cheap."),

        // ---- Idle ----
        E("dlg.idle.ottilie.1.1", "Ich zähle die Laternen, bevor es dunkel wird. Danach zählt sie keiner mehr.", "I count the lanterns before dark. After that nobody counts them."),
        E("dlg.idle.ottilie.2.1", "Sechsundvierzig. Fragt mich morgen wieder.", "Forty-six. Ask me again tomorrow."),
        E("dlg.idle.ottilie.3.1", "Ich habe heute keine gelöscht. Ich habe nur lange hingesehen.", "I put none out today. I only looked for a long time."),
        E("dlg.idle.merle.1.1", "Die Brache ist still, seit er tot ist. Still ist nicht dasselbe wie gut.", "The barrens have been quiet since he died. Quiet isn't the same as good."),
    };
}
