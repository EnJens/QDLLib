using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using LibUsbDotNet.LudnMonoLibUsb;
using System.IO;
using QDLLib.Preloader;
using QDLLib.Exceptions;

namespace QDLLib
{
    using Exceptions;
    public class QDLUSB : QDL, IDisposable
    {
        private static Guid OPOQDLGuid = new Guid("{059C965F-45BB-4474-BB14-B95FD65A402C}");
        private const int VID = 0x05C6;
        private const int PID = 0x9008;
        private UsbDevice device = null;
        private UsbEndpointReader reader = null;
        private UsbEndpointWriter writer = null;

        public QDLUSB()
        {

        }

        ~QDLUSB()
        {
            if (device != null && device.IsOpen)
            {
                device.Close();
            }
        }

        void IDisposable.Dispose()
        {

        }

        public override void OpenDevice()
        {
            UsbDevice.UsbErrorEvent += new EventHandler<UsbError>(UsbErrorEvent);
            UsbRegistry regDev = null;
            if(Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                regDev = UsbDevice.AllWinUsbDevices.Find((reg) => reg.Vid == VID && reg.Pid == PID);
            } else
            {
                regDev = UsbDevice.AllDevices.Find((reg) => reg.Vid == VID && reg.Pid == PID);
            }
            if(regDev == null)
            {
                throw new QDLDeviceNotFoundException("Unable to find device");
            }

            if(!regDev.Open(out device) || device == null)
            {
                throw new QDLDeviceNotFoundException("Unable to open device");
            }

            if(UsbDevice.IsLinux)
            {
                MonoUsbDevice monodev = device as MonoUsbDevice;
                if(!monodev.DetachKernelDriver())
                {
                    throw new Exception("Failed to detach kernel driver");
                }
            }

            IUsbDevice wholeUsbDevice = device as IUsbDevice;
            if(wholeUsbDevice != null)
            {
                wholeUsbDevice.SetConfiguration(1);
                wholeUsbDevice.ClaimInterface(0);
            }

            reader = device.OpenEndpointReader(ReadEndpointID.Ep01);
            writer = device.OpenEndpointWriter(WriteEndpointID.Ep01, EndpointType.Bulk);
            if(reader == null || writer == null)
            {
                device.Close();
                device = null;
                UsbDevice.Exit();
                throw new Exception("Unable to open endpoints");
            }
        }

        private void UsbErrorEvent(object sender, UsbError e)
        {
            // TODO: Pass this on to Consumers of this library somehow
            Console.WriteLine("Received error event ({0}): {1}", e.ErrorCode, e);
        }

        public override bool isDeviceOpen
        {
            get
            {
                return device != null && device.IsOpen;
            }
        }

        protected override bool transmitCommand(byte[] data, int timeout, ref byte[] response, out int actualReceived)
        {
            int actual = 0;
            if (data != null)
            {
                if (writer.Write(data, timeout, out actual) != ErrorCode.Ok || actual != data.Length)
                {
                    actualReceived = 0;
                    return false;
                }
            }

            ErrorCode err = ErrorCode.Ok;
            if ((err = reader.Read(response, timeout, out actual)) != ErrorCode.Ok)
            {
                actualReceived = 0;
                return false;
            }

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
            if (writer.Write(preloader, 0, HeaderSize, timeout, out actual) != ErrorCode.Ok || actual != HeaderSize)
            {
                return false;
            }
            int start = HeaderSize;

            if (reader.Read(response, timeout, out actual) != ErrorCode.Ok)
            {
                return false;
            }


            for(int pos = start; pos < preloader.Length; pos += batchSize)
            {
                actual = 0;
                int len = Math.Min(preloader.Length - pos, batchSize);
                if(writer.Write(preloader,pos, len, timeout, out actual) != ErrorCode.Ok || actual != len)
                {
                    return false;
                }

                if (reader.Read(response, timeout, out actual) != ErrorCode.Ok)
                {
                    return false;
                }

            }
            return true;
        }

        public override void Close()
        {
            if(device != null && device.IsOpen)
            {
                device.Close();
            }
            UsbDevice.Exit();
            device = null;
        }

    }
}
