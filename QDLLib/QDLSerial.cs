using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Ports;
using Microsoft.Win32;

using QDLLib.Preloader;
using QDLLib.Exceptions;

namespace QDLLib
{
    using Exceptions;
    using log4net;
    using System.Reflection;

    public class QDLSerial : QDL, IDisposable
    {
        private const int VID = 0x05C6;
        private const int PID = 0x9008;
        private SerialPort port;
        protected static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public QDLSerial()
        {
            log.Info("QDLSerial initializing");
        }

        ~QDLSerial()
        {

        }

        void IDisposable.Dispose()
        {

        }

        protected string[] locateInstalledQDLDevices()
        {
            string usbKeyName = @"SYSTEM\CurrentControlSet\Enum\USB\VID_05C6&PID_9008";
            List<string> foundDevices = new List<string>();
            try {
                
                RegistryKey currentHive = Registry.LocalMachine;
                RegistryKey usbKey = currentHive.OpenSubKey(usbKeyName);
                foreach(string key in usbKey.GetSubKeyNames())
                {
                    RegistryKey deviceKey = usbKey.OpenSubKey(key);
                    string value = null;
                    if((value = (string)deviceKey.GetValue("Service")) != null && value.Equals("qcusbser", StringComparison.InvariantCultureIgnoreCase))
                    {
                        RegistryKey deviceParms = deviceKey.OpenSubKey("Device Parameters");
                        string portName = (string)deviceParms.GetValue("PortName");
                        foundDevices.Add(portName);
                    }
                }
            } catch(Exception ex)
            {
                return new string[0];
            }

            return foundDevices.ToArray();
            
        }

        public override void OpenDevice()
        {
            string[] names = locateInstalledQDLDevices();
            log.DebugFormat("Found {0} installed QDL Devices", names.Length);
            foreach (var name in names)
                log.DebugFormat("Found QDLSerial {0}", name);

            if(names.Length < 1)
            {
                throw new QDLDeviceNotFoundException("No matching serial port found");
            }
            // We always pick the first one.
            string selectedName = names[0];
            log.DebugFormat("Picked QDLSerial device with name {0}", selectedName);
            port = new SerialPort(selectedName);
            
            if(port == null)
            {
                throw new QDLDeviceNotFoundException("Unable to create serial port with device");
            }
            port.Open();
            port.ReadTimeout = 100;
            port.WriteTimeout = 100;
        }

        public override bool isDeviceOpen
        {
            get
            {
                return port != null && port.IsOpen;
            }
        }

        protected override bool transmitCommand(byte[] data, int timeout, ref byte[] response, out int actualReceived)
        {

            int actual = 0;
            if (data != null)
            {
                port.Write(data, 0, data.Length);
            }

           
            actual = port.Read(response, 0, response.Length);

            actualReceived = actual;
            return true;
        }


        private const int batchSize = 4096;
        private const int HeaderSize = 80;
        protected override bool transferPreloader(byte[] preloader, int timeout)
        {
            int actual;
            byte[] response = new byte[1024];
            
            // First send header
            // TODO: Catch exceptions?
            port.Write(preloader, 0, HeaderSize);
            
            int start = HeaderSize;

            if ((actual = port.Read(response, 0, response.Length)) <= 0)
            {
                return false;
            }


            for(int pos = start; pos < preloader.Length; pos += batchSize)
            {
                actual = 0;
                int len = Math.Min(preloader.Length - pos, batchSize);

                port.Write(preloader, pos, len);


                if ((actual = port.Read(response, 0, response.Length)) <= 0)
                {
                    return false;
                }

            }
            return true;
        }


        public override void Close()
        {
            port.Close();
            port = null;
        }

    }
}
