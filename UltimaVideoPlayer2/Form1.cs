using FlashCap;
using FlashCap.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UltimaVideoPlayer2 {
    public partial class Form1 : Form {

        // ========================================================================================

        // Constructed capture device.
        private CaptureDevice captureDevice;

        private SynchronizationContext synchContext;

        int prog = 60;

        // ========================================================================================
        public Form1() {
            InitializeComponent();
        }

        // ========================================================================================
        private void Form1_Load(object sender, EventArgs e) {

            trackBar1.Value = prog;
            label3.Text = "Prog " + prog.ToString();

            textBox1.Clear();

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

            Camera();

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

                    byte[] c64bmp = new byte[8000];

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

                    pictureBox1.Image = bmp;

                    bufferScope.ReleaseNow();
                }

            } catch (Exception ex) {
                MessageBox.Show(ex.Message + ex.StackTrace, "Ultima Video Player 2");
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e) {
            prog = trackBar1.Value;
            label3.Text = "Prog " + prog.ToString();
        }
    }
}
