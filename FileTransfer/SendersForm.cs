using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace FileTransfer
{
    public partial class SendersForm : Form
    {
        public SendersForm()
        {
            InitializeComponent();
        }

        private void ButtonSend_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select a file to send";
            openFileDialog.Filter = "All Files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = openFileDialog.FileName;
            }
        }

        private async void btnStartServer_Click(object sender, EventArgs e)
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
            IPAddress localIp = GetLocalIPAddress();

            if (localIp == null)
            {
                MessageBox.Show("Could not determine local IP address.");
                return;
            }

            TcpListener listener = new TcpListener(localIp, port);
            listener.Start();

            try
            {
                label3.Text = $"Waiting for receiver at {localIp}:{port}...";

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
            }
        }

        private IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            return null;
        }
    }
}