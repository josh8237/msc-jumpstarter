using System;
using UnityEngine;

namespace JumpStarter
{
    public static class Utils
    {
        public static void SpawnSparksNear(Vector3 pos)
        {
            // Best-effort placeholder: spawn a primitive particle or debug visual if available
            try
            {
                // If there is a particle prefab bundled later, instantiate here.
                // For now, create a short-lived object.
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.transform.position = pos;
                go.transform.localScale = Vector3.one * 0.05f;
                GameObject.Destroy(go, 0.25f);
            }
            catch { }
        }

        public static void SpawnSparksBetween(Vector3 a, Vector3 b)
        {
            SpawnSparksNear((a + b) / 2f);
        }
    }
}
