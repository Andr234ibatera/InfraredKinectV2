using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Kinect;
using System.IO;
using System.Text.RegularExpressions;
using System.Timers;

namespace Infrared
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor kinectSensor;
        MultiSourceFrameReader reader;

        //Global Control Variable
        bool capturing = false;
        double count = 0;
        string path = "";
        bool off = true;

        public MainWindow()
        {
            InitializeComponent();
            kinectSensor = KinectSensor.GetDefault();
        }

        private void BtStart_Click(object sender, RoutedEventArgs e)
        {
            if (off)
            {
                if (kinectSensor != null)
                {
                    BtStart.Content = "Stop";
                    kinectSensor.Open();
                    reader = kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Infrared);
                    reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
                    gettingPath();
                    off = false;
                    count = 0;
                }
            }
            else{
                BtStart.Content = "Start";
                kinectSensor.Close();
                off = true;
            }
            
        }

        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();
            
            using (var frame = reference.InfraredFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    int width = frame.FrameDescription.Width;
                    int height = frame.FrameDescription.Height;
                    PixelFormat format = PixelFormats.Bgr32;

                    ushort[] frameData = new ushort[width * height];
                    byte[] pixels = new byte[width * height * (format.BitsPerPixel + 7) / 8];

                    frame.CopyFrameDataToArray(frameData);

                    int colorIndex = 0;
                    for (int infraredIndex = 0; infraredIndex < frameData.Length; infraredIndex++)
                    {
                        ushort ir = frameData[infraredIndex];

                        byte intensity = (byte)(ir >> 7);

                        pixels[colorIndex++] = (byte)(intensity / 0.4); // Blue
                        pixels[colorIndex++] = (byte)(intensity / 1); // Green   
                        pixels[colorIndex++] = (byte)(intensity / 1); // Red

                        colorIndex++;
                    }

                    int stride = width * format.BitsPerPixel / 8;

                    BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);

                    image.Source = bitmap;
                    if (capturing)
                    {
                        count += 1;
                        CaptureImage(bitmap, "Infrared", count);
                    }
                }
            }
        }

        public void CaptureImage(BitmapSource bitmap, String type, Double count)
        {
            try
            {
                using (FileStream fileStream = new FileStream(path + "\\" + getEmotion() + count + ".png", FileMode.Create))
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    encoder.Save(fileStream);
                }

            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
                Close();
            }
        }

        private string getEmotion()
        {
            string content = rdAlegria.IsChecked == true ? (string)rdAlegria.Content : "";
            content += rdTristeza.IsChecked == true ? (string)rdTristeza.Content : "";
            content += rdMedo.IsChecked == true ? (string)rdMedo.Content : "";
            content += rdNojo.IsChecked == true ? (string)rdNojo.Content : "";
            content += rdRaiva.IsChecked == true ? (string)rdRaiva.Content : "";
            content += rdSurpresa.IsChecked == true ? (string)rdSurpresa.Content : "";

            return content;
        }

        private void gettingPath()
        {
            path = "";
            var currentDirectory = System.IO.Directory.GetCurrentDirectory();
            string[] parts = currentDirectory.Split('\\');
            for (int i = 0; i < (parts.Length - 5); i++)
            {
                path += parts[i] + "\\";
            }
            path += "data";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path += "\\" + new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            Directory.CreateDirectory(path);
        }

        private void BtCapture_Click(object sender, RoutedEventArgs e)
        {
            if (capturing == true)
            {
                capturing = false;
                path = "";
                BtCapturar.Content = "Start Cap.";
            }
            else
            {
                capturing = true;
                BtCapturar.Content = "Stop Cap.";
            }
        }
        
    }
}
