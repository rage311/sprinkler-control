using System;
using System.IO;

using Microsoft.SPOT;
using Microsoft.SPOT.IO;
using Microsoft.SPOT.Hardware;

using GHIElectronics.NETMF.FEZ;
using GHIElectronics.NETMF.Hardware;
using GHIElectronics.NETMF.IO;
using GHIElectronics.NETMF.Net;
using GHIElectronics.NETMF.Net.Sockets;
using GHIElectronics.NETMF.Net.NetworkInformation;

using Socket = GHIElectronics.NETMF.Net.Sockets.Socket;


namespace SprinklerControl
{
    class Networking
    {
        public static void InitializeNetwork()
        {
            byte[] myip = { 192, 168, 2, 99 };
            byte[] subnet = { 255, 255, 255, 0 };
            byte[] gateway = { 192, 168, 2, 1 };
            byte[] mac = { 0x00, 0x26, 0x1C, 0x7B, 0x29, 0xE8 };

            WIZnet_W5100.Enable(SPI.SPI_module.SPI1, (Cpu.Pin)FEZ_Pin.Digital.Di10,
                                (Cpu.Pin)FEZ_Pin.Digital.Di7, true);

            NetworkInterface.EnableStaticIP(myip, subnet, gateway, mac);
            NetworkInterface.EnableStaticDns(new byte[] { 192, 168, 2, 1 });

            //Set real-time clock from NTP server
            RealTimeClock.SetTime(GetNetworkTime());
        }


        public static DateTime GetNetworkTime()
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            
            IPAddress dnsAddress = Dns.GetHostEntry("time.nist.gov").AddressList[0];
            IPEndPoint ep = new IPEndPoint(dnsAddress, 123);
            
            byte[] ntpData = new byte[48];  // RFC 2030
            ntpData[0] = 0x1B;
            for (int i = 1; i < 48; i++)
                ntpData[i] = 0;

            s.SendTo(ntpData, ep);
           
            EndPoint recep = new IPEndPoint(dnsAddress, 123);
            s.ReceiveFrom(ntpData, ref recep);
            
            byte offsetTransmitTime = 40;
            ulong intpart = 0;
            ulong fractpart = 0;
            for (int i = 0; i <= 3; i++)
                intpart = 256 * intpart + ntpData[offsetTransmitTime + i];

            for (int i = 4; i <= 7; i++)
                fractpart = 256 * fractpart + ntpData[offsetTransmitTime + i];

            ulong milliseconds = (intpart * 1000 + (fractpart * 1000) / 0x100000000L);

            s.Close();

            TimeSpan timeSpan = TimeSpan.FromTicks((long)milliseconds * TimeSpan.TicksPerMillisecond);
            DateTime dateTime = new DateTime(1900, 1, 1);
            dateTime += timeSpan;

            DateTime networkDateTime = dateTime.AddHours(-6);   //Change to my time zone
            Debug.Print(networkDateTime.ToString());

            return networkDateTime;
        }
    }
}
