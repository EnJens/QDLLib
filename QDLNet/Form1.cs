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

        private void button1_Click(object sender, EventArgs e)
        {
            if(qdl == null)
            {
                qdl = new QDL();
                
            }
            if(!qdl.isDeviceOpen())
            {
                qdl.OpenDevice();
            }

            if(!qdl.PerformBootstrap())
            {
                MessageBox.Show("Failure during bootstrap!");
            }

            //var stream = new BufferedStream(File.Open(@"G:\Fastboot\lk_fastbootmobile_test.img", FileMode.Open));
            var stream = new BufferedStream(File.Open(@"G:\Fastboot\twrp-2.8.6.0-find7.img", FileMode.Open));
            
            if(!qdl.WriteFile(0x06000000, stream))
            {
                MessageBox.Show("Failure writing file to flash");
            }

            if(!qdl.ResetDevice())
            {
                MessageBox.Show("Unable to reset device");
            }

            qdl.Close();

        }

    }
}
