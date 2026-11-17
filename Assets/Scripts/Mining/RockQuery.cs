using UnityEngine;

namespace Rebuild.Mining
{

    internal static class RockQuery
    {
        static Rock[] s_snapshot = System.Array.Empty<Rock>();
        static bool s_dirty = true;

        internal static int Requests { get; private set; }
        internal static int SceneSearches { get; private set; }

        internal static void Invalidate() => s_dirty = true;


        internal static Rock[] Snapshot()
        {
            Requests++;
            if (s_dirty)
            {
                s_snapshot = Object.FindObjectsByType<Rock>(FindObjectsSortMode.None);
                s_dirty = false;
                SceneSearches++;
            }
            return s_snapshot;
        }
    }
}
