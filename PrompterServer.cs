// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace zClock
{
    // A small, read-only HTTP endpoint. Office COM is never touched on worker threads.
    internal sealed class PrompterServer : IDisposable
    {
        readonly TcpListener listener;
        readonly CancellationTokenSource stop = new CancellationTokenSource();
        readonly HashSet<TcpClient> clients = new HashSet<TcpClient>();
        readonly string prefix;
        readonly byte[] page;
        byte[] snapshot;
        public int Port { get; private set; }
        public bool AllowLan { get; private set; }
        public string LocalUrl { get { return "http://127.0.0.1:" + Port + prefix; } }

        public PrompterServer(int port, bool allowLan, string key)
        {
            Guid parsed;
            if (!Guid.TryParseExact(key, "N", out parsed)) throw new ArgumentException("Invalid access key.");
            if (port < 1024 || port > 65535) throw new ArgumentOutOfRangeException("port");
            Port = port;
            AllowLan = allowLan;
            prefix = "/" + key + "/";
            using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("zClock.Prompter.html"))
            {
                if (resource == null) throw new InvalidOperationException("找不到提词器网页资源。");
                using (var output = new MemoryStream()) { resource.CopyTo(output); page = output.ToArray(); }
            }
            Publish(PrompterState.Waiting("等待 PowerPoint 放映…"));
            listener = new TcpListener(allowLan ? IPAddress.Any : IPAddress.Loopback, port);
            listener.Start(32);
            Task.Run((Func<Task>)AcceptLoop);
        }

        public void Publish(PrompterState state)
        {
            var serializer = new JavaScriptSerializer { MaxJsonLength = 32 * 1024 * 1024 };
            byte[] value;
            try { value = Encoding.UTF8.GetBytes(serializer.Serialize(state)); }
            catch (InvalidOperationException)
            {
                value = Encoding.UTF8.GetBytes(serializer.Serialize(PrompterState.Waiting("备注内容过大，请关闭共享全部备注后重试。")));
            }
            Interlocked.Exchange(ref snapshot, value);
        }

        public string[] Urls()
        {
            var addresses = new List<string> { LocalUrl };
            if (AllowLan)
                foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
                    if (adapter.OperationalStatus == OperationalStatus.Up && adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback && adapter.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                        foreach (var address in adapter.GetIPProperties().UnicastAddresses)
                            if (address.Address.AddressFamily == AddressFamily.InterNetwork)
                                addresses.Add("http://" + address.Address + ":" + Port + prefix);
            return addresses.Distinct().ToArray();
        }

        async Task AcceptLoop()
        {
            while (!stop.IsCancellationRequested)
            {
                TcpClient client;
                try { client = await listener.AcceptTcpClientAsync().ConfigureAwait(false); }
                catch (ObjectDisposedException) { break; }
                catch (SocketException) { break; }
                if (!IsAllowedClient(client)) { client.Close(); continue; }
                lock (clients)
                {
                    if (stop.IsCancellationRequested || clients.Count >= 32) { client.Close(); continue; }
                    clients.Add(client);
                }
                // Handle owns and closes each connection; concurrency is bounded above.
                ThreadPool.QueueUserWorkItem(_ => Handle(client));
            }
        }

        internal static bool IsSameSubnet(IPAddress remote, IPAddress local, IPAddress mask)
        {
            if (remote.AddressFamily != AddressFamily.InterNetwork || local.AddressFamily != AddressFamily.InterNetwork || mask == null) return false;
            byte[] a = remote.GetAddressBytes(), b = local.GetAddressBytes(), m = mask.GetAddressBytes();
            if (m.Length != 4 || m.All(value => value == 0)) return false;
            for (int i = 0; i < 4; i++) if ((a[i] & m[i]) != (b[i] & m[i])) return false;
            return true;
        }

        bool IsAllowedClient(TcpClient client)
        {
            var endpoint = client.Client.RemoteEndPoint as IPEndPoint;
            if (endpoint == null) return false;
            if (IPAddress.IsLoopback(endpoint.Address)) return true;
            if (!AllowLan) return false;
            try
            {
                foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
                    if (adapter.OperationalStatus == OperationalStatus.Up && adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback && adapter.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                        foreach (var address in adapter.GetIPProperties().UnicastAddresses)
                            if (address.Address.AddressFamily == AddressFamily.InterNetwork && IsSameSubnet(endpoint.Address, address.Address, address.IPv4Mask)) return true;
            }
            catch (NetworkInformationException) { }
            return false;
        }

        void Handle(TcpClient client)
        {
            try
            {
                client.ReceiveTimeout = 3000;
                client.SendTimeout = 3000;
                using (client)
                using (var stream = client.GetStream())
                {
                    var request = new StringBuilder();
                    var elapsed = Stopwatch.StartNew();
                    int value;
                    while (request.Length < 8192 && elapsed.ElapsedMilliseconds < 3000 && (value = stream.ReadByte()) >= 0)
                    {
                        request.Append((char)value);
                        if (request.Length >= 4 && request.ToString(request.Length - 4, 4) == "\r\n\r\n") break;
                    }
                    string[] first = request.ToString().Split(new[] { "\r\n" }, StringSplitOptions.None)[0].Split(' ');
                    string path = first.Length == 3 ? first[1].Split('?')[0] : "";
                    if (!request.ToString().EndsWith("\r\n\r\n")) Reply(stream, "400 Bad Request", "text/plain", Encoding.UTF8.GetBytes("Bad request"));
                    else if (first[0] != "GET") Reply(stream, "405 Method Not Allowed", "text/plain", Encoding.UTF8.GetBytes("Read only"));
                    else if (path == prefix) Reply(stream, "200 OK", "text/html", page);
                    else if (path == prefix + "state") Reply(stream, "200 OK", "application/json", snapshot);
                    else Reply(stream, "404 Not Found", "text/plain", Encoding.UTF8.GetBytes("请使用 zClock 提供的完整提词器地址。"));
                }
            }
            catch (IOException) { }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
            finally { lock (clients) clients.Remove(client); client.Close(); }
        }

        static void Reply(Stream stream, string status, string type, byte[] body)
        {
            string headers = "HTTP/1.1 " + status + "\r\nContent-Type: " + type + "; charset=utf-8\r\nContent-Length: " + body.Length +
                "\r\nConnection: close\r\nCache-Control: no-store\r\nX-Content-Type-Options: nosniff\r\nReferrer-Policy: no-referrer" +
                "\r\nContent-Security-Policy: default-src 'self'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; connect-src 'self'; frame-ancestors 'none'; base-uri 'none'\r\n\r\n";
            byte[] bytes = Encoding.ASCII.GetBytes(headers);
            stream.Write(bytes, 0, bytes.Length);
            stream.Write(body, 0, body.Length);
        }

        public void Dispose()
        {
            stop.Cancel();
            listener.Stop();
            lock (clients) { foreach (var client in clients) client.Close(); }
        }
    }
}
