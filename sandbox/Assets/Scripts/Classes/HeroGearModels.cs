using System;
using System.Collections.Generic;
using UnityEngine;

public static class HeroGearModels
{
    public static readonly Color Brass = new Color32(242, 193, 62, 255);
    static readonly Color Iron = new Color32(124, 151, 159, 255);
    static readonly Color Dark = new Color32(51, 58, 74, 255);
    static readonly Color Ember = new Color32(246, 95, 47, 255);
    static readonly Color Pearl = new Color32(221, 213, 189, 255);
    static readonly Color Jade = new Color32(78, 195, 177, 255);
    static readonly Color Lilac = new Color32(173, 126, 223, 255);

    readonly struct Model
    {
        public readonly ItemSlot Slot;
        public readonly Action<Transform> Build;
        public Model(ItemSlot slot, Action<Transform> build) { Slot = slot; Build = build; }
    }

    static readonly Dictionary<string, Model> Models = new Dictionary<string, Model>(StringComparer.Ordinal)
    {
        ["Emberbrand Blade"] = new Model(ItemSlot.Weapon, root =>
        {
            Grip(root);
            Plate(root, "Flame edge", Ember, .045f, 0,
                -.10f,.17f, -.04f,.31f, -.09f,.43f, 0,.66f, .09f,.46f, .045f,.31f, .09f,.17f);
        }),
        [ItemDatabase.LancerWardId] = new Model(ItemSlot.Weapon, root =>
        {
            Grip(root, .46f);
            Plate(root, "Bearded axe head", Iron, .075f, 0,
                -.07f,.38f, -.07f,.56f, .12f,.59f, .28f,.66f, .32f,.47f, .28f,.28f, .15f,.32f, .12f,.41f);
        }),
        [ItemDatabase.RogueKeepsakeId] = new Model(ItemSlot.Weapon, root =>
        {
            Grip(root);
            Plate(root, "Hooked blade", Lilac, .045f, 0, -.06f,.14f, -.04f,.36f, .05f,.46f,
                .16f,.42f, .09f,.39f, .055f,.29f, .06f,.14f);
        }),
        [ItemDatabase.NightfangId] = new Model(ItemSlot.Weapon, root =>
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Plate(root, "Recurved limb", Lilac, .05f, 0, -.025f,0, .035f,0, .12f,.20f*side,
                    .08f,.36f*side, -.06f,.53f*side, -.03f,.35f*side, .04f,.18f*side);
                Beam(root, "String", Pearl, new Vector3(-.06f,.52f*side,0), new Vector3(-.06f,0,0), .009f);
            }
        }),
        ["Lantern of the Hold"] = new Model(ItemSlot.Weapon, root =>
        {
            Beam(root, "Handle", Dark, Vector3.zero, new Vector3(0,-.16f,0), .045f);
            Box(root, "Square roof", Brass, new Vector3(0,-.15f,0), new Vector3(.28f,.07f,.28f));
            Box(root, "Square foot", Brass, new Vector3(0,-.44f,0), new Vector3(.24f,.055f,.24f));
            Box(root, "Light chamber", Ember, new Vector3(0,-.30f,0), new Vector3(.15f,.20f,.15f));
            foreach (float x in new[] { -.105f, .105f })
                foreach (float z in new[] { -.105f, .105f })
                    Box(root, "Corner post", Dark, new Vector3(x,-.30f,z), new Vector3(.029f,.27f,.029f));
        }),
        [ItemDatabase.WickOfHaloId] = new Model(ItemSlot.Weapon, root =>
        {
            Beam(root, "Censer handle", Pearl, Vector3.zero, new Vector3(0,-.17f,0), .05f);
            Crystal(root, "Censer bowl", Brass, new Vector3(0,-.44f,0), new Vector3(.30f,.17f,.30f));
            Crystal(root, "Censer core", Jade, new Vector3(0,-.33f,0), new Vector3(.17f,.22f,.17f));
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 2f * Mathf.PI / 3f;
                Beam(root, "Chain", Iron, new Vector3(0,-.14f,0),
                    new Vector3(.127f * Mathf.Cos(angle),-.42f,.127f * Mathf.Sin(angle)), .018f);
            }
        }),
        ["Cryptwarden Plate"] = new Model(ItemSlot.Armor, root =>
        {
            Plate(root, "Tomb plate", Iron, .10f, .36f, -.30f,.10f, .30f,.10f, .23f,-.27f,
                0,-.37f, -.23f,-.27f);
            Box(root, "Raised spine", Brass, new Vector3(0,-.10f,.425f), new Vector3(.09f,.34f,.045f));
        }),
        [ItemDatabase.BulwarkCarrierId] = new Model(ItemSlot.Armor, root =>
        {
            Plate(root, "Tall shield breastplate", Brass, .11f, .36f, -.37f,.13f, 0,.20f, .37f,.13f,
                .31f,-.24f, 0,-.38f, -.31f,-.24f);
            Box(root, "Crossbar", Dark, new Vector3(0,-.02f,.429f), new Vector3(.52f,.075f,.05f));
        }),
        [ItemDatabase.MercyShroudId] = new Model(ItemSlot.Armor, root =>
        {
            Plate(root, "Split mantle", Pearl, .06f, .36f, -.39f,.10f, -.14f,.18f, 0,.05f, .14f,.18f,
                .39f,.10f, .31f,-.15f, .08f,-.30f, 0,-.18f, -.08f,-.30f, -.31f,-.15f);
            Crystal(root, "Brooch", Jade, new Vector3(0,.015f,.405f), new Vector3(.14f,.16f,.08f));
        }),
        [ItemDatabase.ShadowstrideVestId] = new Model(ItemSlot.Armor, root =>
        {
            Plate(root, "Asymmetric leather panels", Dark, .09f, .36f, -.30f,.10f, -.06f,.12f, .30f,-.29f,
                .22f,-.39f, -.29f,-.29f);
            Beam(root, "Diagonal clasp", Lilac, new Vector3(-.23f,.08f,.419f),
                new Vector3(.20f,-.31f,.419f), .063f);
        }),
        [ItemDatabase.ShardbackAegisId] = new Model(ItemSlot.Armor, root =>
        {
            Plate(root, "Jagged cuirass", Dark, .10f, .36f, -.32f,.09f, 0,.14f, .32f,.09f, .20f,-.37f, -.20f,-.37f);
            for (int i = -2; i <= 2; i++)
                Crystal(root, "Glass splinter", Jade, new Vector3(i*.13f,.12f-Mathf.Abs(i)*.042f,-.39f),
                    new Vector3(.14f,.49f-Mathf.Abs(i)*.095f,.20f));
        }),
        ["Seer's Circlet"] = new Model(ItemSlot.Helmet, root =>
        {
            Band(root, Brass, .26f, .20f, .51f);
            Crystal(root, "Oracle stone", Lilac, new Vector3(0,.32f,.27f), new Vector3(.16f,.24f,.12f));
        }),
        [ItemDatabase.SeismicCrownId] = new Model(ItemSlot.Helmet, root =>
        {
            Band(root, Iron, .30f, .21f, .55f);
            for (int i = -2; i <= 2; i++)
                Crystal(root, "Fault spire", Brass, new Vector3(i*.116f,.38f-Mathf.Abs(i)*.033f,.23f),
                    new Vector3(.10f,.31f,.13f));
        }),
        [ItemDatabase.DawnCircleId] = new Model(ItemSlot.Helmet, root =>
        {
            Band(root, Brass, .25f, .18f, .51f);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4f;
                var center = new Vector3(0,.32f,-.25f);
                var ray = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle),0);
                Beam(root, "Sun ray", Pearl, center + ray*.16f, center + ray*.34f, .047f);
            }
        }),
        [ItemDatabase.KeenCadenceId] = new Model(ItemSlot.Helmet, root =>
        {
            Plate(root, "Forward cowl", Dark, .10f, .24f, -.26f,.17f, -.27f,.36f, -.14f,.50f, .14f,.50f,
                .27f,.36f, .26f,.17f, .13f,.31f, -.13f,.31f);
            Box(root, "Hood crest", Lilac, new Vector3(0,.44f,.29f), new Vector3(.075f,.08f,.20f));
        }),
        [ItemDatabase.GlasswrightPrismId] = new Model(ItemSlot.Helmet, root =>
        {
            Band(root, Dark, .26f, .21f, .54f);
            Crystal(root, "Central prism", Jade, new Vector3(0,.50f,.15f), new Vector3(.30f,.42f,.28f));
            Crystal(root, "Left prism", Jade, new Vector3(-.24f,.42f,.13f), new Vector3(.13f,.25f,.16f));
            Crystal(root, "Right prism", Jade, new Vector3(.24f,.42f,.13f), new Vector3(.13f,.25f,.16f));
        }),
        // ---- gear-slots S5 zone uniques ----
        [ItemDatabase.CinderfangCleaverId] = new Model(ItemSlot.Weapon, root =>
        {
            Grip(root, .44f);
            Plate(root, "Cleaver blade", Ember, .07f, 0,
                -.06f,.34f, -.06f,.62f, .26f,.62f, .30f,.48f, .22f,.34f);
            Box(root, "Cinder spine", Dark, new Vector3(.08f,.63f,0), new Vector3(.30f,.035f,.08f));
        }),
        [ItemDatabase.OssuaryThuribleId] = new Model(ItemSlot.Weapon, root =>
        {
            Beam(root, "Thurible chain", Iron, Vector3.zero, new Vector3(0,-.2f,0), .03f);
            Crystal(root, "Bone cage", Pearl, new Vector3(0,-.36f,0), new Vector3(.26f,.26f,.26f));
            Crystal(root, "Grave ember", Jade, new Vector3(0,-.36f,0), new Vector3(.12f,.16f,.12f));
        }),
        [ItemDatabase.GlassthornBowId] = new Model(ItemSlot.Weapon, root =>
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Plate(root, "Glass limb", Jade, .05f, 0, -.025f,0, .035f,0, .10f,.22f*side,
                    .05f,.40f*side, -.07f,.55f*side, -.03f,.38f*side, .03f,.2f*side);
                Crystal(root, "Thorn", Pearl, new Vector3(.09f,.3f*side,0), new Vector3(.07f,.10f,.05f));
                Beam(root, "String", Pearl, new Vector3(-.07f,.54f*side,0), new Vector3(-.07f,0,0), .009f);
            }
        }),
        [ItemDatabase.FenWrapId] = new Model(ItemSlot.Armor, root =>
        {
            Plate(root, "Reed wrap", Jade, .06f, .36f, -.32f,.10f, .32f,.10f, .26f,-.30f, -.26f,-.30f);
            for (int i = 0; i < 3; i++)
                Box(root, "Wrap band", Leather, new Vector3(0,.02f-i*.12f,.395f), new Vector3(.58f,.035f,.03f));
        }),
        [ItemDatabase.AshenHauberkId] = new Model(ItemSlot.Armor, root =>
        {
            Plate(root, "Ash mail", Dark, .10f, .36f, -.34f,.12f, .34f,.12f, .30f,-.36f, -.30f,-.36f);
            Plate(root, "Ash collar", Iron, .06f, .42f, -.20f,.16f, .20f,.16f, .12f,.04f, -.12f,.04f);
        }),
        [ItemDatabase.KilnheartMailId] = new Model(ItemSlot.Armor, root =>
        {
            Plate(root, "Kiln plates", Iron, .09f, .36f, -.31f,.11f, .31f,.11f, .24f,-.31f, -.24f,-.31f);
            Crystal(root, "Kiln heart", Ember, new Vector3(0,-.06f,.42f), new Vector3(.16f,.20f,.06f));
        }),
        [ItemDatabase.ProspectorsHoodId] = new Model(ItemSlot.Helmet, root =>
        {
            Plate(root, "Prospector cowl", Leather, .10f, .24f, -.26f,.17f, -.25f,.38f, -.10f,.48f, .10f,.48f,
                .25f,.38f, .26f,.17f);
            Crystal(root, "Lamp stone", Brass, new Vector3(0,.36f,.30f), new Vector3(.10f,.12f,.06f));
        }),
        [ItemDatabase.BonewatchVisorId] = new Model(ItemSlot.Helmet, root =>
        {
            Band(root, Pearl, .28f, .20f, .54f);
            Plate(root, "Bone visor", Pearl, .05f, .28f, -.22f,.30f, .22f,.30f, .18f,.16f, -.18f,.16f);
            Box(root, "Eye slit", Dark, new Vector3(0,.235f,.31f), new Vector3(.30f,.03f,.02f));
        }),
        [ItemDatabase.EmberwreathCrownId] = new Model(ItemSlot.Helmet, root =>
        {
            Band(root, Brass, .26f, .20f, .52f);
            for (int i = -2; i <= 2; i++)
                Crystal(root, "Ember leaf", Ember, new Vector3(i*.11f,.32f-Mathf.Abs(i)*.03f,.24f),
                    new Vector3(.09f,.18f,.06f));
        }),
    };

    // ---- gear-slots S3: worn hands, feet and belt --------------------------------
    static readonly Color Leather = new Color32(122, 82, 52, 255);
    static readonly Color Cloth = new Color32(176, 160, 132, 255);

    /// <summary>Look of one hands/feet/belt piece: base colour, trim colour, build weight 0 light .. 2 heavy.</summary>
    readonly struct LimbLook
    {
        public readonly ItemSlot Slot;
        public readonly Color Main;
        public readonly Color Trim;
        public readonly int Weight;
        public LimbLook(ItemSlot slot, Color main, Color trim, int weight) { Slot = slot; Main = main; Trim = trim; Weight = weight; }
    }

    static readonly Dictionary<string, LimbLook> Limbs = new Dictionary<string, LimbLook>(StringComparer.Ordinal)
    {
        ["SilkWraps"] = new LimbLook(ItemSlot.Gloves, Cloth, Pearl, 0),
        ["LeatherGloves"] = new LimbLook(ItemSlot.Gloves, Leather, Dark, 1),
        ["ChainGauntlets"] = new LimbLook(ItemSlot.Gloves, Iron, Dark, 2),
        ["WrappedSandals"] = new LimbLook(ItemSlot.Boots, Cloth, Leather, 0),
        ["LeatherBoots"] = new LimbLook(ItemSlot.Boots, Leather, Dark, 1),
        ["IronSabatons"] = new LimbLook(ItemSlot.Boots, Iron, Dark, 2),
        ["RopeSash"] = new LimbLook(ItemSlot.Belt, Cloth, Leather, 0),
        ["LeatherBelt"] = new LimbLook(ItemSlot.Belt, Leather, Brass, 1),
        ["StuddedGirdle"] = new LimbLook(ItemSlot.Belt, Dark, Iron, 2),
        [ItemDatabase.IronboundGripsId] = new LimbLook(ItemSlot.Gloves, Iron, Brass, 2),
        [ItemDatabase.MendersTouchId] = new LimbLook(ItemSlot.Gloves, Pearl, Jade, 0),
        [ItemDatabase.CutpurseWrapsId] = new LimbLook(ItemSlot.Gloves, Dark, Lilac, 1),
        [ItemDatabase.BastionTreadsId] = new LimbLook(ItemSlot.Boots, Brass, Iron, 2),
        [ItemDatabase.PilgrimStepsId] = new LimbLook(ItemSlot.Boots, Pearl, Jade, 0),
        [ItemDatabase.GhostwalkBootsId] = new LimbLook(ItemSlot.Boots, Dark, Lilac, 1),
        [ItemDatabase.WardensGirdleId] = new LimbLook(ItemSlot.Belt, Iron, Brass, 2),
        [ItemDatabase.VialSashId] = new LimbLook(ItemSlot.Belt, Pearl, Jade, 0),
        [ItemDatabase.QuickdrawBeltId] = new LimbLook(ItemSlot.Belt, Dark, Ember, 1),
    };

    /// <summary>True for the three gear-slots that render on hands, feet and hips.</summary>
    public static bool IsLimbSlot(ItemSlot slot) =>
        slot == ItemSlot.Gloves || slot == ItemSlot.Boots || slot == ItemSlot.Belt;

    /// <summary>Art key of a worn hands/feet/belt item: the unique id when it has a look, else the base.</summary>
    public static string VisualKey(ItemInstance item)
    {
        if (item == null || Equipment.IsBlank(item) || !IsLimbSlot(item.slot)) return null;
        if (item.rarity == ItemRarity.Unique && item.uniqueId != null && Limbs.ContainsKey(item.uniqueId)) return item.uniqueId;
        return Limbs.ContainsKey(item.baseType ?? "") ? item.baseType : null;
    }

    public static bool IsKnownLimb(string key, ItemSlot slot) =>
        key != null && Limbs.TryGetValue(key, out var look) && look.Slot == slot;

    public static IEnumerable<string> LimbKeys => Limbs.Keys;

    /// <summary>
    /// One glove on a wrist bone. Authored in bone space: +Y runs from the
    /// wrist into the hand (KayKit rig convention), so the cuff rings the bone.
    /// </summary>
    public static GameObject CreateGlove(string key, Transform wrist)
    {
        if (!IsKnownLimb(key, ItemSlot.Gloves) || wrist == null) return null;
        var look = Limbs[key];
        var root = LimbRoot(key, wrist);
        Ring(root, "Glove cuff", look.Main, .082f + .008f * look.Weight, .082f + .008f * look.Weight, -.03f, .05f + .015f * look.Weight);
        Ring(root, "Cuff trim", look.Trim, .09f + .008f * look.Weight, .09f + .008f * look.Weight, -.065f - .01f * look.Weight, .025f);
        Box(root, "Glove back", look.Main, new Vector3(0f, .08f, 0f), new Vector3(.20f, .15f, .19f));
        if (look.Weight >= 2)
            Box(root, "Knuckle plate", look.Trim, new Vector3(0f, .12f, .1f), new Vector3(.2f, .05f, .03f));
        return root.gameObject;
    }

    /// <summary>One boot: shaft rings on the lower leg, the shoe shell on the foot bone.</summary>
    public static GameObject CreateBoot(string key, Transform lowerLeg, Transform foot)
    {
        if (!IsKnownLimb(key, ItemSlot.Boots) || lowerLeg == null || foot == null) return null;
        var look = Limbs[key];
        var root = LimbRoot(key, lowerLeg);
        float r = .085f + .01f * look.Weight;
        int rings = look.Weight + 1;
        for (int i = 0; i < rings; i++)
            Ring(root, "Boot shaft", look.Main, r, r, .14f - i * .045f, .045f);
        Ring(root, "Boot top", look.Trim, r + .01f, r + .01f, .14f - rings * .045f, .025f);
        var shoe = LimbRoot("Boot shoe", foot);
        Link(root.gameObject, shoe.gameObject);
        Box(shoe, "Shoe", look.Main, new Vector3(0f, .08f, 0f), new Vector3(.21f + .01f * look.Weight, .26f, .17f));
        Box(shoe, "Sole", look.Trim, new Vector3(0f, .085f, -.09f), new Vector3(.22f, .27f, .03f));
        return root.gameObject;
    }

    /// <summary>
    /// Belt on the hips bone (+Y towards the spine). <paramref name="forward"/>
    /// is the hero's facing in hips space, so the buckle sits at the front.
    /// </summary>
    public static GameObject CreateBelt(string key, Transform hips, Vector3 forward)
    {
        if (!IsKnownLimb(key, ItemSlot.Belt) || hips == null) return null;
        var look = Limbs[key];
        var root = LimbRoot(key, hips);
        forward.y = 0f;
        if (forward.sqrMagnitude < .0001f) forward = Vector3.forward;
        root.localRotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        Ring(root, "Belt strap", look.Main, .235f, .2f, .13f, .05f + .012f * look.Weight);
        Box(root, "Buckle", look.Trim, new Vector3(0f, .13f, .205f), new Vector3(.09f, .075f, .03f));
        if (look.Weight == 0)
            Box(root, "Sash tail", look.Main, new Vector3(.11f, .05f, .19f), new Vector3(.05f, .16f, .02f));
        if (look.Weight >= 1)
            Box(root, "Pouch", look.Trim, new Vector3(-.19f, .09f, .09f), new Vector3(.07f, .08f, .06f));
        if (look.Weight >= 2)
            foreach (float x in new[] { -.13f, .13f })
                Box(root, "Stud", look.Trim, new Vector3(x, .13f, .19f), new Vector3(.03f, .03f, .03f));
        return root.gameObject;
    }

    /// <summary>Both gloves; the right one follows the left one's lifetime and visibility.</summary>
    public static GameObject CreateGlovePair(string key, Transform leftWrist, Transform rightWrist)
    {
        var left = CreateGlove(key, leftWrist);
        if (left == null) return null;
        Link(left, CreateGlove(key, rightWrist));
        return left;
    }

    /// <summary>Both boots, linked like the gloves.</summary>
    public static GameObject CreateBootPair(string key, Transform leftLeg, Transform leftFoot, Transform rightLeg, Transform rightFoot)
    {
        var left = CreateBoot(key, leftLeg, leftFoot);
        if (left == null) return null;
        Link(left, CreateBoot(key, rightLeg, rightFoot));
        return left;
    }

    /// <summary>
    /// Limb piece on a stand-in rig (editor verification): wrists, shins, feet
    /// and hips as plain children of one root, so the whole look is one object.
    /// </summary>
    public static GameObject CreateLimbPreview(string key, ItemSlot slot)
    {
        if (!IsKnownLimb(key, slot)) return null;
        var root = new GameObject(key + " preview");
        Transform Bone(string name, Vector3 at)
        {
            var bone = new GameObject(name).transform;
            bone.SetParent(root.transform, false);
            bone.localPosition = at;
            return bone;
        }
        if (slot == ItemSlot.Gloves)
            CreateGlovePair(key, Bone("wrist.l", new Vector3(-.7f, 1.1f, 0f)), Bone("wrist.r", new Vector3(.7f, 1.1f, 0f)));
        else if (slot == ItemSlot.Boots)
            CreateBootPair(key, Bone("lowerleg.l", new Vector3(-.17f, .29f, 0f)), Bone("foot.l", new Vector3(-.17f, .15f, 0f)),
                Bone("lowerleg.r", new Vector3(.17f, .29f, 0f)), Bone("foot.r", new Vector3(.17f, .15f, 0f)));
        else
            CreateBelt(key, Bone("hips", new Vector3(0f, .4f, 0f)), Vector3.forward);
        return root;
    }

    static void Link(GameObject owner, GameObject follower)
    {
        if (owner == null || follower == null) return;
        owner.AddComponent<GearLink>().follower = follower;
    }

    static Transform LimbRoot(string name, Transform bone)
    {
        var root = new GameObject(name).transform;
        root.SetParent(bone, false);
        return root;
    }

    /// <summary>Elliptic ring in the XZ plane at height y, built from 10 beams.</summary>
    static void Ring(Transform root, string name, Color color, float rx, float rz, float y, float thickness)
    {
        const int segments = 10;
        for (int i = 0; i < segments; i++)
        {
            float a = Mathf.PI * 2f * i / segments;
            float b = Mathf.PI * 2f * (i + 1) / segments;
            Beam(root, name, color, new Vector3(rx * Mathf.Cos(a), y, rz * Mathf.Sin(a)),
                new Vector3(rx * Mathf.Cos(b), y, rz * Mathf.Sin(b)), thickness);
        }
    }

    static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();
    static Mesh cube;
    static Mesh Cube => cube != null ? cube : cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
    static readonly int ColorProperty = Shader.PropertyToID("_BaseColor");

    public static bool IsKnown(string id, ItemSlot slot) =>
        id != null && Models.TryGetValue(id, out var model) && model.Slot == slot;

    public static IEnumerable<string> Ids => Models.Keys;

    /// <summary>Unique bows held in the left hand and drawn like the starter bow.</summary>
    public static bool IsBowUnique(string id) => id == ItemDatabase.NightfangId || id == ItemDatabase.GlassthornBowId;

    /// <summary>Unique lanterns that hang straight down from the hand.</summary>
    public static bool IsHangingLantern(string id) =>
        id == "Lantern of the Hold" || id == ItemDatabase.WickOfHaloId || id == ItemDatabase.OssuaryThuribleId;

    public static GameObject Create(string id, ItemSlot slot, Transform parent)
    {
        if (!IsKnown(id, slot)) return null;
        var root = new GameObject(id);
        root.transform.SetParent(parent, false);
        if (slot == ItemSlot.Helmet)
        {
            root.transform.localPosition = new Vector3(0f, .47f, 0f);
            root.transform.localScale = new Vector3(1.85f, 1f, 1.85f);
        }
        Models[id].Build(root.transform);
        return root;
    }

    public static void StarterArmor(Transform root, string classId)
    {
        Plate(root, "Yellow chest guard", Brass, .065f, .36f, -.33f,.13f, .33f,.13f, .27f,-.18f, 0,-.28f, -.27f,-.18f);
        var trim = classId == ClassCatalog.RogueId ? Lilac : classId == ClassCatalog.LanternbearerId ? Pearl : Iron;
        Box(root, "Guard clasp", trim, new Vector3(0,-.06f,.405f), new Vector3(.12f,.09f,.04f));
    }

    static void Grip(Transform root, float length = .22f)
    {
        Beam(root, "Grip", Dark, new Vector3(0,-.13f,0), new Vector3(0,length,0), .065f);
        Box(root, "Guard", Brass, new Vector3(0,.13f,0), new Vector3(.27f,.05f,.09f));
    }

    static void Band(Transform root, Color color, float radius, float y, float width)
    {
        for (int i = 0; i < 8; i++)
        {
            float a = Mathf.PI * i / 4f;
            float b = Mathf.PI * (i + 1) / 4f;
            Beam(root, "Band", color, new Vector3(radius*Mathf.Cos(a),y,width*.5f*Mathf.Sin(a)),
                new Vector3(radius*Mathf.Cos(b),y,width*.5f*Mathf.Sin(b)), .065f);
        }
    }

    static void Box(Transform root, string name, Color color, Vector3 center, Vector3 size)
    {
        var part = Part(root, name, color, Cube);
        part.localPosition = center;
        part.localScale = size;
    }

    static void Beam(Transform root, string name, Color color, Vector3 a, Vector3 b, float width)
    {
        var part = Part(root, name, color, Cube);
        part.localPosition = (a + b) * .5f;
        part.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
        part.localScale = new Vector3(width, Vector3.Distance(a, b), width);
    }

    static void Crystal(Transform root, string name, Color color, Vector3 center, Vector3 size)
    {
        Plate(root, name, color, size.z, center.z, center.x-size.x/2,center.y, center.x,center.y+size.y/2,
            center.x+size.x/2,center.y, center.x,center.y-size.y/2);
    }

    // A faceted outline with solid side walls and no imported mesh dependencies.
    static void Plate(Transform root, string name, Color color, float depth, float z, params float[] points)
    {
        int count = points.Length / 2;
        var vertices = new Vector3[count * 2];
        for (int i = 0; i < count; i++)
        {
            vertices[i] = new Vector3(points[i * 2], points[i * 2 + 1], z + depth / 2);
            vertices[i + count] = vertices[i] - Vector3.forward * depth;
        }
        var perimeter = new List<int>(count);
        float area = 0f;
        for (int i = 0; i < count; i++)
        {
            perimeter.Add(i);
            var a = vertices[i];
            var b = vertices[(i + 1) % count];
            area += a.x * b.y - b.x * a.y;
        }
        if (area < 0f) perimeter.Reverse();

        var faces = new List<Vector3>();
        for (int i = 0; i < count; i++)
        {
            int a = perimeter[i];
            int b = perimeter[(i + 1) % count];
            faces.AddRange(new[] { vertices[a], vertices[a + count], vertices[b],
                vertices[b], vertices[a + count], vertices[b + count] });
        }

        // Clip convex ears so the notches remain open on concave silhouettes.
        while (perimeter.Count >= 3)
        {
            bool clipped = false;
            for (int i = 0; i < perimeter.Count; i++)
            {
                int a = perimeter[(i + perimeter.Count - 1) % perimeter.Count];
                int b = perimeter[i];
                int c = perimeter[(i + 1) % perimeter.Count];
                if (Turn(vertices[a], vertices[b], vertices[c]) <= .000001f) continue;
                bool contains = perimeter.Exists(j => j != a && j != b && j != c &&
                    Turn(vertices[a], vertices[b], vertices[j]) >= 0f &&
                    Turn(vertices[b], vertices[c], vertices[j]) >= 0f &&
                    Turn(vertices[c], vertices[a], vertices[j]) >= 0f);
                if (contains) continue;
                faces.AddRange(new[] { vertices[a], vertices[b], vertices[c],
                    vertices[a + count], vertices[c + count], vertices[b + count] });
                perimeter.RemoveAt(i);
                clipped = true;
                break;
            }
            if (!clipped) throw new InvalidOperationException($"Invalid gear outline: {name}.");
        }
        var indices = new int[faces.Count];
        for (int i = 0; i < indices.Length; i++) indices[i] = i;
        var mesh = new Mesh { name = name, vertices = faces.ToArray(), triangles = indices };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        Part(root, name, color, mesh).gameObject.AddComponent<GeneratedGearMesh>().Mesh = mesh;
    }

    static float Turn(Vector3 a, Vector3 b, Vector3 c) =>
        (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);

    static Transform Part(Transform root, string name, Color color, Mesh mesh)
    {
        var part = new GameObject(name);
        part.transform.SetParent(root, false);
        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        if (!Materials.TryGetValue(color, out var material) || material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null) // dedicated server builds ship no shaders
            {
                material = new Material(shader);
                material.SetColor(ColorProperty, color);
                Materials[color] = material;
            }
        }
        part.AddComponent<MeshRenderer>().sharedMaterial = material;
        return part.transform;
    }
}

/// <summary>
/// Keeps a split gear piece together (gear-slots S3): the right glove or the
/// shoe on the foot bone lives on another bone, so it follows the owner's
/// visibility and is destroyed with it.
/// </summary>
public sealed class GearLink : MonoBehaviour
{
    public GameObject follower;

    void OnEnable() { if (follower != null) follower.SetActive(true); }
    void OnDisable() { if (follower != null) follower.SetActive(false); }
    void OnDestroy() { if (follower != null && Application.isPlaying) Destroy(follower); }
}
