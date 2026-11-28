using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>cinderreach-town-npc-identity D2: figures turn to a hero within 6 m and back to rest when alone.</summary>
public sealed class NpcFacingPlayTests
{
    GameObject npc, hero;

    [TearDown]
    public void TearDown()
    {
        if (npc != null) Object.Destroy(npc);
        if (hero != null) Object.Destroy(hero);
    }

    [UnityTest]
    public IEnumerator TurnsToHeroInReach_ReturnsToRestOutside()
    {
        npc = new GameObject("FacingNpc");
        npc.transform.rotation = Quaternion.identity; // rest: looking +Z
        var facing = npc.AddComponent<NpcFacing>();
        npc.AddComponent<NpcLook>().npcId = StoryIds.Ilsabe;
        hero = new GameObject(DiabloNetworkPlayer.HeroName);
        hero.transform.position = new Vector3(4f, 0f, 0f); // 4 m east, inside the 6 m reach
        yield return DiabloTestFixture.WaitSeconds(0.5f);
        float toHero = Vector3.Angle(npc.transform.forward, Vector3.right);
        Assert.Less(toHero, 15f, $"turned toward the hero (off by {toHero:0.0}°)");
        Assert.AreEqual(hero.transform, facing.Target);

        hero.transform.position = new Vector3(20f, 0f, 0f); // out of reach
        yield return DiabloTestFixture.WaitSeconds(0.6f);
        Assert.IsNull(facing.Target);
        Assert.Less(Vector3.Angle(npc.transform.forward, Vector3.forward), 15f, "back to its rest pose");
    }

    [UnityTest]
    public IEnumerator Look_AppliesCatalogTint()
    {
        npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        var look = npc.AddComponent<NpcLook>();
        look.npcId = StoryIds.Jorin;
        look.Apply();
        NpcAppearance.TryGet(StoryIds.Jorin, out var expected);
        Assert.AreEqual(expected.tint, look.AppliedTint);
        var block = new MaterialPropertyBlock();
        npc.GetComponent<Renderer>().GetPropertyBlock(block);
        Assert.AreEqual(expected.tint, block.GetColor("_BaseColor"));
        yield return null;
    }
}
