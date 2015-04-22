using System;
using System.Xml;
using System.Threading;
using System.Text;

using Microsoft.SPOT;

using GHIElectronics.NETMF.Hardware;
using GHIElectronics.NETMF.Net;
using GHIElectronics.NETMF.Net.Sockets;

using Socket = GHIElectronics.NETMF.Net.Sockets.Socket;


namespace SprinklerControl
{
    public class TCPServer
    {
        public Socket server;

        public TCPServer()
        {
            try
            {
                const Int32 c_port = 9099;
                server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, c_port);
                server.Bind(localEndPoint);
                server.Listen(1);
            }
            catch (Exception e)
            {
                Debug.Print(e.ToString());
            }
        }

        public void Listen()
        {
            // Wait for a client to connect.
            Socket clientSocket = server.Accept();

            // Process the client request.  true means asynchronous.
            new ProcessClientRequest(clientSocket, true);
        }

        public byte[] ParseReceivedData(byte[] received)
        {
            string receivedString = new string(Encoding.UTF8.GetChars(received));
            string[] receivedStringArray;

            if (receivedString.IndexOf('&') > -1)
                receivedStringArray = receivedString.Split('&');
            else
                receivedStringArray = new string[2]{receivedString, ""};

            Debug.Print("\nRECEIVED DATA: " + receivedString);

            // basic validation of request
            switch (receivedStringArray[0])
            {
                case "system=sprinklers":
                    
                    switch (receivedStringArray[1])
                    {
                        case "action=demand":
                            Program.sprinklerConfig.setDemand(Convert.ToSByte(receivedStringArray[2].Split('=')[1]));
                            goto case "action=get";
                        case "action=set":
                            XmlReader xmlStream = XmlReader.Create(new System.IO.MemoryStream(Encoding.UTF8.GetBytes(receivedStringArray[2])));
                            Program.sprinklerConfig.SetClassProperties(xmlStream);
                            xmlStream.Dispose();

                            Program.sprinklerConfig.CreateXMLParameters();
                            goto case "action=get";
                        case "action=get":

                        default:
                            return Program.sprinklerConfig.ReturnValuesAsXML();    
                        //return Encoding.UTF8.GetBytes(Program.sprinklerConfig.serializeParameters());                
                    }
                    
                default:
                    return Encoding.UTF8.GetBytes("ParseReceivedData(): invalid request: " + receivedStringArray[0]);
            }
        }


        /// <summary>
        /// Processes a client request.
        /// </summary>
        internal sealed class ProcessClientRequest
        {
            private Socket m_clientSocket;

            /// <summary>
            /// The constructor calls another method to handle the request, but can 
            /// optionally do so in a new thread.
            /// </summary>
            /// <param name="clientSocket"></param>
            /// <param name="asynchronously"></param>
            public ProcessClientRequest(Socket clientSocket, Boolean asynchronously)
            {
                m_clientSocket = clientSocket;

                if (asynchronously)
                    // Spawn a new thread to handle the request.
                    new Thread(ProcessRequest).Start();
                else ProcessRequest();
            }

            /// <summary>
            /// Processes the request.
            /// </summary>
            private void ProcessRequest()
            {
                const Int32 c_microsecondsPerSecond = 1000000;

                // 'using' ensures that the client's socket gets closed.
                using (m_clientSocket)
                {
                    // Wait for the client request to start to arrive.
                    Byte[] buffer = new Byte[1024];
                    if (m_clientSocket.Poll(20 * c_microsecondsPerSecond,
                        SelectMode.SelectRead))
                    {
                        // If 0 bytes in buffer, then the connection has been closed, 
                        // reset, or terminated.
                        if (m_clientSocket.Available == 0)
                            return;

                        // Read the first chunk of the request (we don't actually do 
                        // anything with it).
                        Int32 bytesRead = m_clientSocket.Receive(buffer,
                            m_clientSocket.Available, SocketFlags.None);
                        
                        //Debug.Print(new string(Encoding.UTF8.GetChars(buffer)));

                        // pass received byte array to parser
                        byte[] replyBuffer;
                        if (buffer.Length > 0)
                            replyBuffer = Program.server.ParseReceivedData(buffer);
                        else
                            replyBuffer = Encoding.UTF8.GetBytes("Failure to generate a useful reply");
                              

                        int offset = 0;
                        int ret = 0;
                        int len = replyBuffer.Length;
                        while (len > 0)
                        {
                            ret = m_clientSocket.Send(replyBuffer, offset, len, SocketFlags.None);
                            Debug.Print("Bytes sent: " + ret);
                            len -= ret;
                            offset += ret;
                        }

                        m_clientSocket.Close();
                    }
                }
            }
        }
    }
}



/*
// Return a static HTML document to the client.
String s =  "This is the future home of Melvinius Sprinkler Control 3000\r\n\r\n" + 
            RealTimeClock.GetTime() + 
            "\r\n\r\nSprinkler Config:\r\nTotal zones: " + Program.sprinklerConfig.numZones +
            "\r\nStart time: " + (Program.sprinklerConfig.startTimeInMinutes / 60) + ":" + (Program.sprinklerConfig.startTimeInMinutes % 60);
for (int i = 1; i <= Program.sprinklerConfig.numZones; i++)
    s += "\r\nZone " + i + " duration: " + Program.sprinklerConfig.durationMinutes[i - 1] + " minutes";

s += "\r\nSystem enabled: " + Program.sprinklerConfig.sysEnabled.ToString();
*/
/*
    "HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\n\r\n<html><head><title>.NET Micro Framework Web Server on USBizi Chipset </title></head>" +
    "<body><bold><a href=\"http://www.tinyclr.com/\">Learn more about the .NET Micro Framework with FEZ by clicking here</a></bold></body></html>";*/
//byte[] buf = Encoding.UTF8.GetBytes(s);