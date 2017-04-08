using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Enumeration;
using Windows.Devices.Spi;
using Windows.Devices.Gpio;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CubeController
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private SpiDevice SpiCube;
        private GpioController IoController;
        private GpioPin LatchPin;
        private GpioPin TogglePin;
        private DispatcherTimer CubeController;

        private byte[] red0 = new byte[64], red1 = new byte[64], red2 = new byte[64], red3 = new byte[64];
        private byte[] gre0 = new byte[64], gre1 = new byte[64], gre2 = new byte[64], gre3 = new byte[64];
        private byte[] blu0 = new byte[64], blu1 = new byte[64], blu2 = new byte[64], blu3 = new byte[64];
        private byte[] anode = { 1, 2, 4, 8, 16, 32, 64, 128 };
        private GpioPinValue High = GpioPinValue.High;
        private GpioPinValue Low = GpioPinValue.Low;

        private int BAM_Bit = 0, BAM_Counter = 0, level = 0;
        public MainPage()
        {
            this.InitializeComponent();

            Unloaded += MainPage_Unloaded;

            InitAll();
        }

        private async void InitAll()
        {
            try
            {
                InitGpio();
                await InitSpi();
                CubeController = new DispatcherTimer();
                CubeController.Interval = TimeSpan.FromMilliseconds(124);
                CubeController.Tick += loadCube;
                CubeController.Start();
            }
            catch (Exception ex)
            {
                ExceptionBox.Text = "Exception: " + ex.Message;
                if (ex.InnerException != null)
                {
                    ExceptionBox.Text += "\nInner Exception: " + ex.InnerException.Message;
                }
                return;
            }

            ExceptionBox.Text = "Status: Initialized";
        }

        private async Task InitSpi()
        {
            try
            {
                var settings = new SpiConnectionSettings(0);
                settings.ClockFrequency = 8000000;
                settings.Mode = SpiMode.Mode0;
                var controller = await SpiController.GetDefaultAsync();
                SpiCube = controller.GetDevice(settings);
            }
            catch (Exception ex)
            {
                throw new Exception("SPI Initialization Failed", ex);
            }
        }

        private void InitGpio()
        {
            IoController = GpioController.GetDefault(); /* Get the default GPIO controller on the system */
            if (IoController == null)
            {
                throw new Exception("GPIO does not exist on the current system.");
            }

            /* Initialize a pin as output for the Data/Command line on the display  */
            LatchPin = IoController.OpenPin(23);
            LatchPin.Write(GpioPinValue.Low);
            LatchPin.SetDriveMode(GpioPinDriveMode.Output);

            /* Initialize a pin as output for the hardware Reset line on the display */
            TogglePin = IoController.OpenPin(24);
            TogglePin.Write(GpioPinValue.High);
            TogglePin.SetDriveMode(GpioPinDriveMode.Output);

        }

        private void MainPage_Unloaded(object sender, object args)
        {
            /* Cleanup */
            CubeController.Stop();
            SpiCube.Dispose();
            TogglePin.Dispose();
            LatchPin.Dispose();
        }

        private void loadCube(object sender, object e)
        {
            byte[] output;
            TogglePin.Write(High);
            if (BAM_Counter == 8) BAM_Bit++;
            if (BAM_Counter == 24) BAM_Bit++;
            if (BAM_Counter == 56) BAM_Bit++;
            BAM_Counter++;

            switch (BAM_Bit)
            {
                case 0:
                    for (var shift_out = level; shift_out < level + 8; shift_out++) SpiCube.Write(red0[shift_out]);
                    break;
            }
        }
    }
}
