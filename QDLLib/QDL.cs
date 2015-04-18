using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using System.IO;
using QDLLib.Preloader;

namespace QDLLib
{
    using Exceptions;
    public partial class QDL : IDisposable
    {
        private static Guid OPOQDLGuid = new Guid("{059C965F-45BB-4474-BB14-B95FD65A402C}");
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

        public bool OpenDevice()
        {
            if(!UsbDevice.OpenUsbDevice(ref OPOQDLGuid, out device))
            {
                return false;
            }

            reader = device.OpenEndpointReader(ReadEndpointID.Ep01);
            writer = device.OpenEndpointWriter(WriteEndpointID.Ep01, EndpointType.Bulk);
            if(reader == null || writer == null)
            {
                device.Close();
                return false;
            }
            return true;
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

            if (reader.Read(response, timeout, out actual) != ErrorCode.Ok)
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

        public bool PerformBootstrap()
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
                    throw new QDLBootstrapFailureException("Failure during cmd3");
                }

                // Now we're talking to the uploaded preloader, most likely.
                if (!transmitCommand(Commands.Magic, 1000, out response))
                {
                    throw new QDLBootstrapFailureException("Failure during cmd4");
                }

                if (!transmitCommand(Commands.Magic, 1000, out response))
                {
                    throw new QDLBootstrapFailureException("Failure during cmd4");
                }

                if (!transmitCommand(Commands.SetSecureMode, 1000, out response))
                {
                    throw new QDLBootstrapFailureException("Failure during cmd5");
                }

                if (!transmitCommand(Commands.OpenMulti, 1000, out response))
                {
                    throw new QDLBootstrapFailureException("Failure during cmd6");
                }
            }
            catch(Exception ex)
            {
                Close();
                return false;
            }

            return true;
        }

     

        public bool WriteFile(uint flashOffset, BufferedStream file)
        {
            byte[] buffer = new byte[1024];
            uint localOffset = flashOffset;
            int read = 0;
            while((read = file.Read(buffer, 0, 1024)) > 0)
            {
                PreloaderCommand response;
                PreloaderCommand cmd = new PreloaderCommand(new WriteFlashPayload(localOffset, buffer));
                byte[] data = cmd.Serialize();
                transmitCommand(cmd, 1000, out response);
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
            return true;
        }
        
        public bool ResetDevice()
        {
            PreloaderCommand response;
            if (!transmitCommand(Commands.CloseFlush, 1000,out response))
            {
                throw new QDLBootstrapFailureException("Failure during cmd7");
            }

            if (!transmitCommand(Commands.Reset, 1000, out response))
            {
                throw new QDLBootstrapFailureException("Failure during cmd8");
            }
            return true;
        }

        public void Close()
        {
            if(device != null && device.IsOpen)
            {
                device.Close();
            }
        }
        
    }
}
