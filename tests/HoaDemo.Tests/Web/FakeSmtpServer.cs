using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HoaDemo.Tests.Web
{
    /// <summary>Accepts a single SMTP session on a loopback port and returns the full transcript.</summary>
    public sealed class FakeSmtpServer : IDisposable
    {
        private readonly TcpListener _listener = new TcpListener(IPAddress.Loopback, 0);

        public FakeSmtpServer()
        {
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            Transcript = Task.Run(ServeOneAsync);
        }

        public int Port { get; }

        public Task<string> Transcript { get; }

        private async Task<string> ServeOneAsync()
        {
            using var client = await _listener.AcceptTcpClientAsync();
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII);
            using var writer = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };
            var transcript = new StringBuilder();

            await writer.WriteLineAsync("220 fake-smtp ready");
            while (await reader.ReadLineAsync() is { } line)
            {
                transcript.AppendLine(line);
                var command = line.ToUpperInvariant();
                if (command == "DATA")
                {
                    await writer.WriteLineAsync("354 end with <CRLF>.<CRLF>");
                    while (await reader.ReadLineAsync() is { } data && data != ".")
                    {
                        transcript.AppendLine(data);
                    }
                    await writer.WriteLineAsync("250 queued");
                }
                else if (command == "QUIT")
                {
                    await writer.WriteLineAsync("221 bye");
                    break;
                }
                else
                {
                    await writer.WriteLineAsync("250 OK");
                }
            }

            return transcript.ToString();
        }

        public void Dispose() => _listener.Stop();
    }
}
