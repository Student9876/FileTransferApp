using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileTransfer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }


        private void Button1_Click(object sender, EventArgs e)
        {
            if (Application.OpenForms["SenderForm"] == null)
            {
                using (SendersForm sendersForm = new SendersForm())
                {
                    sendersForm.ShowDialog();
                }
            }
        }
        private void Button2_Click(object sender, EventArgs e)
        {
            if (Application.OpenForms["ReceiverForm"] == null)
            {
                // Open Receiver Form as a modal dialog
                using (ReceiverForm receiverForm = new ReceiverForm())
                {
                    receiverForm.ShowDialog(); // This will disable the main form until closed
                }
            }
        }
    }
}
