// SPDX-License-Identifier: MIT
// RustLike — a small playground spawned at runtime for the Phase 0 demo.
//
// We do NOT ship a Unity scene file in the repo because:
//   - .unity files are hard to merge,
//   - scene serialization references prefabs that don't exist yet (we have no
//     art),
//   - a code-built scene is reproducible from a clean clone.
//
// What's built:
//   - 200x200 m ground plane (URP-Lit, neutral grey),
//   - directional sun at 50° pitch,
//   - a low fog ambient,
//   - a few cubes/cylinders so you can tell you're moving.

using UnityEngine;

namespace RustLike.Presentation.Bootstrap
{
    public static class PlaygroundWorldBuilder
    {
        public static GameObject Build(Transform root)
        {
            // ground
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(root, false);
            ground.transform.localScale = new Vector3(20f, 1f, 20f); // Plane is 10x10
            SetColor(ground, new Color(0.45f, 0.50f, 0.40f));

            // sun
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(root, false);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.96f, 0.85f);
            sun.intensity = 1.1f;
            sunGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            sun.shadows = LightShadows.Soft;

            // ambient (cheap)
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor    = new Color(0.55f, 0.65f, 0.78f);
            RenderSettings.ambientEquatorColor= new Color(0.50f, 0.50f, 0.45f);
            RenderSettings.ambientGroundColor = new Color(0.20f, 0.18f, 0.15f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 80f;
            RenderSettings.fogEndDistance   = 250f;
            RenderSettings.fogColor = new Color(0.60f, 0.66f, 0.70f);

            // landmarks
            for (int i = 0; i < 24; i++)
            {
                float a = (i / 24f) * Mathf.PI * 2f;
                float r = 12f + (i % 4) * 6f;
                var go = GameObject.CreatePrimitive((i & 1) == 0 ? PrimitiveType.Cube : PrimitiveType.Cylinder);
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(Mathf.Cos(a) * r, 1f, Mathf.Sin(a) * r);
                go.transform.localScale = new Vector3(2f, 2f + (i % 3), 2f);
                SetColor(go, new Color((i*0.13f) % 1f, (i*0.27f) % 1f, (i*0.41f) % 1f));
            }

            // a tall pillar near origin so you can see something on Play
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.transform.SetParent(root, false);
            pillar.transform.position = new Vector3(0f, 5f, 8f);
            pillar.transform.localScale = new Vector3(1.2f, 5f, 1.2f);
            SetColor(pillar, new Color(0.85f, 0.30f, 0.20f));

            return ground;
        }

        private static void SetColor(GameObject go, Color c)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend == null) return;
            // MaterialPropertyBlock to avoid creating a unique material per object.
            var mpb = new MaterialPropertyBlock();
            mpb.SetColor("_BaseColor", c); // URP-Lit
            mpb.SetColor("_Color", c);     // built-in fallback
            rend.SetPropertyBlock(mpb);
        }
    }
}
