using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace UltimaKeyb {
    internal class Program {
        static void Main(string[] args) {
            //var mySocket = Connect(args[0]);

            var mySocket = Connect("192.168.8.123");

            if (mySocket != null) {
                Console.WriteLine("Type...");

                while (true) {

                    var k = Console.ReadKey();

                    if (k.Key != ConsoleKey.Escape) {

                        var buf = new byte[] {
                        // $03$ff$01$00$41 - A

                        0x03,
                        0xff,
                        0x01,
                        0x00,
                        (byte)k.Key
                        };

                        mySocket.Send(buf);

                    } else {
                        
                        break;
                    }


                }
            }


        }

        public static Socket Connect(string host) {
            IPAddress[] IPs = Dns.GetHostAddresses(host);

            Socket s = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);

            Console.WriteLine("Establishing Connection to {0}",
                host);
            s.Connect(IPs[0], 64);
            Console.WriteLine("Connection established");

            return s;
        }

    }
}
