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
        private int currentRadioButtonState;
        public Form1()
        {
            InitializeComponent();
            currentRadioButtonState = radioButton1.Checked? 0:1;
        }


        private void Button1_Click(object sender, EventArgs e)
        {
            if(currentRadioButtonState == 1)
            {
                if (Application.OpenForms["SenderForm"] == null)
                {
                    using (SendersForm sendersForm = new SendersForm())
                    {
                        sendersForm.ShowDialog();
                    }
                }
            }
            else
            {
                if (Application.OpenForms["SenderFormInternet"] == null)
                {
                    using (SenderFormInternet senderFormInternet = new SenderFormInternet())
                    {
                        senderFormInternet.ShowDialog();
                    }
                }
            }
        }
        private void Button2_Click(object sender, EventArgs e)
        {
            if(currentRadioButtonState == 1) { 
                if (Application.OpenForms["ReceiverForm"] == null)
                {
                    // Open Receiver Form as a modal dialog
                    using (ReceiverForm receiverForm = new ReceiverForm())
                    {
                        receiverForm.ShowDialog(); // This will disable the main form until closed
                    }
                }
            } 
            else
            {
                Console.WriteLine("Not implemented yet");
            }
        }

        private void RadioButton1_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            if (radioButton != null)
            {
                Console.WriteLine($"RadioButton1 is {(radioButton.Checked ? "checked 1" : "unchecked")}");
            }
            currentRadioButtonState = 0;
        }

        private void RadioButton2_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            if (radioButton != null)
            {
                Console.WriteLine($"RadioButton2 is {(radioButton.Checked ? "checked 2" : "unchecked")}");
            }
            currentRadioButtonState = 1;
        }
    }
}
