// SPDX-License-Identifier: MIT
// RustLike — networked entity contract.
//
// Any entity participating in snapshots implements `INetSerializable`.
// The snapshot builder iterates AOI-relevant entities and asks each to write
// its delta into a BitWriter.
//
// Convention:
//   - First, entity writes a UInt8 "fields-changed" mask.
//   - For each set bit, write the field in compressed form.
//   - Entity is responsible for tracking its own dirty mask.

using RustLike.Net.Transport;

namespace RustLike.Net.Snapshots
{
    /// <summary> Server-side. Called once per snapshot per receiving client. </summary>
    public interface INetSerializable
    {
        /// <summary> Stable id; used to address deltas. </summary>
        int NetId { get; }

        /// <summary> Returns true if the entity has any pending state to send. </summary>
        bool HasPendingDelta { get; }

        /// <summary> Highest priority value sent first when bandwidth is tight. </summary>
        byte SnapshotPriority { get; }

        /// <summary>
        /// Write a delta into `w` and clear the dirty mask if `clearDirty` is true.
        /// Returns false if the writer ran out of bits (caller should defer).
        /// </summary>
        bool WriteDelta(ref BitWriter w, bool clearDirty);
    }

    public interface INetDeserializable
    {
        int NetId { get; }
        void ReadDelta(ref BitReader r);
    }
}
