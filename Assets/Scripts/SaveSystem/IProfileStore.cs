using Rival.BehaviorAnalysis;

namespace Rival.SaveSystem
{
    /// <summary>
    /// Persistence contract for learned behavior profiles. The Rival brain asks
    /// for a seed profile through this interface so it never touches file I/O.
    /// </summary>
    public interface IProfileStore
    {
        /// <summary>True if a stored profile was found and loaded.</summary>
        bool TryLoad(string actor, out BehaviorProfile profile);

        void Save(string actor, BehaviorProfile profile);
    }
}
