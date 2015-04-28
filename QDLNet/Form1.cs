using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using QDLLib;

namespace QDLNet
{
    public partial class Form1 : Form
    {
        private QDL qdl = null;

        public Form1()
        {
            InitializeComponent();
        }

        private QDL openBestDevice()
        {
            QDL myQDL = qdl;
            try
            {
                if (myQDL == null)
                {
                    myQDL = new QDLUSB();
                }
                if (!myQDL.isDeviceOpen)
                {
                    myQDL.OpenDevice();
                }
            }
            catch (QDLLib.Exceptions.QDLDeviceNotFoundException nex)
            {
                myQDL = null;
            }

            if (myQDL == null)
            {
                try
                {
                    if (myQDL == null)
                    {
                        myQDL = new QDLSerial();
                    }
                    if (!myQDL.isDeviceOpen)
                    {
                        myQDL.OpenDevice();
                    }
                }
                catch (QDLLib.Exceptions.QDLDeviceNotFoundException nex)
                {
                    myQDL = null;
                }
            }

            return myQDL;
        }

        private void button1_Click(object sender, EventArgs e)
        {

            qdl = openBestDevice();

            if(qdl == null || !qdl.isDeviceOpen)
            {
                MessageBox.Show("Unable to open device");
                return;
            }

            qdl.PerformBootstrap();

            var stream = new BufferedStream(File.Open(@"G:\Fastboot\twrp-2.8.6.0-find7.img", FileMode.Open));
            qdl.WriteFile(0x06000000, stream);

            qdl.ResetDevice();
            qdl.Close();
        }

    }
}
