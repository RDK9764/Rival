namespace Rival.SaveSystem
{
    /// <summary>
    /// Persistence contract for the player's loadout. Callers depend on this
    /// interface, never on file I/O. Mirrors IProfileStore's minimal surface.
    /// </summary>
    public interface ILoadoutStore
    {
        LoadoutData Load();
        void Save(LoadoutData data);
    }
}
