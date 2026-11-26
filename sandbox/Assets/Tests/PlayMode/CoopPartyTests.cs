using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// cinderreach-party-roles S5: co-op party mapping, heal validation and join
/// gate (pure mapping; delivery is proven by the 2-process pair run).
/// </summary>
public class CoopPartyTests
{
    [Test]
    public void ReconcileRoles_StepsAsideOnePerJoiningRole()
    {
        var ai = new List<ClassDefinition.PartyRole>
            { ClassDefinition.PartyRole.Healer, ClassDefinition.PartyRole.Dps };
        var gone = PartyRoster.ReconcileRoles(ai,
            new List<ClassDefinition.PartyRole> { ClassDefinition.PartyRole.Healer });
        CollectionAssert.AreEqual(new[] { ClassDefinition.PartyRole.Healer }, gone);
        Assert.IsEmpty(PartyRoster.ReconcileRoles(ai, new List<ClassDefinition.PartyRole>()));
        Assert.IsEmpty(PartyRoster.ReconcileRoles(ai,
            new List<ClassDefinition.PartyRole> { ClassDefinition.PartyRole.Tank }));
    }

    [Test]
    public void ReconcileRoles_DuplicateJoinsRemoveOne()
    {
        var ai = new List<ClassDefinition.PartyRole> { ClassDefinition.PartyRole.Healer };
        var gone = PartyRoster.ReconcileRoles(ai,
            new List<ClassDefinition.PartyRole> { ClassDefinition.PartyRole.Healer, ClassDefinition.PartyRole.Healer });
        CollectionAssert.AreEqual(new[] { ClassDefinition.PartyRole.Healer }, gone);
    }

    [Test]
    public void NeedsJoinGate_OverflowsBeyondThree()
    {
        Assert.IsFalse(PartyRoster.NeedsJoinGate(1));
        Assert.IsFalse(PartyRoster.NeedsJoinGate(3));
        Assert.IsTrue(PartyRoster.NeedsJoinGate(4));
    }

    [Test]
    public void RemoteRoles_WithoutNetworkIsEmpty()
    {
        Assert.IsEmpty(PartyRoster.RemoteRoles(null));
    }

    [Test]
    public void ValidateHeal_AcceptsAndRejects()
    {
        Vector3 healer = Vector3.zero;
        Vector3 near = new Vector3(5f, 0f, 0f);
        Vector3 far = new Vector3(20f, 0f, 0f);
        Assert.IsNull(HitValidator.ValidateHeal(healer, near, 12f, float.NegativeInfinity, 5f, 100f, 12f));
        Assert.IsNotNull(HitValidator.ValidateHeal(healer, far, 12f, float.NegativeInfinity, 5f, 100f, 12f), "range");
        Assert.IsNotNull(HitValidator.ValidateHeal(healer, near, 12f, 99f, 5f, 100f, 12f), "cooldown");
        Assert.IsNotNull(HitValidator.ValidateHeal(healer, near, 12f, float.NegativeInfinity, 5f, 100f, 0f), "amount");
        Assert.IsNotNull(HitValidator.ValidateHeal(healer, near, 12f, float.NegativeInfinity, 5f, 100f, 31f), "amount cap");
        Assert.IsNull(HitValidator.ValidateHeal(healer, near, 6f, float.NegativeInfinity, 12f, 100f, 8f), "group radius");
        Assert.IsNotNull(HitValidator.ValidateHeal(healer, new Vector3(7f, 0f, 0f), 6f, float.NegativeInfinity, 12f, 100f, 8f), "group range");
    }

    [Test]
    public void IsCastSkillId_CoversTheWholeEnum()
    {
        Assert.IsTrue(DiabloNetworkPlayer.IsCastSkillId(0));
        Assert.IsTrue(DiabloNetworkPlayer.IsCastSkillId((int)DiabloSkills.Skill.Cleave));
        Assert.IsTrue(DiabloNetworkPlayer.IsCastSkillId((int)DiabloSkills.Skill.SeismicSlam));
        Assert.IsTrue(DiabloNetworkPlayer.IsCastSkillId((int)DiabloSkills.Skill.RenewingCircle));
        Assert.IsTrue(DiabloNetworkPlayer.IsCastSkillId((int)DiabloSkills.Skill.PowderKeg), "class-kits-parity appends 12-19");
        Assert.IsFalse(DiabloNetworkPlayer.IsCastSkillId(20));
        Assert.IsFalse(DiabloNetworkPlayer.IsCastSkillId(-1));
    }

    [Test]
    public void ClassRoles_CoverTankHealerDps()
    {
        var roles = new HashSet<ClassDefinition.PartyRole>();
        foreach (var c in ClassCatalog.All) roles.Add(c.role);
        Assert.AreEqual(3, roles.Count);
        Assert.AreEqual(ClassDefinition.PartyRole.Tank, ClassCatalog.Fenwalker.role);
        Assert.AreEqual(ClassDefinition.PartyRole.Healer, ClassCatalog.Lanternbearer.role);
        Assert.AreEqual(ClassDefinition.PartyRole.Dps, ClassCatalog.Rogue.role);
    }

    [UnityTest]
    public IEnumerator HealRelay_SoloNeedsNoNetwork()
    {
        // Solo healers relay nothing: the local apply stands on its own.
        RunProfile.Reset();
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        var healer = new GameObject("SoloHealer");
        healer.transform.position = Vector3.zero;
        healer.AddComponent<DiabloProgression>();
        healer.AddComponent<PlayerHealth>();
        var skills = healer.AddComponent<DiabloSkills>();
        var ally = new GameObject("SoloAlly");
        ally.transform.position = new Vector3(0f, 0f, 3f);
        var allyHp = ally.AddComponent<PlayerHealth>();
        allyHp.TakeDamage(10f);
        float restored = skills.DoTargetHeal(cosmetic: false);
        Assert.AreEqual(10f, restored, 0.0001f);
        Assert.AreEqual(allyHp.MaxHP, allyHp.CurrentHP, 0.0001f);
        Object.DestroyImmediate(healer);
        Object.DestroyImmediate(ally);
        RunProfile.Reset();
        yield return null;
    }
}
