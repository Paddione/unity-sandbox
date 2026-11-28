using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hold figures look their own (cinderreach-town-npc-identity): every named
/// NPC gets its own KayKit body and a tint, so no two NPCs, and no NPC and a
/// hero, read the same. <c>TownBuilder</c> picks the body from here;
/// <see cref="NpcLook"/> applies the tint at runtime.
/// </summary>
public static class NpcAppearance
{
    public readonly struct Look
    {
        public readonly string model;   // file under Adventurers/Characters/
        public readonly Color tint;
        public Look(string model, Color tint) { this.model = model; this.tint = tint; }
    }

    static readonly Dictionary<string, Look> looks = new Dictionary<string, Look>
    {
        { StoryIds.Ilsabe, new Look("Rogue_Hooded.fbx", new Color(0.78f, 0.55f, 0.85f)) },  // trader in plum
        { StoryIds.Ottilie, new Look("Mage.fbx", new Color(0.55f, 0.80f, 0.55f)) },        // moss-green seer
        { StoryIds.Merle, new Look("Rogue.fbx", new Color(0.80f, 0.72f, 0.60f)) },         // ash-dusted carter
        { StoryIds.Jorin, new Look("Barbarian.fbx", new Color(0.70f, 0.62f, 0.58f)) },     // soot-stained smith
    };

    public static IEnumerable<string> NpcIds => looks.Keys;

    public static bool TryGet(string npcId, out Look look) => looks.TryGetValue(npcId ?? "", out look);
}
