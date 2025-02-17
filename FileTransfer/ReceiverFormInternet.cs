using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using Newtonsoft.Json;
using System.Linq;
using System.Net;

namespace FileTransfer
{
    public partial class ReceiverFormInternet : Form
    {
        private const string RelayServerUrl = "https://filetransferapp-relay-server.vercel.app/api/relay";
        private const int CodeLength = 8;

        public ReceiverFormInternet()
        {
            InitializeComponent();
        }

        private async void ButtonReceive_Click(object sender, EventArgs e)
        {
            string input = textBox1.Text.Trim();

            if (IsValidConnectionCode(input))
            {
                await HandleInternetTransfer(input);
            }
            else if (TryParseIpPort(input, out string ip, out int port))
            {
                await HandleLocalTransfer(ip, port);
            }
            else
            {
                MessageBox.Show("Invalid input. Enter either:\n- 8-character code for internet transfer\n- IP:PORT for local transfer");
            }
        }

        private bool IsValidConnectionCode(string input)
        {
            return input.Length == CodeLength && input.All(char.IsLetterOrDigit);
        }

        private bool TryParseIpPort(string input, out string ip, out int port)
        {
            ip = null;
            port = 0;

            if (!input.Contains(":")) return false;

            var parts = input.Split(':');
            if (parts.Length != 2) return false;
            if (!int.TryParse(parts[1], out port)) return false;

            ip = parts[0];
            return true;
        }

        private async Task HandleInternetTransfer(string code)
        {
            try
            {
                var senderDetails = await FetchSenderDetails(code);
                if (senderDetails == null)
                {
                    MessageBox.Show("Invalid code or sender offline");
                    return;
                }

                await ReceiveFile(senderDetails.Ip, senderDetails.Port);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Transfer failed: {ex.Message}");
            }
        }

        private async Task HandleLocalTransfer(string ip, int port)
        {
            try
            {
                await ReceiveFile(ip, port);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Local transfer failed: {ex.Message}");
            }
        }

        private async Task<SenderResponse> FetchSenderDetails(string code)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync($"{RelayServerUrl}?code={code}");
                    if (!response.IsSuccessStatusCode) return null;

                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<SenderResponse>(json);
                }
            }
            catch
            {
                return null;
            }
        }

        private async Task ReceiveFile(string ip, int port)
        {
            try
            {
                // Resolve and validate IPv4 address
                IPAddress ipAddress = await ResolveIPv4Address(ip);
                if (ipAddress == null)
                {
                    MessageBox.Show("Could not resolve valid IPv4 address");
                    return;
                }

                using (var client = new TcpClient(AddressFamily.InterNetwork)) // Force IPv4
                {
                    await client.ConnectAsync(ipAddress, port);
                    using (var stream = client.GetStream())
                    {
                        // Read metadata
                        var fileNameLengthBytes = new byte[4];
                        await ReadFullAsync(stream, fileNameLengthBytes);
                        int fileNameLength = BitConverter.ToInt32(fileNameLengthBytes, 0);

                        var fileNameBytes = new byte[fileNameLength];
                        await ReadFullAsync(stream, fileNameBytes);
                        string fileName = Encoding.UTF8.GetString(fileNameBytes);

                        var fileSizeBytes = new byte[8];
                        await ReadFullAsync(stream, fileSizeBytes);
                        long fileSize = BitConverter.ToInt64(fileSizeBytes, 0);

                        // Get save location
                        using (var saveDialog = new SaveFileDialog
                        {
                            FileName = fileName,
                            Title = "Save Received File"
                        })
                        {
                            if (saveDialog.ShowDialog() != DialogResult.OK) return;

                            await SaveFile(stream, saveDialog.FileName, fileSize);
                            MessageBox.Show("File received successfully!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
        private async Task<IPAddress> ResolveIPv4Address(string ip)
        {
            try
            {
                // Try parsing directly first
                if (IPAddress.TryParse(ip, out IPAddress address) &&
                    address.AddressFamily == AddressFamily.InterNetwork)
                {
                    return address;
                }

                // DNS resolution with IPv4 filtering
                var addresses = await Dns.GetHostAddressesAsync(ip);
                return addresses.FirstOrDefault(a =>
                    a.AddressFamily == AddressFamily.InterNetwork);
            }
            catch
            {
                return null;
            }
        }

        private async Task ReadFullAsync(NetworkStream stream, byte[] buffer)
        {
            int bytesRead = 0;
            while (bytesRead < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer, bytesRead, buffer.Length - bytesRead);
                if (read == 0) throw new EndOfStreamException();
                bytesRead += read;
            }
        }
        private async Task SaveFile(NetworkStream stream, string filePath, long fileSize)
        {
            using (var fileStream = File.Create(filePath))
            {
                var buffer = new byte[4096];
                long totalReceived = 0;
                int bytesRead;

                while (totalReceived < fileSize)
                {
                    int readSize = (int)Math.Min(buffer.Length, fileSize - totalReceived);
                    bytesRead = await stream.ReadAsync(buffer, 0, readSize);
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalReceived += bytesRead;

                    // Update progress bar thread-safely
                    Invoke(new Action(() =>
                    {
                        progressBar1.Value = (int)((totalReceived * 100) / fileSize);
                    }));
                }
            }
        }

        public class SenderResponse
        {
            [JsonProperty("ip")]
            public string Ip { get; set; }

            [JsonProperty("port")]
            public int Port { get; set; }
        }
    }
}