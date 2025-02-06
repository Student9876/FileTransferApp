using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace FileTransfer
{
    public partial class ReceiverForm : Form
    {
        public ReceiverForm()
        {
            InitializeComponent();
        }

        private async void ButtonReceive_Click(object sender, EventArgs e)
        {
            string[] parts = textBox1.Text.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[1], out int port))
            {
                MessageBox.Show("Invalid IP:PORT format");
                return;
            }

            using (TcpClient client = new TcpClient())
            {
                try
                {
                    await client.ConnectAsync(parts[0], port);
                    using (NetworkStream stream = client.GetStream())
                    {
                        // Read metadata
                        byte[] fileNameLenBytes = new byte[4];
                        await ReadFullAsync(stream, fileNameLenBytes);
                        int fileNameLen = BitConverter.ToInt32(fileNameLenBytes, 0);

                        byte[] fileNameBytes = new byte[fileNameLen];
                        await ReadFullAsync(stream, fileNameBytes);
                        string fileName = Encoding.UTF8.GetString(fileNameBytes);

                        byte[] fileSizeBytes = new byte[8];
                        await ReadFullAsync(stream, fileSizeBytes);
                        long fileSize = BitConverter.ToInt64(fileSizeBytes, 0);

                        using (SaveFileDialog saveDialog = new SaveFileDialog())
                        {
                            saveDialog.FileName = fileName;
                            saveDialog.Filter = "All Files (*.*)|*.*";
                            if (saveDialog.ShowDialog() == DialogResult.OK)
                            {
                                using (FileStream fileStream = File.Create(saveDialog.FileName))
                                {
                                    byte[] buffer = new byte[4096];
                                    long totalReceived = 0;
                                    int bytesRead;

                                    while (totalReceived < fileSize)
                                    {
                                        int readSize = (int)Math.Min(buffer.Length, fileSize - totalReceived);
                                        bytesRead = await stream.ReadAsync(buffer, 0, readSize);
                                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                                        totalReceived += bytesRead;
                                        progressBar1.Value = (int)((totalReceived * 100) / fileSize);
                                    }
                                }
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
    }
}