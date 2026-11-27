using UnityEngine;

/// <summary>
/// A story find (cinderreach-deutsch-story S5): walking into it books the
/// collect objective. It only despawns once an active task actually counted
/// it, so finds reached early wait instead of being lost.
/// </summary>
public sealed class StoryPickup : MonoBehaviour
{
    public string itemId;

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        var hero = other.gameObject;
        if (hero.GetComponent<DiabloProgression>() == null) return;
        if (DiabloUIFactory.IsRemoteMirror(hero.GetComponent<DiabloProgression>())) return;
        // cinderreach-team-autoplay K2: a co-op client's find also books on the host/server.
        StoryDirector.ForwardToHost(StoryDirector.StoryEventCollect, itemId, hero);
        if (!StoryDirector.BookCollect(itemId)) return;
        var director = hero.GetComponent<StoryDirector>();
        if (director != null) director.Toast(Loc.F("story.pickup", Loc.T("story.item." + itemId)));
        gameObject.SetActive(false);
    }
}
