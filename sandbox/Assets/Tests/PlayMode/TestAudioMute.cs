using NUnit.Framework;

/// <summary>Global test mute: PlayMode suites synthesize real clips through
/// DiabloAudio/MusicDirector, which beeps on any machine with audio output.
/// Counters and clip asserts still run — only the device stays silent.
/// Reset in OneTimeTearDown so normal Editor play keeps sound.</summary>
[SetUpFixture]
public class TestAudioMute
{
    [OneTimeSetUp]
    public void Mute()
    {
        DiabloAudio.MutePlayback = true;
    }

    [OneTimeTearDown]
    public void Unmute()
    {
        DiabloAudio.MutePlayback = false;
    }
}
