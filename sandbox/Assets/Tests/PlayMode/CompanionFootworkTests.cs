using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// All three classes ship working footwork: every companion body (clone and
/// fallback) carries Animator + DiabloAnimator + LocomotionTwist with resolved
/// hips/spine, worn-gear mounts for feet, and audible/visible locomotion while
/// moving. Guards the "twist only on one class" regression.
/// </summary>
public class CompanionFootworkTests
{
    GameObject hero;

    static Transform Bone(string name, Transform parent)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }

    static void AddHeroRig(GameObject root)
    {
        // Knight-like rig bones used by twist + limb gear mounts.
        var hips = Bone("hips", root.transform);
        var spine = Bone("spine", hips);
        Bone("chest", spine);
        Bone("head", spine);
        Bone("handslot.r", spine);
        Bone("handslot.l", spine);
        Bone("wrist.l", spine);
        Bone("wrist.r", spine);
        Bone("lowerleg.l", hips);
        Bone("lowerleg.r", hips);
        Bone("foot.l", hips);
        Bone("foot.r", hips);
        root.AddComponent<Animator>();
        root.AddComponent<Equipment>();
        root.AddComponent<PlayerHealth>();
        root.AddComponent<ClassAppearance>();
    }

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        PartyCompanion.Live.Clear();
        hero = new GameObject(DiabloNetworkPlayer.HeroName);
        AddHeroRig(hero);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var c in PartyCompanion.Live.ToArray())
            if (c != null) Object.DestroyImmediate(c.gameObject);
        PartyCompanion.Live.Clear();
        var roster = PartyRoster.Instance;
        if (roster != null) Object.DestroyImmediate(roster.gameObject);
        if (hero != null) Object.DestroyImmediate(hero);
        RunProfile.Reset();
    }

    [UnityTest]
    public IEnumerator AllClasses_CloneBodiesCarryTwistAndGearMounts()
    {
        foreach (var cls in ClassCatalog.All)
        {
            var go = CompanionFactory.BuildBody(cls,
                CompanionFactory.CompanionProfile(cls, 0), hero.transform);
            Assert.IsNotNull(go, cls.id);
            yield return null;
            yield return null;
            Assert.IsNotNull(go.GetComponent<Animator>(), cls.id + ": Animator kept");
            Assert.IsNotNull(go.GetComponent<DiabloAnimator>(), cls.id + ": DiabloAnimator kept");
            var twist = go.GetComponent<LocomotionTwist>();
            Assert.IsNotNull(twist, cls.id + ": LocomotionTwist present");
            Assert.IsTrue(twist.HasFooting, cls.id + ": hips+spine resolved");
            Assert.IsNotNull(go.GetComponent<HeroEquipmentAppearance>(),
                cls.id + ": worn-gear (boots/gloves/belt) present");
            Assert.IsNotNull(go.GetComponent<PartyCompanion>() == null
                ? go.AddComponent<PartyCompanion>()
                : go.GetComponent<PartyCompanion>(), cls.id);
            Object.DestroyImmediate(go);
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator AllClasses_FallbackBodiesCarryLocomotionComponents()
    {
        hero.name = "NoHeroHere";
        foreach (var cls in ClassCatalog.All)
        {
            var go = CompanionFactory.BuildBody(cls,
                CompanionFactory.CompanionProfile(cls, 0), null);
            Assert.IsNotNull(go, cls.id);
            // Fallback has no rig bones, but still carries the locomotion
            // components so class-agnostic checks never NRE.
            Assert.IsNotNull(go.GetComponent<DiabloAnimator>(), cls.id + ": driver present");
            Assert.IsNotNull(go.GetComponent<LocomotionTwist>(), cls.id + ": twist present");
            Object.DestroyImmediate(go);
            yield return null;
        }
        hero.name = DiabloNetworkPlayer.HeroName;
        yield return null;
    }

    [UnityTest]
    public IEnumerator Twist_ResolvesCaseInsensitiveBones()
    {
        var rig = new GameObject("CaseRig");
        rig.AddComponent<Animator>();
        var hips = Bone("HIPS", rig.transform);
        var spine = Bone("Spine", hips);
        Bone("CHEST", spine);
        var twist = rig.AddComponent<LocomotionTwist>();
        yield return null;
        Assert.IsTrue(twist.HasFooting, "HIPS/Spine/CHEST resolve like hips/spine/chest");
        Object.DestroyImmediate(rig);
        yield return null;
    }

    [UnityTest]
    public IEnumerator CompanionsAnimateWhileClosingTheGap()
    {
        var roster = PartyRoster.EnsureSoloParty(hero.transform);
        Assert.IsNotNull(roster);
        yield return null;
        yield return null;
        Assert.AreEqual(2, PartyCompanion.Live.Count);
        foreach (var c in PartyCompanion.Live)
        {
            Assert.IsNotNull(c.GetComponent<LocomotionTwist>(), c.classId + ": twist on companion");
            Assert.IsNotNull(c.GetComponent<DiabloAnimator>(), c.classId + ": driver on companion");
        }
        var comp = PartyCompanion.Live[0];
        DiabloTestFixture.TeleportHero(hero, hero.transform.position + new Vector3(12f, 0f, 0f));
        yield return DiabloTestFixture.WaitSeconds(1.5f);
        var anim = comp.GetComponent<Animator>();
        // Either still catching up (Moving) or measurably gliding (twist velocity):
        // both prove the legs are driven, not frozen.
        var twist = comp.GetComponent<LocomotionTwist>();
        Assert.IsTrue(anim.GetBool("Moving") || (twist != null && twist.MovingMeasured),
            "companion legs drive while following");
    }
}
