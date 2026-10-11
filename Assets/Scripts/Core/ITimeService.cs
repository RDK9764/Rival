namespace Rival.Core
{
    /// <summary>
    /// Controls gameplay time (pause, future slow-motion and hit-stop).
    /// Gameplay systems depend on this interface instead of touching
    /// Time.timeScale directly, so there is exactly one owner of game time.
    /// </summary>
    public interface ITimeService
    {
        float Scale { get; }
        void SetScale(float scale);
    }
}
