using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace FileTransfer
{
    public partial class SenderFormInternet : Form
    {
        private TcpListener listener;
        private const string RelayServerURL = "https://filetransferapp-relay-server.vercel.app/api/relay";
        private CancellationTokenSource cts;
        public SenderFormInternet()
        {
            InitializeComponent();
        }

        private void ButtonSend_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select a file to send",
                Filter = "All Files (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = openFileDialog.FileName;
            }
        }

        private async void BtnStartServer_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox1.Text))
            {
                MessageBox.Show("Please select a file first.");
                return;
            }

            if (!int.TryParse(txtPort.Text, out int port))
            {
                MessageBox.Show("Invalid port number.");
                return;
            }

            string filePath = textBox1.Text;
            IPAddress publicIp = await GetPublicIPAddress();
            btnStartServer.Enabled = false;
            cts = new CancellationTokenSource();

            if (publicIp == null)
            {
                MessageBox.Show("Could not determine public IP address.");
                return;
            }

            string connectionCode = Guid.NewGuid().ToString().Substring(0, 8);
            bool registered = await RegisterSender(publicIp.ToString(), port, connectionCode);

            if (!registered)
            {
                MessageBox.Show("Failed to register with relay server.");
                return;
            }

            label3.Text = $"Waiting for receiver at {publicIp}:{port}\nConnection: {connectionCode}";
            Console.WriteLine($"Connection: {connectionCode}");  
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            try
            {
                using (TcpClient client = await listener.AcceptTcpClientAsync())
                using (NetworkStream stream = client.GetStream())
                using (FileStream fileStream = File.OpenRead(filePath))
                {
                    string fileName = Path.GetFileName(filePath);
                    byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
                    byte[] fileNameLength = BitConverter.GetBytes(fileNameBytes.Length);
                    byte[] fileSize = BitConverter.GetBytes(fileStream.Length);

                    // Send metadata
                    await stream.WriteAsync(fileNameLength, 0, 4);
                    await stream.WriteAsync(fileNameBytes, 0, fileNameBytes.Length);
                    await stream.WriteAsync(fileSize, 0, 8);

                    // Send file data
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    long totalSent = 0;
                    long fileLength = fileStream.Length;

                    while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await stream.WriteAsync(buffer, 0, bytesRead);
                        totalSent += bytesRead;
                        progressBar1.Value = (int)((totalSent * 100) / fileLength);
                    }
                }

                MessageBox.Show("File sent successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
            finally
            {
                listener.Stop();
                btnStartServer.Enabled = true;
            }

        }
        private async Task<IPAddress> GetPublicIPAddress()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string ip = await client.GetStringAsync("https://api64.ipify.org");
                    return IPAddress.Parse(ip);
                }
            }
            catch
            {
                return null;
            }
        }

        private async Task<bool> RegisterSender(string ip, int port, string code)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var data = new { code, ip, port };
                    string json = JsonConvert.SerializeObject(data);
                    StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync(RelayServerURL, content);
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            cts?.Cancel();
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            cts?.Cancel();
            listener?.Stop();
            base.OnFormClosing(e);
        }

    }
}
