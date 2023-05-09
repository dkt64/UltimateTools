using System;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using FlashCap;
using FlashCap.Utilities;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace UltimaVideoPlayer {
    internal class Program {

        // Constructed capture device.
        private CaptureDevice captureDevice;
        private SynchronizationContext synchContext;

        // ========================================================================================
        static void Main(string[] args) {



            string ipaddr = "192.168.8.123";
            //var ipaddr = args[0];
            var mySocket = Connect(ipaddr);

            var buf = new byte[] {
                        0x06,
                        0xff,
                        0xea,
                        0x03,
                        0x00,
                        0x04
                        };

            byte[] bmp = new byte[1000];

            int shift = 0;

            while (!Console.KeyAvailable & mySocket.Connected) {

                for (int i = 0; i < bmp.Length; i++) {
                    bmp[i] = (byte)(i + shift);
                }

                //mySocket.Send(Combine(buf, bmp));

                shift++;

            }

            Console.WriteLine("Connection End!");
            mySocket.Close();

        }

        // ========================================================================================
        public async void Camera() {
            var devices = new CaptureDevices();

            var descriptors = devices.EnumerateDescriptors().
                // You could filter by device type and characteristics.
                //Where(d => d.DeviceType == DeviceTypes.DirectShow).  // Only DirectShow device.
                Where(d => d.Characteristics.Length >= 1).             // One or more valid video characteristics.
                ToArray();

            // Use first device.
            var descriptor0 = descriptors.ElementAtOrDefault(0);
            if (descriptor0 != null) {
#if true
                // Request video characteristics strictly:
                // Will raise exception when parameters are not accepted.
                var characteristics = new VideoCharacteristics(
                    PixelFormats.JPEG, 1920, 1080, 30);
#else
                // Or, you could choice from device descriptor:
                // Hint: Show up video characteristics into ComboBox and like.
                var characteristics = descriptor0.Characteristics[0];
#endif

                captureDevice = await descriptor0.OpenAsync(
                characteristics,
                OnPixelBufferArrived);
            }
        }

        // ========================================================================================
        private void OnPixelBufferArrived(PixelBufferScope bufferScope) {
            ////////////////////////////////////////////////
            // Pixel buffer has arrived.
            // NOTE: Perhaps this thread context is NOT UI thread.
#if true
            // Get image data binary:
            byte[] image = bufferScope.Buffer.ExtractImage();
#else
            // Or, refer image data binary directly.
            // (Advanced manipulation, see README.)
            ArraySegment<byte> image = bufferScope.Buffer.ReferImage();
#endif


        }


        // ========================================================================================
        public static byte[] Combine(byte[] first, byte[] second) {
            byte[] bytes = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, bytes, 0, first.Length);
            Buffer.BlockCopy(second, 0, bytes, first.Length, second.Length);
            return bytes;
        }

        // ========================================================================================
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
