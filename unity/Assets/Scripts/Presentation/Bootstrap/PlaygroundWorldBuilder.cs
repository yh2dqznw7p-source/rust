// SPDX-License-Identifier: MIT
// RustLike — small playground spawned at runtime for the Phase 0 demo.
//
// What's built:
//   - large grey ground plane + sun + ambient fog
//   - ring of stylized rocks/cylinders for spatial reference
//   - ~20 trees (harvestable)
//   - ~12 stone nodes
//   - ~6 ore crystals (Metal/Sulfur/HQM, glowing)
//   - 6 dummy enemies (boxes that take damage)

using RustLike.Gameplay.Loot;
using UnityEngine;

namespace RustLike.Presentation.Bootstrap
{
    public static class PlaygroundWorldBuilder
    {
        public static GameObject Build(Transform root)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(root, false);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            SetColor(ground, new Color(0.45f, 0.50f, 0.40f));

            // Sun
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(root, false);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.96f, 0.85f);
            sun.intensity = 1.1f;
            sunGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            sun.shadows = LightShadows.Soft;

            // Ambient + fog
            RenderSettings.ambientMode        = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor    = new Color(0.55f, 0.65f, 0.78f);
            RenderSettings.ambientEquatorColor= new Color(0.50f, 0.50f, 0.45f);
            RenderSettings.ambientGroundColor = new Color(0.20f, 0.18f, 0.15f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 80f;
            RenderSettings.fogEndDistance   = 250f;
            RenderSettings.fogColor = new Color(0.60f, 0.66f, 0.70f);

            // Trees
            for (int i = 0; i < 20; i++)
            {
                float a = (i / 20f) * Mathf.PI * 2f;
                float r = 18f + (i % 5) * 4f;
                Vector3 pos = new(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                MakeTree(root, pos);
            }

            // Stone nodes
            for (int i = 0; i < 12; i++)
            {
                float a = (i / 12f) * Mathf.PI * 2f + 0.3f;
                float r = 30f + (i % 3) * 5f;
                Vector3 pos = new(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                MakeStone(root, pos);
            }

            // Ore (glowing)
            MakeOre(root, new Vector3(  35f, 0f,  10f), ResourceKind.Metal);
            MakeOre(root, new Vector3(  40f, 0f, -15f), ResourceKind.Metal);
            MakeOre(root, new Vector3( -38f, 0f,  12f), ResourceKind.Sulfur);
            MakeOre(root, new Vector3( -34f, 0f, -18f), ResourceKind.Sulfur);
            MakeOre(root, new Vector3(   8f, 0f, -42f), ResourceKind.HQM);
            MakeOre(root, new Vector3(  -6f, 0f,  46f), ResourceKind.HQM);

            // Dummy enemies (red cubes)
            for (int i = 0; i < 6; i++)
            {
                float a = (i / 6f) * Mathf.PI * 2f;
                Vector3 pos = new(Mathf.Cos(a) * 14f, 1.0f, Mathf.Sin(a) * 14f);
                MakeDummy(root, pos);
            }

            // Reference pillar near origin
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "Pillar";
            pillar.transform.SetParent(root, false);
            pillar.transform.position = new Vector3(0f, 5f, 8f);
            pillar.transform.localScale = new Vector3(1.2f, 5f, 1.2f);
            SetColor(pillar, new Color(0.85f, 0.30f, 0.20f));

            return ground;
        }

        // ---- prop builders ----

        private static void MakeTree(Transform root, Vector3 pos)
        {
            var go = new GameObject("Tree");
            go.transform.SetParent(root, false);
            go.transform.position = pos;

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.transform.SetParent(go.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            trunk.transform.localScale = new Vector3(0.6f, 1.6f, 0.6f);
            SetColor(trunk, new Color(0.40f, 0.25f, 0.10f));

            var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.transform.SetParent(go.transform, false);
            canopy.transform.localPosition = new Vector3(0f, 3.4f, 0f);
            canopy.transform.localScale = Vector3.one * 2.4f;
            SetColor(canopy, new Color(0.20f, 0.55f, 0.20f));

            var node = go.AddComponent<ResourceNode>();
            node.Kind = ResourceKind.Tree;
            node.Health = 200f;
            node.YieldPerHit = 12;
        }

        private static void MakeStone(Transform root, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Stone";
            go.transform.SetParent(root, false);
            go.transform.position = pos + Vector3.up * 0.5f;
            go.transform.rotation = Quaternion.Euler(Random.value * 30f, Random.value * 360f, Random.value * 30f);
            go.transform.localScale = new Vector3(1.3f, 1.0f, 1.2f) + Vector3.one * Random.value * 0.4f;
            SetColor(go, new Color(0.55f, 0.55f, 0.55f));

            var node = go.AddComponent<ResourceNode>();
            node.Kind = ResourceKind.Stone;
            node.Health = 250f;
            node.YieldPerHit = 10;
        }

        private static void MakeOre(Transform root, Vector3 pos, ResourceKind kind)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = kind + "Ore";
            go.transform.SetParent(root, false);
            go.transform.position = pos + Vector3.up * 0.7f;
            go.transform.localScale = new Vector3(1.6f, 1.4f, 1.5f);
            // base rock color; the glowing crystal child is added by ResourceNode
            SetColor(go, new Color(0.40f, 0.35f, 0.30f));

            var node = go.AddComponent<ResourceNode>();
            node.Kind = kind;
            node.Health = 400f;
            node.YieldPerHit = 6;
        }

        private static void MakeDummy(Transform root, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Dummy";
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.9f, 1.9f, 0.9f);
            SetColor(go, new Color(0.85f, 0.20f, 0.20f));
            go.AddComponent<RustLike.Presentation.Player.DummyEnemy>();
        }

        private static void SetColor(GameObject go, Color c)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend == null) return;
            var mpb = new MaterialPropertyBlock();
            mpb.SetColor("_BaseColor", c);
            mpb.SetColor("_Color", c);
            rend.SetPropertyBlock(mpb);
        }
    }
}
