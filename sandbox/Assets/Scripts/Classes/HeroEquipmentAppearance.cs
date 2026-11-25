using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Worn-gear art on top of the class body (<see cref="ClassAppearance"/>):
/// visible Unique models per slot and the Fenwalker's starter chest guard.
/// The body itself stays the class+gender model, so classes keep distinct
/// silhouettes.
/// </summary>
public sealed class HeroEquipmentAppearance : MonoBehaviour
{
    /// <summary>Bone mounts this component adds (clones strip them, see CompanionFactory).</summary>
    public static readonly string[] MountNames =
    {
        "Chest art", "Head art", "Right hand art", "Left hand art",
        // gear-slots S3: bone-space mounts (identity under the bone) for hands, feet and belt.
        "Left wrist art", "Right wrist art", "Left shin art", "Right shin art",
        "Left foot art", "Right foot art", "Hips art",
    };

    Equipment equipment;
    ClassAppearance appearance;
    DiabloNetworkPlayer networkPlayer;
    DiabloSkills skills;
    Transform chest;
    Transform head;
    Transform right;
    Transform left;
    Transform sword;
    SkinnedMeshRenderer[] baseSkin;
    GameObject starterGuard;
    GameObject armor;
    GameObject helmet;
    GameObject weapon;
    string armorId;
    string helmetId;
    string weaponId;
    string classId;
    Transform wristL, wristR, shinL, shinR, footL, footR, hips;
    Vector3 hipsForward = Vector3.forward;
    GameObject gloves;
    GameObject boots;
    GameObject belt;
    string glovesKey;
    string bootsKey;
    string beltKey;

    /// <summary>Worn hands/feet/belt art keys (test seam, null when bare).</summary>
    public string GlovesKey => gloves != null ? glovesKey : null;
    public string BootsKey => boots != null ? bootsKey : null;
    public string BeltKey => belt != null ? beltKey : null;

    /// <param name="source">Equipment to mirror; defaults to this object's own
    /// (the inventory paperdoll passes the live hero's).</param>
    public bool Initialize(ClassAppearance owner, Equipment source = null)
    {
        appearance = owner;
        equipment = source != null ? source : GetComponent<Equipment>();
        networkPlayer = GetComponent<DiabloNetworkPlayer>();
        skills = GetComponent<DiabloSkills>();
        var bones = new Dictionary<string, Transform>();
        foreach (var child in GetComponentsInChildren<Transform>(true))
            bones.TryAdd(child.name, child);
        if (equipment == null || !bones.TryGetValue("chest", out var chestBone) ||
            !bones.TryGetValue("head", out var headBone) || !bones.TryGetValue("handslot.r", out var rightBone) ||
            !bones.TryGetValue("handslot.l", out var leftBone))
            return false;

        baseSkin = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        chest = Mount(MountNames[0], chestBone);
        head = Mount(MountNames[1], headBone);
        right = Mount(MountNames[2], rightBone);
        left = Mount(MountNames[3], leftBone);
        bones.TryGetValue(DiabloSkills.SwordName, out sword);
        // gear-slots S3: optional limb mounts; rigs without them simply show no limb gear.
        wristL = BoneMount(bones, "wrist.l", MountNames[4]);
        wristR = BoneMount(bones, "wrist.r", MountNames[5]);
        shinL = BoneMount(bones, "lowerleg.l", MountNames[6]);
        shinR = BoneMount(bones, "lowerleg.r", MountNames[7]);
        footL = BoneMount(bones, "foot.l", MountNames[8]);
        footR = BoneMount(bones, "foot.r", MountNames[9]);
        hips = BoneMount(bones, "hips", MountNames[10]);
        if (hips != null) hipsForward = hips.InverseTransformDirection(transform.forward);
        return true;
    }

    Transform Mount(string name, Transform bone)
    {
        var anchor = new GameObject(name).transform;
        anchor.SetParent(bone, false);
        // Author gear facing forward in character space, then convert to the rig's bind axes.
        foreach (var skin in baseSkin)
        {
            int index = System.Array.IndexOf(skin.bones, bone);
            if (index < 0 || index >= skin.sharedMesh.bindposes.Length) continue;
            var bind = skin.sharedMesh.bindposes[index] * skin.transform.worldToLocalMatrix * transform.localToWorldMatrix;
            anchor.localRotation = bind.rotation;
            anchor.localScale = bind.lossyScale;
            return anchor;
        }
        anchor.rotation = transform.rotation;
        anchor.localScale = Vector3.one / bone.lossyScale.x;
        return anchor;
    }

