using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>Shotcheck part: Zone map overlay and fullscreen (-map-shots).</summary>
public sealed partial class DiabloShotCheck
{
    IEnumerator MapShots(DiabloClickController hero)
    {
        var story = RunProfile.Story;
        story.Refresh(RunProfile.Act);
        story.Accept("h1");
        story.Refresh(RunProfile.Act);
        story.Accept("n1");
        yield return Wait(1.2f);
        int marks = hero.GetComponent<StoryDirector>().Marks.Count;
        Log($"MAP marks={marks} minimapIcons={hero.GetComponent<MinimapHud>().IconsShown} renders={ZoneMap.Renders}");
        yield return Shot("map_minimap_tracker");

        var map = hero.GetComponent<WorldMapUI>();
        bool savedMapFullscreen = DiabloSettings.MapFullscreen;
        DiabloSettings.MapFullscreen = false;
        map.SetOpen(true);
        yield return Wait(0.6f);
        Log($"MAP zone={map.ViewZone} icons={map.IconsShown} mode={map.Mode} zoom={map.Zoom}");
        yield return Shot("map_overlay");
        yield return Shot("map_zone_fen");
        map.ToggleMode();
        yield return Wait(0.6f);
        Log($"MAP mode={map.Mode} backing={map.BackingAlpha}");
        yield return Shot("map_fullscreen");
        map.Cycle(1);
        yield return Wait(0.6f);
        yield return Shot("map_zone_ossuary");
        map.SetOpen(false);
        DiabloSettings.MapFullscreen = savedMapFullscreen;
        yield return Wait(0.3f);

        StoryDialogShare.Receive(99, "Alrik", SharedDialogOp.Open, "h1.offer", StoryIds.Ottilie, "0,1,2,3", 1);
        yield return Wait(0.5f);
        yield return Shot("dialog_shared_listener");
        StoryDialogShare.Receive(99, "Alrik", SharedDialogOp.Close, "h1.offer", StoryIds.Ottilie, "", 1);
        yield return Wait(0.3f);

        var log = hero.GetComponent<StoryQuestUI>();
        log.SetOpen(true);
        yield return Wait(0.5f);
        yield return Shot("quest_log_tracking");
        log.SetOpen(false);
    }
}
