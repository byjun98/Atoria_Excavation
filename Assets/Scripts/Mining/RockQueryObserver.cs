using UnityEngine;

namespace Rebuild.Mining
{

    [DisallowMultipleComponent]
    internal sealed class RockQueryObserver : MonoBehaviour
    {
        void OnEnable() => RockQuery.Invalidate();
        void OnDisable() => RockQuery.Invalidate();
        void OnDestroy() => RockQuery.Invalidate();
    }
}
