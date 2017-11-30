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

namespace CubeController
{
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

        private int BAM_Bit = 0, BAM_Counter = 0, level = 0, anodelevel = 0;
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
            IoController = GpioController.GetDefault();
            if (IoController == null)
            {
                throw new Exception("GPIO does not exist on the current system.");
            }
            LatchPin = IoController.OpenPin(23);
            LatchPin.Write(GpioPinValue.Low);
            LatchPin.SetDriveMode(GpioPinDriveMode.Output);
            TogglePin = IoController.OpenPin(24);
            TogglePin.Write(GpioPinValue.High);
            TogglePin.SetDriveMode(GpioPinDriveMode.Output);

        }

        private void MainPage_Unloaded(object sender, object args)
        {
            CubeController.Stop();
            SpiCube.Dispose();
            TogglePin.Dispose();
            LatchPin.Dispose();
        }

        private void loadCube(object sender, object e)
        {
            byte[] output = { };
            TogglePin.Write(High);
            if (BAM_Counter == 8) BAM_Bit++;
            if (BAM_Counter == 24) BAM_Bit++;
            if (BAM_Counter == 56) BAM_Bit++;
            BAM_Counter++;

            switch (BAM_Bit)
            {
                case 0:
                    for (var shift_out = level; shift_out < level + 8; shift_out++) output[shift_out] = red0[shift_out];
                    for (var shift_out = level; shift_out < level + 8; shift_out++) output[shift_out + 8] = gre0[shift_out];
                    for (var shift_out = level; shift_out < level + 8; shift_out++) output[shift_out + 16] = blu0[shift_out];
                    break;
                case 1:
                    for (var shift_out = level; shift_out < level + 8; shift_out++) output[shift_out] = red1[shift_out];
                    for (var shift_out = level; shift_out < level + 8; shift_out++) output[shift_out + 8] = gre1[shift_out];
                    for (var shift_out = level; shift_out < level + 8; shift_out++) output[shift_out + 16] = blu1[shift_out];
                    break;
                case 2:
                    for (var shift_out = level; shift_out < level + 8; shift_out++) output[shift_out] = red2[shift_out];
                    for (var shift_out = level; shift_out < level + 8; shift_out++) output[shift_out + 8] = gre2[shift_out];
                    for (var shift_out = level; shift_out < level + 8; shift_out++) output[shift_out + 16] = blu2[shift_out];
                    break;
                case 3:
                    for (var shift_out = level; shift_out < level + 8; shift_out++) output[shift_out] = red3[shift_out];
                    for (var shift_out = level; shift_out < level + 8; shift_out++) output[shift_out + 8] = gre3[shift_out];
                    for (var shift_out = level; shift_out < level + 8; shift_out++) output[shift_out + 16] = blu3[shift_out];
                    if (BAM_Counter == 120)
                    {
                        BAM_Counter = 0;
                        BAM_Bit = 0;
                    }
                    break;
            }
            output[24] = anode[anodelevel];

            SpiCube.Write(output);
            LatchPin.Write(High);
            LatchPin.Write(Low);
            TogglePin.Write(Low);
            anodelevel++;
            level += 8;
            if (anodelevel == 8) anodelevel = 0;
            if (level == 64) level = 0;
        }

        private void LED(int level, int row, int column, byte red, byte green, byte blue)
        {
            if(level<0)level=0;if(level>7)level=7;if(row<0)row=0;if(row>7)row=7;if(column<0)column=0;if(column>7)column=7;if(red<0)red=0;if(red>15)red=15;if(green<0)green=0;if(green>15)green=15;if(blue<0)blue=0;if(blue>15)blue=15;
            int whichbyte = ((level * 64) + (row * 8) + column) / 8;
            int wholebyte = (level * 64) + (row * 8) + column;
            int bitpos = wholebyte - (8 * whichbyte);
            red0[whichbyte] = bitWrite(red0[whichbyte], wholebyte - (8 * whichbyte), bitRead(red, 3));
            red1[whichbyte] = bitWrite(red1[whichbyte], wholebyte - (8 * whichbyte), bitRead(red, 2));
            red2[whichbyte] = bitWrite(red2[whichbyte], wholebyte - (8 * whichbyte), bitRead(red, 1));
            red3[whichbyte] = bitWrite(red3[whichbyte], wholebyte - (8 * whichbyte), bitRead(red, 0));
            gre0[whichbyte] = bitWrite(gre0[whichbyte], wholebyte - (8 * whichbyte), bitRead(green, 3));
            gre1[whichbyte] = bitWrite(gre1[whichbyte], wholebyte - (8 * whichbyte), bitRead(green, 2));
            gre2[whichbyte] = bitWrite(gre2[whichbyte], wholebyte - (8 * whichbyte), bitRead(green, 1));
            gre3[whichbyte] = bitWrite(gre3[whichbyte], wholebyte - (8 * whichbyte), bitRead(green, 0));
            blu0[whichbyte] = bitWrite(blu0[whichbyte], wholebyte - (8 * whichbyte), bitRead(blue, 3));
            blu1[whichbyte] = bitWrite(blu1[whichbyte], wholebyte - (8 * whichbyte), bitRead(blue, 2));
            blu2[whichbyte] = bitWrite(blu2[whichbyte], wholebyte - (8 * whichbyte), bitRead(blue, 1));
            blu3[whichbyte] = bitWrite(blu3[whichbyte], wholebyte - (8 * whichbyte), bitRead(blue, 0));
        }

        private byte bitWrite(byte var, int where, char what)
        {
            string output = "";
            string input = Convert.ToString(var, 2);
            for (int i = 0; i < 8; i++)
            {
                if(i != where)
                {
                    output += input[i];
                }
                else
                {
                    output += what;
                }
            }
            return Convert.ToByte(output, 2);
        }

        private char bitRead(byte var, int where)
        {
            return Convert.ToString(var, 2)[where];
        }
    }
}
