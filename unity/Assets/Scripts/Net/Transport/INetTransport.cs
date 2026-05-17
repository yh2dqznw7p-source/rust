// SPDX-License-Identifier: MIT
// RustLike — transport interface.
//
// Decouples the rest of the codebase from FishNet types. The default
// implementation `FishNetTransport` is in this module. Tests can use
// LocalLoopbackTransport.
//
// IMPORTANT — performance:
//   - Send takes ReadOnlySpan<byte>. Spans are stack-only and allocation-free.
//   - The transport implementation MUST copy the bytes immediately because
//     the span lifetime ends when Send returns.
//   - Receive yields the SAME buffer each time (caller must consume in-place).

using System;

namespace RustLike.Net.Transport
{
    public delegate void NetReceiveHandler(int clientId, NetChannel channel, ReadOnlySpan<byte> payload);
    public delegate void NetClientStateHandler(int clientId, bool connected);

    public interface INetTransport
    {
        // ---- server ----
        void StartServer(ushort port, int maxClients);
        void StopServer();
        bool IsServerRunning { get; }
        int  ConnectedClientCount { get; }

        // ---- client ----
        void StartClient(string address, ushort port);
        void StopClient();
        bool IsClientConnected { get; }

        // ---- send ----
        void SendToClient(int clientId, NetChannel channel, ReadOnlySpan<byte> payload);
        void SendToAll  (NetChannel channel, ReadOnlySpan<byte> payload);
        void SendToServer(NetChannel channel, ReadOnlySpan<byte> payload);

        // ---- callbacks ----
        event NetReceiveHandler     OnReceive;
        event NetClientStateHandler OnClientState; // server side: client connected/disconnected
        event Action<bool>          OnLocalState;  // client side: connect/disconnect
    }
}
