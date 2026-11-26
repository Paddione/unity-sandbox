/// <summary>Player text, domain 'skill': one effect/combo line per skill (cinderreach-class-kits-parity).</summary>
public static partial class LocCatalog
{
    static readonly Entry[] Skill =
    {
        // Base set
        E("skill.desc.fireball", "Feuerkugel, die beim Aufprall im Umkreis explodiert.", "A fire orb that bursts on impact."),
        E("skill.desc.arrow", "Schneller Pfeil auf ein einzelnes Ziel.", "A fast arrow at a single target."),
        // Moorgänger
        E("skill.desc.cleave", "Schwung um dich herum. Zerschmettern: Betäubte Gegner nehmen 50 % mehr Schaden. Vergeltung: setzt die Ladung aus dem Eisenschwur frei.", "A swing around you. Shatter: stunned foes take 50% more. Retribution: releases the Iron Vow store."),
        E("skill.desc.mirehook", "Kette auf den ersten Gegner in der Linie: zieht ihn vor dich, verschlammt ihn und fordert ihn heraus.", "Chains the first foe in line: pulls it in front of you, mires it and calls it out."),
        E("skill.desc.seismicslam", "Erdstoß betäubt alle Gegner um dich. Moorfessel: Verschlammte Gegner bleiben doppelt so lange betäubt.", "Stuns every foe around you. Fen Shackle: mired foes stay stunned twice as long."),
        E("skill.desc.bogcharge", "Ansturm bis 6 m: Jeder Gegner auf der Bahn nimmt Schaden und wird verschlammt.", "Charge up to 6 m: every foe in the lane is struck and mired."),
        E("skill.desc.mireground", "Sumpfgrund zu deinen Füßen (6 s): verschlammt Gegner und schadet ihnen jede Sekunde.", "A bog pool at your feet (6 s): mires foes and hurts them every second."),
        E("skill.desc.ironvow", "Schild von 10 Leben, fordert alle Gegner in 8 m heraus. Erlittener Schaden lädt die Vergeltung deines nächsten Spaltens.", "10 HP shield, taunts every foe within 8 m. Damage taken charges your next Cleave's Retribution."),
        // Laternenträgerin
        E("skill.desc.lanternbolt", "Lichtblitz, der ein Ziel durchbohrt.", "A bolt of light that pierces one target."),
        E("skill.desc.wisps", "Beschwört Irrlichter, die an deiner Seite kämpfen.", "Summons wisps that fight beside you."),
        E("skill.desc.searinghalo", "Brennender Kranz um dich herum.", "A burning halo around you."),
        E("skill.desc.veilstep", "Sprung durch den Schleier, bis zu 6 m.", "A blink through the veil, up to 6 m."),
        E("skill.desc.mendinglight", "Heilt den nächsten verletzten Verbündeten.", "Mends the nearest injured ally."),
        E("skill.desc.renewingcircle", "Heilt dich und alle Verbündeten im Kreis.", "Mends you and every ally in the circle."),
        // Schmuggler
        E("skill.desc.quickshot", "Schneller Bolzen. Zündschuss: Trifft er ein Pulverfass, explodiert es sofort mit 50 % mehr Wucht.", "A fast bolt. Spark Shot: through a powder keg it sets it off at once, 50% harder."),
        E("skill.desc.bladeflurry", "Klingen im Frontkegel. Kehlenschnitt: Geblendete Gegner nehmen 50 % mehr Schaden.", "Blades through the frontal cone. Throat Cut: blinded foes take 50% more."),
        E("skill.desc.smokepot", "Rauchtopf (5 s): Gegner im Rauch sind geblendet und verfehlen ihre Schläge.", "Smoke cloud (5 s): foes inside are blinded and miss their swings."),
        E("skill.desc.shadowroll", "Rolle bis 5 m. Hinterhalt: Dein nächster Treffer ist kritisch.", "Roll up to 5 m. Ambush: your next hit is a critical hit."),
        E("skill.desc.venomvial", "Giftphiole: 2 Giftstapel auf alle Getroffenen (je 1 Schaden pro Sekunde, bis 5 Stapel).", "Venom vial: 2 venom stacks on every foe hit (1 damage per second each, up to 5)."),
        E("skill.desc.powderkeg", "Pulverfass mit 1,5 s Lunte. Giftbrand: verbrennt Gift für 2 Schaden pro Stapel.", "Powder keg on a 1.5 s fuse. Venom Burn: burns off venom for 2 damage per stack."),
    };
}