    static Transform BoneMount(Dictionary<string, Transform> bones, string bone, string name)
    {
        if (!bones.TryGetValue(bone, out var b)) return null;
        var anchor = new GameObject(name).transform;
        anchor.SetParent(b, false);
        return anchor;
    }

    static string Id(ItemInstance item) => item?.rarity == ItemRarity.Unique ? item.uniqueId : null;

    void LateUpdate()
    {
        if (equipment == null || chest == null) return;
        if (classId != appearance.AppliedClassId)
        {
            classId = appearance.AppliedClassId;
            if (starterGuard != null)
            {
                starterGuard.SetActive(false);
                Destroy(starterGuard);
            }
            starterGuard = new GameObject("Yellow starter armor");
            starterGuard.transform.SetParent(chest, false);
            HeroGearModels.StarterArmor(starterGuard.transform, classId);
        }

        bool mirror = networkPlayer != null && networkPlayer.IsSpawned && !networkPlayer.IsOwner;
        string nextArmor = mirror ? networkPlayer.visualArmor.Value.ToString() : Id(equipment.armor);
        string nextHelmet = mirror ? networkPlayer.visualHelmet.Value.ToString() : Id(equipment.helmet);
        string nextWeapon = mirror ? networkPlayer.visualWeapon.Value.ToString() : Id(equipment.weapon);
        Replace(ref armor, ref armorId, nextArmor, ItemSlot.Armor, chest);
        Replace(ref helmet, ref helmetId, nextHelmet, ItemSlot.Helmet, head);
        Replace(ref weapon, ref weaponId, nextWeapon, ItemSlot.Weapon,
            nextWeapon == ItemDatabase.NightfangId ? left : right);
        string nextGloves = mirror ? networkPlayer.visualGloves.Value.ToString() : HeroGearModels.VisualKey(equipment.gloves);
        string nextBoots = mirror ? networkPlayer.visualBoots.Value.ToString() : HeroGearModels.VisualKey(equipment.boots);
        string nextBelt = mirror ? networkPlayer.visualBelt.Value.ToString() : HeroGearModels.VisualKey(equipment.belt);
        if (Swap(ref gloves, ref glovesKey, nextGloves) && wristL != null && wristR != null)
            gloves = HeroGearModels.CreateGlovePair(nextGloves, wristL, wristR);
        if (Swap(ref boots, ref bootsKey, nextBoots) && shinL != null && footL != null && shinR != null && footR != null)
            boots = HeroGearModels.CreateBootPair(nextBoots, shinL, footL, shinR, footR);
        if (Swap(ref belt, ref beltKey, nextBelt) && hips != null)
            belt = HeroGearModels.CreateBelt(nextBelt, hips, hipsForward);

        bool lanternbearer = classId == ClassCatalog.LanternbearerId;
        bool bowDrawn = skills != null && skills.BowDrawn;
        // Brass plate is the tank's starter look; robe and leathers stay bare.
        starterGuard.SetActive(armor == null && classId == ClassCatalog.FenwalkerId);
        if (sword != null) sword.gameObject.SetActive(weapon == null && !lanternbearer && !bowDrawn);
        appearance.SetStarterLanternVisible(lanternbearer && weapon == null);
        if (skills != null) skills.SetStarterBowModelVisible(weaponId != ItemDatabase.NightfangId);
        if (weapon != null)
        {
            bool uniqueBow = weaponId == ItemDatabase.NightfangId;
            bool hangingLantern = weaponId == "Lantern of the Hold" || weaponId == ItemDatabase.WickOfHaloId;
            if (hangingLantern || (uniqueBow && !bowDrawn))
                weapon.transform.rotation = transform.rotation;
            else
                weapon.transform.localRotation = Quaternion.identity;
            weapon.SetActive(!bowDrawn || uniqueBow || lanternbearer);
        }
    }

    /// <summary>Drop the old limb model when the key changed; true when a new one should be built.</summary>
    static bool Swap(ref GameObject model, ref string previous, string key)
    {
        if (string.IsNullOrEmpty(key)) key = null;
        if (previous == key) return false;
        if (model != null)
        {
            model.SetActive(false);
            Destroy(model);
            model = null;
        }
        previous = key;
        return key != null;
    }

    static void Replace(ref GameObject model, ref string previous, string id, ItemSlot slot, Transform mount)
    {
        if (previous == id) return;
        if (model != null)
        {
            model.SetActive(false);
            Destroy(model);
        }
        previous = id;
        model = HeroGearModels.Create(id, slot, mount);
    }
}
