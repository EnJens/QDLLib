using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using QDLLib.Preloader;
using QDLLib.Exceptions;

namespace QDLLib
{
    using Exceptions;
    public abstract class QDL : IDisposable
    {
        void IDisposable.Dispose()
        {

        }

        public abstract void OpenDevice();
        public abstract bool isDeviceOpen { get; }
        protected abstract bool transmitCommand(byte[] data, int timeout, ref byte[] response, out int actualReceived);
        protected abstract bool transferPreloader(byte[] preloader, int timeout);
        public abstract void Close();


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

        public void PerformBootstrap()
        {
            if(!isDeviceOpen)
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
    }
}
