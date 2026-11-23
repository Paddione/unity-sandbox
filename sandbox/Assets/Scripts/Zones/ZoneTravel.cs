using UnityEngine;

/// <summary>
/// One teleport path for zone gates and waypoint shrines (act-one S5):
/// CharacterController off, move, on. Co-op replication of this step lives
/// in cinderreach-coop-session S3 and only needs to hook here. AI companions
/// of the travelling leader port along (<see cref="PartyRoster.PortWithLeader"/>).
/// </summary>
public static class ZoneTravel
{
    public static int Travels;

    public static void TeleportHero(GameObject hero, Vector3 destination)
    {
        if (hero == null) return;
        var cc = hero.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        hero.transform.position = destination;
        if (cc != null) cc.enabled = true;
        PartyRoster.PortWithLeader(hero, destination);
        Travels++;
    }

    public static void ResetRun() => Travels = 0;
}
