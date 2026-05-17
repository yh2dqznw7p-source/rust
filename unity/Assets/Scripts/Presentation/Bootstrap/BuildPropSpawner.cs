// SPDX-License-Identifier: MIT
// RustLike — primitive prop spawner for the demo.
//
// Each item-id maps to a procedurally built GameObject (cube/cylinder/etc.)
// with a colored material. Real game has Addressables prefabs per item;
// for the playable demo we keep zero-asset.

using RustLike.Gameplay.Inventory;
using UnityEngine;

namespace RustLike.Presentation.Bootstrap
{
    public static class BuildPropSpawner
    {
        public static GameObject Spawn(ItemDef def, Vector3 pos, Quaternion rot)
        {
            if (def == null) return null;
            switch (def.ItemId)
            {
                case ItemIds.BFoundation: return MakeBox(pos, rot, new Vector3(3f, 0.4f, 3f), new Color(0.65f, 0.55f, 0.40f), "Foundation");
                case ItemIds.BWall:       return MakeBox(pos, rot, new Vector3(3f, 3f, 0.2f), new Color(0.55f, 0.45f, 0.30f), "Wall");
                case ItemIds.BFloor:      return MakeBox(pos, rot, new Vector3(3f, 0.2f, 3f), new Color(0.60f, 0.50f, 0.35f), "Floor");
                case ItemIds.BDoorway:    return MakeDoorwayFrame(pos, rot);
                case ItemIds.BWoodDoor:   return MakeBox(pos, rot, new Vector3(1f, 2f, 0.1f), new Color(0.45f, 0.30f, 0.15f), "Wood Door");

                case ItemIds.SmallBox:    return MakeBox(pos, rot, new Vector3(0.8f, 0.7f, 0.6f), new Color(0.60f, 0.45f, 0.25f), "Small Box");
                case ItemIds.LargeBox:    return MakeBox(pos, rot, new Vector3(1.4f, 1.2f, 0.8f), new Color(0.50f, 0.35f, 0.20f), "Large Box");
                case ItemIds.Locker:      return MakeBox(pos, rot, new Vector3(1.0f, 1.9f, 0.5f), new Color(0.40f, 0.50f, 0.55f), "Locker");
                case ItemIds.Fridge:      return MakeBox(pos, rot, new Vector3(0.9f, 1.8f, 0.7f), new Color(0.85f, 0.85f, 0.90f), "Fridge");
                case ItemIds.SleepingBag: return MakeBox(pos, rot, new Vector3(2.0f, 0.2f, 0.8f), new Color(0.70f, 0.30f, 0.30f), "Sleeping Bag");
                case ItemIds.Furnace:     return MakeBox(pos, rot, new Vector3(1.0f, 1.1f, 1.0f), new Color(0.45f, 0.45f, 0.50f), "Furnace");
                case ItemIds.Workbench1:  return MakeBox(pos, rot, new Vector3(2.0f, 1.0f, 1.0f), new Color(0.50f, 0.40f, 0.25f), "Workbench T1");
                case ItemIds.Workbench2:  return MakeBox(pos, rot, new Vector3(2.0f, 1.0f, 1.0f), new Color(0.55f, 0.45f, 0.30f), "Workbench T2");
                case ItemIds.ResearchTbl: return MakeBox(pos, rot, new Vector3(1.6f, 1.0f, 1.0f), new Color(0.45f, 0.35f, 0.20f), "Research Table");
                case ItemIds.Cupboard:    return MakeBox(pos, rot, new Vector3(0.8f, 1.4f, 0.8f), new Color(0.40f, 0.30f, 0.15f), "Tool Cupboard");
            }
            return null;
        }

        private static GameObject MakeBox(Vector3 pos, Quaternion rot, Vector3 size, Color c, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetPositionAndRotation(pos + Vector3.up * (size.y * 0.5f), rot);
            go.transform.localScale = size;
            SetColor(go, c);
            return go;
        }

        private static GameObject MakeDoorwayFrame(Vector3 pos, Quaternion rot)
        {
            var root = new GameObject("Doorway");
            root.transform.SetPositionAndRotation(pos, rot);

            // top lintel
            var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "Lintel";
            top.transform.SetParent(root.transform, false);
            top.transform.localPosition = new Vector3(0f, 2.7f, 0f);
            top.transform.localScale = new Vector3(3f, 0.6f, 0.2f);
            SetColor(top, new Color(0.50f, 0.40f, 0.25f));

            // left jamb
            var l = GameObject.CreatePrimitive(PrimitiveType.Cube);
            l.name = "JambL";
            l.transform.SetParent(root.transform, false);
            l.transform.localPosition = new Vector3(-1.2f, 1.2f, 0f);
            l.transform.localScale = new Vector3(0.6f, 2.4f, 0.2f);
            SetColor(l, new Color(0.50f, 0.40f, 0.25f));

            // right jamb
            var r = GameObject.CreatePrimitive(PrimitiveType.Cube);
            r.name = "JambR";
            r.transform.SetParent(root.transform, false);
            r.transform.localPosition = new Vector3(1.2f, 1.2f, 0f);
            r.transform.localScale = new Vector3(0.6f, 2.4f, 0.2f);
            SetColor(r, new Color(0.50f, 0.40f, 0.25f));

            return root;
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
