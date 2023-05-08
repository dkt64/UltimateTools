using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace UltimaRunPrg {
    internal class Program {
        static void Main(string[] args) {

            //string ipaddr = "192.168.8.123";
            var ipaddr = args[0];
            var mySocket = Connect(ipaddr);

            //string filename = "yadm.prg";
            var filename = args[1];

            var file = File.ReadAllBytes(filename);

            Console.WriteLine("{0} byte file size.", file.Length);

            byte[] bytes = new byte[0xf800];

            int loadAddress = file[0] + file[1] * 256;

            int startingIndex = loadAddress - 0x0800;

            for (int i = 0; i < file.Length - 2; i++) {
                bytes[startingIndex + i] = file[i + 2];
            }

            if (mySocket != null) {

                var buf = new byte[] {
                        0x02,
                        0xff,
                        0x00,
                        0xf8,
                        0x00,
                        0x08
                        };

                var sent = mySocket.Send(Combine(buf, bytes));

                Console.WriteLine("{0} bytes sent.", sent);

                mySocket.Close();

            } else {
                Console.WriteLine("No connection!");
            }


        }

        public static byte[] Combine(byte[] first, byte[] second) {
            byte[] bytes = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, bytes, 0, first.Length);
            Buffer.BlockCopy(second, 0, bytes, first.Length, second.Length);
            return bytes;
        }

        public static Socket Connect(string host) {
            IPAddress[] IPs = Dns.GetHostAddresses(host);

            Socket s = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);

            Console.WriteLine("Establishing Connection to {0} at port 64.",
                host);
            s.Connect(IPs[0], 64);
            Console.WriteLine("Connection established :)");

            return s;
        }

    }
}
