using System.Collections.Generic;
using UnityEngine;

namespace Rival.Enemy
{
    /// <summary>
    /// An ordered list of rival archetypes. The index is the selection order;
    /// a future menu picks roster[index] and RivalMatchSetup re-configures the
    /// rival on the fly. Keeps all rival identities in one data asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Rival/Enemy/Rival Roster", fileName = "RivalRoster")]
    public class RivalRoster : ScriptableObject
    {
        [Tooltip("First entry is index 0. Each archetype needs a unique ActorName.")]
        public List<RivalArchetype> Archetypes = new List<RivalArchetype>();
    }
}
