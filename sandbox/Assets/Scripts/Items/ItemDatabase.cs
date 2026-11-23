using System.Collections.Generic;

/// <summary>Base types per slot + the 3 fixed uniques (P2).</summary>
public static class ItemDatabase
{
    // second-class S4: weapon bases per class. Armor and helmets fit every class.
    public static readonly string[] FenwalkerWeapons = { "ShortSword", "HeavyAxe", "Dagger" };
    public static readonly string[] LanternbearerWeapons = { "Lantern", "HoodedLantern", "EmberCenser" };
    public static readonly string[] RogueWeapons = { "RogueBlade", "ShortBow", "HuntingBow" };
    public const string StarterSwordBase = "Worn Short Sword";
    public const string StarterLanternBase = "Worn Lantern";
    public const string StarterBowBase = "Worn Short Bow";

    /// <summary>
    /// Build uniques (cinderreach-build-uniques S1): class-bound weapons that
    /// change skill tactics. Fenwalker sword interrupts a Lancer charge with
    /// Cleave; Lanternbearer lantern prioritizes Wisp targets inside the halo.
    /// </summary>
    public const string LancerWardId = "lancer-ward";
    public const string WickOfHaloId = "wick-of-the-halo";
    public const string NightfangId = "nightfang-edge";
    public const string RogueKeepsakeId = "Rogue's Keepsake";

    /// <summary>
    /// S4 role-unique matrix (cinderreach-itemization S4): six new armor/helmet
    /// uniques on shared bases (ClassRestriction "" — freely wearable by every
    /// class). The three matrix weapons reuse the class-bound build uniques
    /// above (pinned by BuildUniqueLootTests), so "freely wearable" covers the
    /// new armor/helmets; weapons keep their class binding.
    /// </summary>
    public const string BulwarkCarrierId = "bulwark-carrier";
    public const string SeismicCrownId = "seismic-crown";
    public const string MercyShroudId = "mercy-shroud";
    public const string DawnCircleId = "circle-of-dawn";
    public const string ShadowstrideVestId = "shadowstride-vest";
    public const string KeenCadenceId = "keen-cadence";

    /// <summary>
    /// act-two-opening S4: Glass Barrens uniques, freely wearable. They drop only
    /// from bracket-4 sources (the Barrens) and from the Glasswright's first kill.
    /// </summary>
    public const string ShardbackAegisId = "shardback-aegis";
    public const string GlasswrightPrismId = "glasswright-prism";
    public static readonly string[] ZoneUniqueIds = { ShardbackAegisId, GlasswrightPrismId };

    /// <summary>Class that may equip a base type; "" = every class.</summary>
    public static string ClassRestriction(string baseType)
    {
        if (string.IsNullOrEmpty(baseType)) return "";
        if (baseType == StarterSwordBase || System.Array.IndexOf(FenwalkerWeapons, baseType) >= 0)
            return ClassCatalog.FenwalkerId;
        if (baseType == StarterLanternBase || System.Array.IndexOf(LanternbearerWeapons, baseType) >= 0)
            return ClassCatalog.LanternbearerId;
        if (baseType == StarterBowBase || System.Array.IndexOf(RogueWeapons, baseType) >= 0)
            return ClassCatalog.RogueId;
        return "";
    }

    /// <summary>Base types of a slot for one class (weapons differ, the rest is shared).</summary>
    public static List<string> GetBaseTypes(ItemSlot slot, string classId)
    {
        if (slot != ItemSlot.Weapon) return GetBaseTypes(slot);
        string id = ClassCatalog.Get(classId).id;
        if (id == ClassCatalog.LanternbearerId) return new List<string>(LanternbearerWeapons);
        if (id == ClassCatalog.RogueId) return new List<string>(RogueWeapons);
        return new List<string>(FenwalkerWeapons);
    }

