using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace StillWater.Net
{
    /// <summary>One direct connection. Async continuations stay on Unity's main thread.</summary>
    internal sealed class LocalRaceLink : IDisposable
    {
        public const int Port = 2569;
        private const int MaxPacketBytes = 16384;
        private const int MaxPendingPackets = 64;
        private const int ConnectTimeoutMs = 5000;

        [Serializable]
        private sealed class Packet
        {
            public string type, json;
        }

        public event Action Connected;
        public event Action<string, string> Received;
        public event Action<string> Failed;

        private readonly Queue<byte[]> pending = new Queue<byte[]>();
        private TcpListener listener;
        private TcpClient socket;
        private NetworkStream stream;
        private bool disposed, writing;

        public static string[] Addresses()
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces()
                    .SelectMany(adapter => adapter.GetIPProperties().UnicastAddresses)
                    .Select(info => info.Address)
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    .Select(ip => ip.ToString())
                    .Distinct()
                    .ToArray();
            }
            catch (Exception e) when (e is NetworkInformationException || e is NotSupportedException || e is SocketException)
            {
                return Array.Empty<string>();
            }
        }

        public async Task Listen()
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, Port);
                listener.Start(1);
                var accepted = await listener.AcceptTcpClientAsync();
                if (disposed) { accepted.Dispose(); return; }
                socket = accepted;
                listener.Stop();
                listener = null;
                await Read();
            }
            catch (Exception e) { Fail("Local host connection ended.", e); }
        }

        public async Task Connect(IPAddress address)
        {
            try
            {
                socket = new TcpClient(AddressFamily.InterNetwork);
                var connecting = socket.ConnectAsync(address, Port);
                if (await Task.WhenAny(connecting, Task.Delay(ConnectTimeoutMs)) != connecting)
                {
                    Fail("Connection timed out. Check the host address and hotspot.");
                    try { await connecting; }
                    catch (Exception) when (disposed) { }
                    return;
                }
                await connecting;
                if (!disposed) await Read();
            }
            catch (Exception e) { Fail("Could not reach the local host.", e); }
        }

        private async Task Read()
        {
            socket.NoDelay = true;
            stream = socket.GetStream();
            Connected?.Invoke();
            var header = new byte[sizeof(int)];
            while (!disposed && await ReadExactly(header))
            {
                int size = BinaryPrimitives.ReadInt32LittleEndian(header);
                if (size <= 0 || size > MaxPacketBytes) throw new IOException("Invalid local race packet length.");
                var payload = new byte[size];
                if (!await ReadExactly(payload)) break;
                var packet = JsonUtility.FromJson<Packet>(Encoding.UTF8.GetString(payload));
                if (packet == null || string.IsNullOrEmpty(packet.type)) throw new IOException("Invalid local race packet.");
                Received?.Invoke(packet.type, packet.json);
            }
            Fail("The other phone disconnected.");
        }

        private async Task<bool> ReadExactly(byte[] buffer)
        {
            int offset = 0;
            // A TCP read can contain only part of a frame.
            while (offset < buffer.Length && !disposed)
            {
                int read = await stream.ReadAsync(buffer, offset, buffer.Length - offset);
                if (read == 0) break;
                offset += read;
            }
            return offset == buffer.Length && !disposed;
        }

        public void Send(string type, string json = null)
        {
            if (disposed || stream == null) return;
            byte[] payload = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Packet { type = type, json = json }));
            if (payload.Length > MaxPacketBytes || pending.Count >= MaxPendingPackets)
            {
                Fail("The local connection could not keep up.");
                return;
            }
            var frame = new byte[sizeof(int) + payload.Length];
            BinaryPrimitives.WriteInt32LittleEndian(frame, payload.Length);
            Buffer.BlockCopy(payload, 0, frame, sizeof(int), payload.Length);
            pending.Enqueue(frame);
            if (!writing) _ = Flush();
        }

        private async Task Flush()
        {
            writing = true;
            try
            {
                while (!disposed && pending.Count > 0)
                {
                    var frame = pending.Dequeue();
                    await stream.WriteAsync(frame, 0, frame.Length);
                }
            }
            catch (Exception e) { Fail("The other phone disconnected.", e); }
            finally { writing = false; }
        }

        private void Fail(string message, Exception error = null)
        {
            if (disposed) return;
            Dispose();
            if (error != null) Debug.LogWarning($"[Local Multiplayer] {error.Message}");
            Failed?.Invoke(message);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            listener?.Stop();
            socket?.Dispose();
            pending.Clear();
        }
    }
}
