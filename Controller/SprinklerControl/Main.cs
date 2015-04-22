using System.Threading;

using Microsoft.SPOT;
using Microsoft.SPOT.IO;
using Microsoft.SPOT.Hardware;

using GHIElectronics.NETMF.Hardware;
using GHIElectronics.NETMF.FEZ;
using GHIElectronics.NETMF.Net;
using GHIElectronics.NETMF.Net.NetworkInformation;


namespace SprinklerControl
{
    public class Program
    {        
        public static SprinklerConfig sprinklerConfig;
        public static TCPServer server;

        static OutputPort readyStatusLED = new OutputPort((Cpu.Pin)FEZ_Pin.Digital.LED, false);

        public static void Main()
        {
            Networking.InitializeNetwork();
            
            sprinklerConfig = new SprinklerConfig();
            
            server = new TCPServer();

            readyStatusLED.Write(true);

            Timer checkTimer = new Timer(sprinklerConfig.CheckRunCriteria, null, 0, 10000);
            
            while (true)
            {
                server.Listen();                
            }
        }       
    }   
}
