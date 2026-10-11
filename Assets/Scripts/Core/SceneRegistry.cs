namespace Rival.Core
{
    /// <summary>
    /// Central scene names so gameplay code never uses magic strings.
    /// Keep these in sync with the scenes in Build Settings.
    /// </summary>
    public static class SceneRegistry
    {
        public const string Boot = "Boot";
        public const string MainMenu = "MainMenu";
        public const string Gameplay = "Gameplay";
    }
}
