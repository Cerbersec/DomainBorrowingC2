using System;

namespace ClientC2.interfaces
{
    public interface IC2Connector
    {
        /// <summary>
        ///     Determines if the C2 has started
        /// </summary>
        bool Started { get; }

        /// <summary>
        ///     The channel for communicating with the beacon
        /// </summary>
        IC2Channel BeaconChannel { get; }

        /// <summary>
        ///     The channel for communicating with the server
        /// </summary>
        IC2Channel ServerChannel { get; }

        /// <summary>
        ///     Main initialization function of the C2 Connector
        /// </summary>
        Func<bool> Initialize { get; }

        /// <summary>
        ///     Starts the communication loop between the channels
        /// </summary>
        void Go();

        /// <summary>
        ///     Stops the communication channels
        /// </summary>
        void Stop();
    }
}