    public static List<string> GetBaseTypes(ItemSlot slot)
    {
        switch (slot)
        {
            case ItemSlot.Weapon:
                return new List<string> { "ShortSword", "HeavyAxe", "Dagger" };
            case ItemSlot.Armor:
                return new List<string> { "ClothTunic", "LeatherVest", "Breastplate" };
            default:
                return new List<string> { "Hood", "Helm", "Circlet" };
        }
    }

    static readonly Dictionary<string, System.Func<ItemInstance>> UniqueFactories =
        new Dictionary<string, System.Func<ItemInstance>>(System.StringComparer.Ordinal)
        {
            ["Emberbrand Blade"] = () => new ItemInstance
            {
                slot = ItemSlot.Weapon,
                rarity = ItemRarity.Unique,
                baseType = "ShortSword",
                uniqueId = "Emberbrand Blade",
                affixes = new List<Affix> { new Affix("Damage", 6f), new Affix("Crit", 10f) },
            },
            ["Lantern of the Hold"] = () => new ItemInstance
            {
                slot = ItemSlot.Weapon,
                rarity = ItemRarity.Unique,
                baseType = "Lantern",
                uniqueId = "Lantern of the Hold",
                affixes = new List<Affix> { new Affix("SkillRank", 1f), new Affix("CooldownReduction", 10f), new Affix("MaxHP", 10f) },
            },
            ["Cryptwarden Plate"] = () => new ItemInstance
            {
                slot = ItemSlot.Armor,
                rarity = ItemRarity.Unique,
                baseType = "Breastplate",
                uniqueId = "Cryptwarden Plate",
                affixes = new List<Affix> { new Affix("MaxHP", 30f), new Affix("Armor", 4f) },
            },
            [LancerWardId] = () => new ItemInstance
            {
                slot = ItemSlot.Weapon,
                rarity = ItemRarity.Unique,
                baseType = "HeavyAxe",
                uniqueId = LancerWardId,
                affixes = new List<Affix> { new Affix("Damage", 5f) },
            },
            [WickOfHaloId] = () => new ItemInstance
            {
                slot = ItemSlot.Weapon,
                rarity = ItemRarity.Unique,
                baseType = "EmberCenser",
                uniqueId = WickOfHaloId,
                affixes = new List<Affix> { new Affix("SkillRank", 1f), new Affix("MaxHP", 10f) },
            },
            [NightfangId] = () => new ItemInstance
            {
                slot = ItemSlot.Weapon,
                rarity = ItemRarity.Unique,
                baseType = "ShortBow",
                uniqueId = NightfangId,
                affixes = new List<Affix> { new Affix("Damage", 5f), new Affix("Crit", 8f) },
            },
            [RogueKeepsakeId] = () => new ItemInstance
            {
                slot = ItemSlot.Weapon,
                rarity = ItemRarity.Unique,
                baseType = "RogueBlade",
                uniqueId = RogueKeepsakeId,
                affixes = new List<Affix> { new Affix("Damage", 4f), new Affix("Crit", 10f) },
            },
            [BulwarkCarrierId] = () => new ItemInstance
            {
                slot = ItemSlot.Armor,
                rarity = ItemRarity.Unique,
                baseType = "Breastplate",
                uniqueId = BulwarkCarrierId,
                affixes = new List<Affix> { new Affix("MaxHP", 20f), new Affix("Armor", 3f) },
            },
            [SeismicCrownId] = () => new ItemInstance
            {
                slot = ItemSlot.Helmet,
                rarity = ItemRarity.Unique,
                baseType = "Helm",
                uniqueId = SeismicCrownId,
                affixes = new List<Affix> { new Affix("MaxHP", 20f), new Affix("CooldownReduction", 8f) },
            },
            [MercyShroudId] = () => new ItemInstance
            {
                slot = ItemSlot.Armor,
                rarity = ItemRarity.Unique,
                baseType = "ClothTunic",
                uniqueId = MercyShroudId,
                affixes = new List<Affix> { new Affix("MaxHP", 20f), new Affix("MagicFind", 20f) },
            },
            [DawnCircleId] = () => new ItemInstance
            {
                slot = ItemSlot.Helmet,
                rarity = ItemRarity.Unique,
                baseType = "Circlet",
                uniqueId = DawnCircleId,
                affixes = new List<Affix> { new Affix("SkillRank", 1f), new Affix("MaxHP", 20f) },
            },
            [ShadowstrideVestId] = () => new ItemInstance
            {
                slot = ItemSlot.Armor,
                rarity = ItemRarity.Unique,
                baseType = "LeatherVest",
                uniqueId = ShadowstrideVestId,
                affixes = new List<Affix> { new Affix("MoveSpeed", 0.3f), new Affix("Armor", 3f) },
            },
            [KeenCadenceId] = () => new ItemInstance
            {
                slot = ItemSlot.Helmet,
                rarity = ItemRarity.Unique,
                baseType = "Hood",
                uniqueId = KeenCadenceId,
                affixes = new List<Affix> { new Affix("Crit", 10f), new Affix("CritRating", 20f) },
            },
            [ShardbackAegisId] = () => new ItemInstance
            {
                slot = ItemSlot.Armor,
                rarity = ItemRarity.Unique,
                baseType = "Breastplate",
                uniqueId = ShardbackAegisId,
                affixes = new List<Affix> { new Affix("MaxHP", 25f), new Affix("Armor", 5f), new Affix("Thorns", 3f) },
            },
            [GlasswrightPrismId] = () => new ItemInstance
            {
                slot = ItemSlot.Helmet,
                rarity = ItemRarity.Unique,
                baseType = "Circlet",
                uniqueId = GlasswrightPrismId,
                affixes = new List<Affix> { new Affix("CritRating", 25f), new Affix("IncreasedArea", 20f), new Affix("CooldownReduction", 10f) },
            },
            ["Seer's Circlet"] = () => new ItemInstance
            {
                slot = ItemSlot.Helmet,
                rarity = ItemRarity.Unique,
                baseType = "Circlet",
                uniqueId = "Seer's Circlet",
                affixes = new List<Affix> { new Affix("XPMul", 20f), new Affix("Crit", 8f) },
            },
        };

