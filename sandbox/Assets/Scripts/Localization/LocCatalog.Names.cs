/// <summary>Proper names: places, foes, classes, skills (glossary in docs/story-bible.md).</summary>
public static partial class LocCatalog
{
    static readonly Entry[] Names =
    {
        // Places
        E("name.region", "Cinderreach", "Cinderreach"),
        E("name.zone.town", "Laternenwacht", "Lantern Hold"),
        E("name.zone.fen", "Aschenmoor", "Ashen Fen"),
        E("name.zone.fengate", "Moortor", "Fen Gate"),
        E("name.zone.ossuary", "Versunkenes Beinhaus", "Sunken Ossuary"),
        E("name.zone.kiln", "Glutofen", "Cinder Kiln"),
        E("name.zone.barrens", "Glasbrache", "Glass Barrens"),
        E("name.shrine.waypoint", "Wegschrein", "Waypoint"),
        E("name.shrine.risk", "Wagnisschrein", "Risk Shrine"),

        // Foes
        E("name.enemy.husk", "Hülle", "Husk"),
        E("name.enemy.fenbrute", "Moorschläger", "Fen Brute"),
        E("name.enemy.ashelite", "Aschenhüne", "Ash Elite"),
        E("name.enemy.bonewarden", "Knochenwächter", "Bone Warden"),
        E("name.enemy.abbot", "Der Hohle Abt", "The Hollow Abbot"),
        E("name.enemy.thrall", "Schlackenknecht", "Cinder Thrall"),
        E("name.enemy.lancer", "Aschenlanzer", "Ash Lancer"),
        E("name.enemy.kilnelite", "Ofenschinder", "Kiln Elite"),
        E("name.enemy.kilnwarden", "Ofenwächter", "Kiln Warden"),
        E("name.enemy.kilnmother", "Die Ofenmutter", "The Kilnmother"),
        E("name.enemy.shardback", "Scherbenrücken", "Shardback"),
        E("name.enemy.augur", "Dünenseher", "Dune Augur"),
        E("name.enemy.glasselite", "Glasvogt", "Glass Elite"),
        E("name.enemy.glasspillar", "Glassäule", "Glass Pillar"),
        E("name.enemy.glasswright", "Der Glasmacher", "The Glasswright"),

        // Classes and roles
        E("name.class.fenwalker", "Moorgänger", "Fenwalker"),
        E("name.class.lanternbearer", "Laternenträgerin", "Lanternbearer"),
        E("name.class.rogue", "Schmuggler", "Rogue"),
        E("name.role.tank", "Schild", "Tank"),
        E("name.role.healer", "Heilung", "Healer"),
        E("name.role.dps", "Schaden", "DPS"),

        // Skills
        E("name.skill.fireball", "Feuerball", "Fireball"),
        E("name.skill.arrow", "Bogen", "Bow"),
        E("name.skill.cleave", "Spalten", "Cleave"),
        E("name.skill.seismicslam", "Erdstoß", "Seismic Slam"),
        E("name.skill.lanternbolt", "Laternenblitz", "Lantern Bolt"),
        E("name.skill.wisps", "Irrlichter", "Wisps"),
        E("name.skill.searinghalo", "Sengender Kranz", "Searing Halo"),
        E("name.skill.veilstep", "Schleierschritt", "Veil Step"),
        E("name.skill.mendinglight", "Heilendes Licht", "Mending Light"),
        E("name.skill.renewingcircle", "Erneuernder Kreis", "Renewing Circle"),
        E("name.skill.quickshot", "Schnellschuss", "Quick Shot"),
        E("name.skill.bladeflurry", "Klingenwirbel", "Blade Flurry"),
        E("name.skill.mirehook", "Moorhaken", "Mire Hook"),
        E("name.skill.bogcharge", "Moorsturm", "Bog Charge"),
        E("name.skill.mireground", "Sumpfgrund", "Mire Ground"),
        E("name.skill.ironvow", "Eisenschwur", "Iron Vow"),
        E("name.skill.smokepot", "Rauchtopf", "Smoke Pot"),
        E("name.skill.shadowroll", "Schattenrolle", "Shadow Roll"),
        E("name.skill.venomvial", "Giftphiole", "Venom Vial"),
        E("name.skill.powderkeg", "Pulverfass", "Powder Keg"),
        E("name.skill.strike", "Schlag", "Strike"),
        E("name.skill.thrust", "Stoß", "Thrust"),
        E("name.skill.block", "Block", "Block"),

        // Boss ability showcase captions (cutscene.ability.*)
        E("cutscene.ability.abbot.slam", "Erdschlag — raus aus dem Kreis!", "Ground Slam — leave the circle!"),
        E("cutscene.ability.abbot.combo", "Nahkampf-Kombo — bleib in Bewegung!", "Melee Combo — keep moving!"),
        E("cutscene.ability.abbot.summon", "Phase 2: Er ruft drei Hüllen zu Hilfe!", "Phase 2: He summons three Husks!"),
        E("cutscene.ability.abbot.firefan", "Feuerfächer am Schlag — weiche seitlich aus!", "Fire Fan on the slam — dodge sideways!"),
        E("cutscene.ability.kilnmother.wave", "Feuerwelle — stell dich in die Lücke!", "Fire Wave — stand in the gap!"),
        E("cutscene.ability.kilnmother.field", "Phase 2: Brennender Boden unter deinen Füßen!", "Phase 2: Burning ground beneath your feet!"),
        E("cutscene.ability.kilnmother.summon", "Sie ruft vier Schlackenknechte!", "She summons four Cinder Thralls!"),
        E("cutscene.ability.glasswright.lances", "Scherbenlanzen — tritt seitlich aus der Linie!", "Shard Lances — step out of the line!"),
        E("cutscene.ability.glasswright.pillars", "Phase 2: Glassäulen schirmen ihn ab — zerschlag sie!", "Phase 2: Glass Pillars ward him — shatter them!"),
        E("cutscene.ability.glasswright.field", "Scherbenfelder unter dir — Entfacht droht die Splitternova!", "Shard fields beneath you — Kindled brings the Shatter Nova!"),

        // Difficulty
        E("name.difficulty.normal", "Normal", "Normal"),
        E("name.difficulty.kindled", "Entfacht", "Kindled"),
    };
}
