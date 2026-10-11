namespace Rival.Core
{
    /// <summary>How the gameplay scene should run the fight.</summary>
    public enum RivalGameMode
    {
        /// <summary>Best-of-N rounds against one rival (default).</summary>
        Match,
        /// <summary>Escalating endless waves of rivals.</summary>
        Endless
    }

    /// <summary>
    /// Lightweight data contract that survives scene transitions. The main menu
    /// writes the selected archetype index and game mode; the gameplay scene
    /// reads them. Lives on a persistent GameObject in the Boot scene so it is
    /// always available.
    /// </summary>
    public interface IMatchConfig
    {
        int SelectedIndex { get; set; }
        RivalGameMode GameMode { get; set; }
    }
}