    public static IEnumerable<string> UniqueIds => UniqueFactories.Keys;

    public static ItemInstance CreateUnique(string uniqueId)
    {
        if (uniqueId != null && UniqueFactories.TryGetValue(uniqueId, out var factory))
            return factory();
        return UniqueFactories["Seer's Circlet"]();
    }

    /// <summary>
    /// Playtest feedback 2026-11-23: the game carries only Rare and Unique
    /// items. <see cref="ItemRarity.Magic"/> stays in the enum (save and net
    /// int contract) but is never created; items that still carry it (old
    /// saves, join reports) are promoted to Rare with their affixes intact.
    /// True when the item changed.
    /// </summary>
    public static bool RetireMagic(ItemInstance item)
    {
        if (item == null || item.rarity != ItemRarity.Magic) return false;
        item.rarity = ItemRarity.Rare;
        return true;
    }

    /// <summary>Healing potion (S3 vendor consumable, slot Consumable, drunk with key 1).</summary>
    public const int PotionPrice = 10;

    /// <summary>HP restored by one healing potion.</summary>
    public const float PotionHeal = 10f;

    /// <summary>Stackable vendor potion: bagged like loot, never equipped.</summary>
    public static ItemInstance CreateHealingPotion()
    {
        return new ItemInstance
        {
            slot = ItemSlot.Consumable,
            rarity = ItemRarity.Rare,
            baseType = "Healing Potion",
            affixes = new List<Affix>(),
        };
    }

    /// <summary>Fixed starter sword, auto-equipped at spawn (Ashen Fen).</summary>
    public static ItemInstance CreateStarterSword()
    {
        return new ItemInstance
        {
            slot = ItemSlot.Weapon,
            rarity = ItemRarity.Rare,
            baseType = StarterSwordBase,
            affixes = new List<Affix> { new Affix("Damage", 2f) },
        };
    }

