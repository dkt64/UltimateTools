using FlashCap;
using FlashCap.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace UltimaVideoPlayer2 {
    public partial class Form1 : Form {

        // ========================================================================================

        // Constructed capture device.
        private CaptureDevice captureDevice;

        private SynchronizationContext synchContext;

        int prog = 60;

        int bytes_sent;

        byte[] c64bmp = new byte[8000];

        Socket mySocket;

        // ========================================================================================
        public Form1() {
            InitializeComponent();
        }

        // ========================================================================================
        private void Form1_Load(object sender, EventArgs e) {

            trackBar1.Value = prog;
            label3.Text = "Prog " + prog.ToString();

            textBox1.Clear();

            Thread threadCamera = new Thread(new ThreadStart(Camera));
            threadCamera.Start();
          

            Thread threadUltimate = new Thread(new ThreadStart(Ultimate));
            threadUltimate.Start();
        }

        // ========================================================================================
        public async void Camera() {
            var devices = new CaptureDevices();

            foreach (var descriptor in devices.EnumerateDescriptors()) {
                // "Logicool Webcam C930e: DirectShow device, Characteristics=34"
                // "Default: VideoForWindows default, Characteristics=1"
                textBox1.Text += descriptor + Environment.NewLine;

                foreach (var characteristics in descriptor.Characteristics) {
                    // "1920x1080 [JPEG, 30fps]"
                    // "640x480 [YUYV, 60fps]"
                    textBox1.Text += characteristics + Environment.NewLine;
                }
            }

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
                    PixelFormats.YUYV, 320, 180, 30);
                //320x180 [YUYV, 30,000fps]
                //var characteristics = new VideoCharacteristics(
                //    PixelFormats.JPEG, 640, 480, 30);
                //640x480 [JPEG, 15,000fps]
#else
                // Or, you could choice from device descriptor:
                // Hint: Show up video characteristics into ComboBox and like.
                var characteristics = descriptor0.Characteristics[0];
#endif

                textBox1.Text += Environment.NewLine;
                textBox1.Text += "Selected " + characteristics.ToString() + Environment.NewLine;

                captureDevice = await descriptor0.OpenAsync(
                characteristics,
                OnPixelBufferArrived);

                await captureDevice.StartAsync();
            }

        }

        // ========================================================================================
        private void OnPixelBufferArrived(PixelBufferScope bufferScope) {


            try {

                if (bufferScope != null) {
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

                    label1.Text = "Image size = " + image.Length.ToString();

                    label2.Text = "Frame index = " + bufferScope.Buffer.FrameIndex.ToString();

                    using (var stream = image.AsStream()) {
                        var bmp_org = Image.FromStream(stream);
                        pictureBox1.Image = bmp_org;
                    }

                    Bitmap bmp = new Bitmap(320, 180);
                    Graphics gfx = Graphics.FromImage(bmp);

                    for (int posy = 0; posy < 180; posy++)
                        for (int posx = 0; posx < 320; posx++) {

                            int pixel_sum = 0;
                            for (int dx = 0; dx < 3; dx++) {
                                var pos1 = (179 - posy) * 320 * 3 + posx * 3 + dx;
                                pixel_sum = image[pos1 + dx];
                            }

                            byte pixel = (byte)(pixel_sum / 3);
                            var pos2 = posy * 320 * 3 + posx * 3;

                            Color kolor;

                            if (pixel_sum > prog) {
                                kolor = Color.FromArgb(255, 255, 255);
                            } else {
                                kolor = Color.FromArgb(0, 0, 0);
                            }

                            bmp.SetPixel(posx, posy, kolor);

                        }

                    // C64 part

                    for (int posy1 = 0; posy1 < 172; posy1 += 8) {
                        for (int posx = 0; posx < 320; posx += 8) {
                            for (int posy2 = 0; posy2 < 8; posy2++) {

                                byte pixel_sum = 0;

                                for (int dx = 0; dx < 8; dx++) {
                                    //var pixel = bmp.GetPixel(1, 1);
                                    var pixel = bmp.GetPixel(posx + dx, posy1 + posy2);

                                    if (pixel.R > 0) {
                                        pixel_sum += (byte)(1 << (7 - dx));
                                    }

                                }

                                c64bmp[posy1 * 320 / 8 + posx + posy2] = pixel_sum;
                            }
                        }
                    }

                    pictureBox2.Image = bmp;

                    bufferScope.ReleaseNow();
                }

            } catch (Exception ex) {
                MessageBox.Show(ex.Message + ex.StackTrace, "Ultima Video Player 2");
            }
        }

        // ========================================================================================
        private void trackBar1_Scroll(object sender, EventArgs e) {
            prog = trackBar1.Value;
            label3.Text = "Prog " + prog.ToString();
        }

        // ========================================================================================
        public static byte[] Combine(byte[] first, byte[] second) {
            byte[] bytes = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, bytes, 0, first.Length);
            Buffer.BlockCopy(second, 0, bytes, first.Length, second.Length);
            return bytes;
        }

        // ========================================================================================
        public void Ultimate() {

            string ipaddr = "192.168.8.123";
            mySocket = Connect(ipaddr);

            var buf = new byte[] {
                        0x06,
                        0xff,
                        0x42,
                        0x1f,
                        0x00,
                        0x20
                        };

            while (mySocket.Connected) {

                bytes_sent += mySocket.Send(Combine(buf, c64bmp));

            }

        }

        // ========================================================================================
        public Socket Connect(string host) {
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

        // ========================================================================================
        private void timer1_Tick(object sender, EventArgs e) {
            label4.Text = mySocket.Connected ? "Connected" : "Not connected";

            label5.Text = "Bytes sent " + bytes_sent.ToString();
        }

        // ========================================================================================
        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {

            mySocket.Disconnect(false);
            mySocket.Close();

            captureDevice.StopAsync();
            captureDevice.Dispose();
            captureDevice = null;
        }
    }
}
