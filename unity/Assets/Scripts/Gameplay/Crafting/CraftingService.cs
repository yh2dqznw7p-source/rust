// SPDX-License-Identifier: MIT
// RustLike — local crafting queue (Phase 0.5).
//
// In Rust, every player has their own crafting queue. We model it as a list
// of pending jobs. Tick() advances time on the head of the queue; on
// completion the produced item is added to the player's inventory.
//
// Inputs are consumed up-front (Rust convention) so refunds on cancellation
// have to return them. We do exactly that.

using System;
using System.Collections.Generic;
using RustLike.Core.Bootstrap;
using RustLike.Core.Logging;
using RustLike.Core.TimeSys;
using RustLike.Gameplay.Inventory;

namespace RustLike.Gameplay.Crafting
{
    public sealed class CraftingJob
    {
        public Recipe Recipe;
        public float Remaining;
        public ushort Count; // how many crafts remain in this job

        public float TotalTime => Recipe.TimeSec;
        public float Progress01 => 1f - (Remaining / Recipe.TimeSec);
    }

    public sealed class CraftingService : ITickable
    {
        public int Order => SystemOrder.Loot; // doesn't really matter, just after gameplay

        private readonly PlayerInventory _inv;
        private readonly List<CraftingJob> _queue = new(8);
        private readonly HashSet<ushort> _learnedRecipes = new();

        public IReadOnlyList<CraftingJob> Queue => _queue;
        public IReadOnlyCollection<ushort> LearnedRecipes => _learnedRecipes;

        public event Action QueueChanged;
        public event Action<Recipe> RecipeLearned;

        public CraftingService(PlayerInventory inv)
        {
            _inv = inv;
            // Default-known recipes
            foreach (var r in CraftingRecipes.All)
                if (!r.LockedByDefault) _learnedRecipes.Add(r.RecipeId);
        }

        public bool IsLearned(Recipe r) => !r.LockedByDefault || _learnedRecipes.Contains(r.RecipeId);

        public bool CanCraft(Recipe r, out string reason)
        {
            reason = null;
            if (r == null) { reason = "no recipe"; return false; }
            if (!IsLearned(r)) { reason = "not learned"; return false; }
            for (int i = 0; i < r.Inputs.Length; i++)
            {
                var ing = r.Inputs[i];
                if (_inv.CountAll(ing.ItemId) < ing.Amount)
                {
                    reason = "missing materials";
                    return false;
                }
            }
            return true;
        }

        /// <summary> Enqueue `count` crafts of `r`. Consumes inputs immediately. </summary>
        public bool TryEnqueue(Recipe r, ushort count = 1)
        {
            if (!CanCraft(r, out _)) return false;
            // consume inputs * count
            for (int i = 0; i < r.Inputs.Length; i++)
            {
                var ing = r.Inputs[i];
                ushort need = (ushort)(ing.Amount * count);
                ushort got = _inv.RemoveAll(ing.ItemId, need);
                if (got != need)
                {
                    Log.Error(LogCat.Inventory, "Craft enqueue underflow on " + ing.ItemId);
                    // best-effort: refund what we took, abort
                    _inv.PickUp(ing.ItemId, got);
                    return false;
                }
            }
            _queue.Add(new CraftingJob { Recipe = r, Remaining = r.TimeSec, Count = count });
            QueueChanged?.Invoke();
            return true;
        }

        /// <summary> Cancel the queued job at index, refund remaining inputs. </summary>
        public void Cancel(int index)
        {
            if ((uint)index >= (uint)_queue.Count) return;
            var job = _queue[index];
            // Refund only the still-pending crafts. The currently-cooking one
            // (index 0, partly elapsed) refunds 50% Rust-style.
            int fullRefund = job.Count - (index == 0 ? 1 : 0);
            for (int i = 0; i < job.Recipe.Inputs.Length; i++)
            {
                var ing = job.Recipe.Inputs[i];
                _inv.PickUp(ing.ItemId, (ushort)(ing.Amount * fullRefund));
                if (index == 0)
                {
                    // half refund of the in-progress craft
                    _inv.PickUp(ing.ItemId, (ushort)(ing.Amount / 2));
                }
            }
            _queue.RemoveAt(index);
            QueueChanged?.Invoke();
        }

        public bool ResearchItem(ushort itemId)
        {
            var r = CraftingRecipes.ByOutput(itemId);
            if (r == null) return false;
            if (!r.LockedByDefault) return true;
            if (_learnedRecipes.Add(r.RecipeId))
            {
                RecipeLearned?.Invoke(r);
                return true;
            }
            return false;
        }

        public void Tick(uint tick, float dt)
        {
            if (_queue.Count == 0) return;
            var head = _queue[0];
            head.Remaining -= dt;
            if (head.Remaining <= 0f)
            {
                CompleteOne(head);
                if (head.Count <= 0) _queue.RemoveAt(0);
                else head.Remaining = head.Recipe.TimeSec;
                QueueChanged?.Invoke();
            }
        }

        private void CompleteOne(CraftingJob job)
        {
            ushort leftover = _inv.PickUp(job.Recipe.OutputItemId, job.Recipe.OutputAmount);
            if (leftover > 0)
            {
                // No room — drop on ground (server side would spawn DroppedItem).
                Log.Warn(LogCat.Inventory,
                    "Craft completed but no room: item=" + job.Recipe.OutputItemId + " left=" + leftover);
            }
            job.Count--;
        }
    }
}