    /// <summary>Lanternbearer starter weapon (same +2 Damage as the sword).</summary>
    public static ItemInstance CreateStarterLantern()
    {
        return new ItemInstance
        {
            slot = ItemSlot.Weapon,
            rarity = ItemRarity.Rare,
            baseType = StarterLanternBase,
            affixes = new List<Affix> { new Affix("Damage", 2f) },
        };
    }

    /// <summary>Rogue starter weapon (same +2 Damage as the sword).</summary>
    public static ItemInstance CreateStarterBow()
    {
        return new ItemInstance
        {
            slot = ItemSlot.Weapon,
            rarity = ItemRarity.Rare,
            baseType = StarterBowBase,
            affixes = new List<Affix> { new Affix("Damage", 2f) },
        };
    }

    /// <summary>Kindled-finale reward weapon (S2): class-compatible Rare with Kindled affixes.</summary>
    public static ItemInstance CreateKindledFinaleWeapon(string classId)
    {
        string id = ClassCatalog.Get(classId).id;
        string baseType = id == ClassCatalog.LanternbearerId ? LanternbearerWeapons[0]
            : id == ClassCatalog.RogueId ? RogueWeapons[0] : FenwalkerWeapons[0];
        const int itemLevel = ItemLevelRules.KindledFinaleRewardLevel;
        const int affixTier = 2;
        return new ItemInstance
        {
            slot = ItemSlot.Weapon,
            rarity = ItemRarity.Rare,
            baseType = baseType,
            itemLevel = itemLevel,
            affixTier = affixTier,
            instanceId = System.Guid.NewGuid().ToString("N"),
            affixes = new List<Affix>
            {
                new Affix(AffixLibrary.Damage, AffixLibrary.GetRange(AffixLibrary.Damage, itemLevel, affixTier).min),
                new Affix(AffixLibrary.Crit, AffixLibrary.GetRange(AffixLibrary.Crit, itemLevel, affixTier).min),
                new Affix(AffixLibrary.MaxHP, AffixLibrary.GetRange(AffixLibrary.MaxHP, itemLevel, affixTier).min),
            },
        };
    }

    /// <summary>Apply top bracket values to a newly generated Unique template.</summary>
    public static ItemInstance RollUnique(ItemInstance item, int itemLevel, int affixTier)
    {
        item.itemLevel = itemLevel;
        item.affixTier = affixTier;
        item.EnsureIdentity();
        foreach (var affix in item.affixes)
            affix.value = AffixLibrary.GetRange(affix.key, itemLevel, affixTier).max;
        return item;
    }

    public static List<ItemInstance> GetUniques()
    {
        return new List<ItemInstance>
        {
            CreateUnique("Emberbrand Blade"),
            CreateUnique("Cryptwarden Plate"),
            CreateUnique("Seer's Circlet"),
        };
    }

    /// <summary>
    /// S4 matrix (PartyRole, ItemSlot) → uniqueId. Weapons reuse the build
    /// uniques (class-bound, pinned); armor/helmets are the six new IDs.
    /// </summary>
    public static string UniqueForMatrix(ClassDefinition.PartyRole role, ItemSlot slot)
    {
        switch (role)
        {
            case ClassDefinition.PartyRole.Tank:
                if (slot == ItemSlot.Weapon) return LancerWardId;
                if (slot == ItemSlot.Armor) return BulwarkCarrierId;
                return SeismicCrownId;
            case ClassDefinition.PartyRole.Healer:
                if (slot == ItemSlot.Weapon) return WickOfHaloId;
                if (slot == ItemSlot.Armor) return MercyShroudId;
                return DawnCircleId;
            default:
                if (slot == ItemSlot.Weapon) return NightfangId;
                if (slot == ItemSlot.Armor) return ShadowstrideVestId;
                return KeenCadenceId;
        }
    }

