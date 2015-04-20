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
    public partial class QDL : IDisposable
    {
        private static Guid OPOQDLGuid = new Guid("{059C965F-45BB-4474-BB14-B95FD65A402C}");
        private const int VID = 0x05C6;
        private const int PID = 0x9008;
        private UsbDevice device = null;
        private UsbEndpointReader reader = null;
        private UsbEndpointWriter writer = null;

        public QDL()
        {

        }

        ~QDL()
        {
            if (device != null && device.IsOpen)
            {
                device.Close();
            }
        }

        void IDisposable.Dispose()
        {

        }

        public void OpenDevice()
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

        public bool isDeviceOpen()
        {
            return device != null && device.IsOpen;
        }

        private bool transmitCommand(byte[] data, int timeout, ref byte[] response, out int actualReceived)
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

        private bool transmitCommand(PreloaderCommand command, int timeout, out PreloaderCommand response)
        {
            int actual;
            byte[] respBytes = new byte[4096];
            byte[] cmdData = command.Serialize();
            if(!transmitCommand(cmdData, timeout, ref respBytes, out actual))
            {
                throw new Exception("Failed transmitting Preloader command");
            }

            response = PreloaderCommand.Deserialize(respBytes, actual);
            if(response.payload is ErrorPayload || response.payload is MessagePayload)
            {
                return false;
            }
            return true;
        }


        private const int batchSize = 4096;
        private const int HeaderSize = 80;
        private bool transferPreloader(byte[] preloader, int timeout)
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

        public void PerformBootstrap()
        {
            if(device == null || !device.IsOpen)
            {
                throw new InvalidOperationException("Device must be opened before bootstrap can be performed");
            }

            PreloaderCommand response;
            int actual = 0;
            byte[] buffer = new byte[4096];
            try
            {
                if (!transmitCommand(null, 1000, ref buffer, out actual))
                {
                    throw new QDLBootstrapFailureException("Failed to receive initial data");
                }

                if (!transmitCommand(Commands.cmd1, 1000, ref buffer, out actual))
                {
                    throw new QDLBootstrapFailureException("Failure during cmd1");
                }

                if (!transferPreloader(Commands.preloader, 1000))
                {
                    throw new QDLBootstrapFailureException("Loading preloader failed");
                }

                // Execute the uploaded preloader
                if (!transmitCommand(Commands.cmd3, 1000, ref buffer, out actual))
                {
                    throw new QDLBootstrapFailureException("Failure during Execute");
                }

                // Now we're talking to the uploaded preloader, most likely.
                if (!transmitCommand(Commands.Magic, 1000, out response))
                {
                    throw new QDLBootstrapFailureException("Failure during Magic");
                }

                if (!transmitCommand(Commands.Magic, 1000, out response))
                {
                    throw new QDLBootstrapFailureException("Failure during Magic2");
                }

                if (!transmitCommand(Commands.SetSecureMode, 1000, out response))
                {
                    throw new QDLBootstrapFailureException("Failure during SetSecureMode");
                }

                if (!transmitCommand(Commands.OpenMulti, 1000, out response))
                {
                    throw new QDLBootstrapFailureException("Failure during OpenMulti");
                }
            }
            catch(Exception ex)
            {
                Close();
                throw;
            }
        }

        public void WriteFile(uint flashOffset, BufferedStream file)
        {
            byte[] buffer = new byte[1024];
            uint localOffset = flashOffset;
            int read = 0;
            while((read = file.Read(buffer, 0, 1024)) > 0)
            {
                PreloaderCommand response;
                PreloaderCommand cmd = new PreloaderCommand(new WriteFlashPayload(localOffset, buffer));
                byte[] data = cmd.Serialize();
                Console.WriteLine("Sending {0} bytes", read);
                if(!transmitCommand(cmd, 1000, out response))
                {
                    throw new Exception("Failure during transmitting writeflash command");
                }
                if(response.payload is WriteFlashPayload)
                {
                    localOffset += (uint)read;
                } else if(response.payload is ErrorPayload)
                {
                    ErrorPayload errorpl = (ErrorPayload)response.payload;
                    throw new Exception(String.Format("Error received from device: {0} with Message: {1}", errorpl.ErrorCode, errorpl.Message ));
                } else if(response.payload is MessagePayload)
                {
                    MessagePayload msgpl = (MessagePayload)response.payload;
                    throw new Exception(String.Format("Message received from device: {0}", msgpl.Message));
                }
            }
        }

        public void ResetDevice()
        {
            PreloaderCommand response;
            if (!transmitCommand(Commands.CloseFlush, 1000,out response))
            {
                throw new QDLResetFailureException("Failure during Flush");
            }

            if (!transmitCommand(Commands.Reset, 1000, out response))
            {
                throw new QDLResetFailureException("Failure during Reset");
            }
        }

        public void Close()
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