    /// <summary>All nine matrix unique IDs (3 roles × weapon/armor/helmet).</summary>
    public static readonly string[] MatrixUniqueIds =
    {
        LancerWardId, BulwarkCarrierId, SeismicCrownId,
        WickOfHaloId, MercyShroudId, DawnCircleId,
        NightfangId, ShadowstrideVestId, KeenCadenceId,
    };

    static readonly string[] MatrixArmors = { BulwarkCarrierId, MercyShroudId, ShadowstrideVestId };
    static readonly string[] MatrixHelmets = { SeismicCrownId, DawnCircleId, KeenCadenceId };

    /// <summary>True for the nine S4 matrix IDs.</summary>
    public static bool IsMatrixUnique(string uniqueId)
    {
        if (string.IsNullOrEmpty(uniqueId)) return false;
        return System.Array.IndexOf(MatrixUniqueIds, uniqueId) >= 0;
    }

    /// <summary>
    /// Host catalog check (S4): every uniqueId the game itself hands out —
    /// legacy P2 uniques, build uniques and the S4 matrix. Unknown IDs are
    /// rejected on the host path (SanitizeGearItem) and never equipped.
    /// </summary>
    public static bool IsKnownUniqueId(string uniqueId) =>
        uniqueId != null && UniqueFactories.ContainsKey(uniqueId);

    /// <summary>
    /// Regular armor Unique pool (S4): legacy Cryptwarden Plate or one of the
    /// three matrix armors (split share in <see cref="DroptableDatabase"/>).
    /// The aggregate Unique weight is untouched — only the within-slot pick.
    /// </summary>
    public static string UniqueArmor(System.Random rng)
    {
        if (rng != null && rng.NextDouble() < DroptableDatabase.MatrixNonWeaponShare)
            return MatrixArmors[rng.Next(MatrixArmors.Length)];
        return "Cryptwarden Plate";
    }

    /// <summary>
    /// Regular helmet Unique pool (S4): legacy Seer's Circlet or one of the
    /// three matrix helmets. Same within-slot split as armor.
    /// </summary>
    public static string UniqueHelmet(System.Random rng)
    {
        if (rng != null && rng.NextDouble() < DroptableDatabase.MatrixNonWeaponShare)
            return MatrixHelmets[rng.Next(MatrixHelmets.Length)];
        return "Seer's Circlet";
    }

    /// <summary>
    /// act-two-opening S4: armor/helmet Unique pick at a bracket-4 source. Half
    /// of those Uniques are the slot's Barrens unique, the rest keep the regular pool.
    /// </summary>
    public static string UniqueForSlot(ItemSlot slot, int itemLevel, System.Random rng)
    {
        if (ItemLevelRules.GetBracket(itemLevel) >= 4 && rng != null
            && rng.NextDouble() < DroptableDatabase.ZoneUniqueShare)
            return slot == ItemSlot.Armor ? ShardbackAegisId : GlasswrightPrismId;
        return slot == ItemSlot.Armor ? UniqueArmor(rng) : UniqueHelmet(rng);
    }

    /// <summary>
    /// Build-unique weapon for a class (cinderreach-build-uniques S1): picks
    /// between the tactial build unique and the legacy class weapon unique so
    /// both stay regularly obtainable through the existing class-bound Unique
    /// selection. Drop chances are unchanged (regular Unique weight applies).
    /// </summary>
    public static ItemInstance UniqueWeaponForClass(string classId, System.Random rng)
    {
        bool build = rng != null && rng.NextDouble() < DroptableDatabase.BuildUniqueWeaponShare;
        string id;
        string own = ClassCatalog.Get(classId).id;
        if (own == ClassCatalog.LanternbearerId)
            id = build ? WickOfHaloId : "Lantern of the Hold";
        else if (own == ClassCatalog.RogueId)
            id = build ? NightfangId : RogueKeepsakeId;
        else
            id = build ? LancerWardId : "Emberbrand Blade";
        return CreateUnique(id);
    }
}
