using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using System.Diagnostics;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;
using System.Threading;
using System.Threading.Tasks;

/*
 * Rube 2 by Jordan Balagot & Ayzenberg
 * https://www.facebook.com/MicrosoftStore/videos/10155054904077480/
 * 
 */

namespace Rube2
{
    /// <summary>
    /// A Rube Goldberg Device using a Raspbery Pi. 
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //Set up pins for LEDs, solenoid, button, relay, and LED matrix sign (controlled by arduino)
        private const int LED_PIN = 5;
        private const int LED_PIN2 = 6;
        private const int SOLENOID_PIN = 17;
        private const int BUTTON_PIN = 27;
        private const int RELAY_PIN = 22;
        private const int SIGN_PIN = 26;
        private GpioPin ledPin;
        private GpioPin ledPin2;
        private GpioPin solenoidPin;
        private GpioPin buttonPin;
        private GpioPin relayPin;
        private GpioPin signPin;

        private DispatcherTimer twoSecondTimer;
        private DispatcherTimer solenoidTimer;
        private SolidColorBrush redBrush = new SolidColorBrush(Windows.UI.Colors.Red);
        private SolidColorBrush grayBrush = new SolidColorBrush(Windows.UI.Colors.LightGray);
        private SolidColorBrush blackBrush = new SolidColorBrush(Windows.UI.Colors.Black);
        private SolidColorBrush blueBrush = new SolidColorBrush(Windows.UI.ColorHelper.FromArgb(0,23,133,217));
        
        enum AdcDevice { NONE, MCP3002, MCP3208, MCP3008 };

        //using the MCP3002 ADC for reading the proximity sensor using SPI
        private AdcDevice ADC_DEVICE = AdcDevice.MCP3002;

        private const string SPI_CONTROLLER_NAME = "SPI0";  /* Friendly name for Raspberry Pi 2 SPI controller          */
        private const Int32 SPI_CHIP_SELECT_LINE = 0;       /* Line 0 maps to physical pin number 24 on the Rpi2        */
        private SpiDevice SpiADC;

        private const byte MCP3002_CONFIG = 0x68; /* 01101000 channel configuration data for the MCP3002 */
        private const byte MCP3208_CONFIG = 0x06; /* 00000110 channel configuration data for the MCP3208 */
        private readonly byte[] MCP3008_CONFIG = { 0x01, 0x80 }; /* 00000001 10000000 channel configuration data for the MCP3008 */

        private int adcValue;
        private int lastAdcValue; //disabled, needed it to be more sensitive
        private int countDown = 2; //seconds until firing

        //servo range
        const int servoMin = 100;  // Min pulse length out of 4095
        const int servoMax = 520;  // Max pulse length out of 4095
        const int servoCenter = 520;
        private Boolean servoTriggered = true; //disable until start

        public MainPage()
        {
            InitializeComponent();
            lastAdcValue = 0;
            InitAll();
            //wait two seconds but update UI each second
            twoSecondTimer = new DispatcherTimer();
            twoSecondTimer.Interval = TimeSpan.FromMilliseconds(1000);
            //fire solenoid
            twoSecondTimer.Tick += Timer_Tick;
            solenoidTimer = new DispatcherTimer();
            solenoidTimer.Interval = TimeSpan.FromMilliseconds(300);
            solenoidTimer.Tick += Solenoid_Tick;
            Debug.WriteLine("Starting");
        }

        private async void InitAll()
        {

            try
            {
                InitGPIO();         /* Initialize GPIO                                            */
                await InitSPI();    /* Initialize the SPI bus for communicating with the ADC      */

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return;
            }
        }

        private void InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                ledPin = null;
                ledPin2 = null;
                return;
            }

            //Init pins
            ledPin = gpio.OpenPin(LED_PIN);
            ledPin2 = gpio.OpenPin(LED_PIN2);
            solenoidPin = gpio.OpenPin(SOLENOID_PIN);
            buttonPin = gpio.OpenPin(BUTTON_PIN);
            relayPin = gpio.OpenPin(RELAY_PIN);
            signPin = gpio.OpenPin(SIGN_PIN);
            ledPin.SetDriveMode(GpioPinDriveMode.Output);
            ledPin2.SetDriveMode(GpioPinDriveMode.Output);
            solenoidPin.SetDriveMode(GpioPinDriveMode.Output);
            relayPin.SetDriveMode(GpioPinDriveMode.Output);
            signPin.SetDriveMode(GpioPinDriveMode.Output);
            // Check if input pull-up resistors are supported
            if (buttonPin.IsDriveModeSupported(GpioPinDriveMode.InputPullUp))
                buttonPin.SetDriveMode(GpioPinDriveMode.InputPullUp);
            else
            {
                //buttonpin.SetDriveMode(GpioPinDriveMode.Input);
                Debug.WriteLine("Could not set Pull up pin state");
                return;
            }
            buttonPin.DebounceTimeout = TimeSpan.FromMilliseconds(50);
            // Register for the ValueChanged event so our buttonPin_ValueChanged 
            // function is called when the button is pressed
            buttonPin.ValueChanged += buttonpin_ValueChanged;

            //Init LEDs High (Off)
            ledPin.Write(GpioPinValue.Low);
            ledPin2.Write(GpioPinValue.Low);
            //Init solenoid Low (Off)
            solenoidPin.Write(GpioPinValue.Low);
            //Init relay Low (Off)
            relayPin.Write(GpioPinValue.Low);
            signPin.Write(GpioPinValue.Low);
        }

        /* Read from the ADC, update the UI */
        private void Spi_Tick(object state)
        {
            ReadADC();
        }

        public void ReadADC()
        {
            byte[] readBuffer = new byte[3]; /* Buffer to hold read data*/
            byte[] writeBuffer = new byte[3] { 0x00, 0x00, 0x00 };

            /* Setup the appropriate ADC configuration byte */
            switch (ADC_DEVICE)
            {
                case AdcDevice.MCP3002:
                    writeBuffer[0] = MCP3002_CONFIG;
                    break;
                case AdcDevice.MCP3208:
                    writeBuffer[0] = MCP3208_CONFIG;
                    break;
                case AdcDevice.MCP3008:
                    writeBuffer[0] = MCP3008_CONFIG[0];
                    writeBuffer[1] = MCP3008_CONFIG[1];
                    break;
            }

            SpiADC.TransferFullDuplex(writeBuffer, readBuffer); /* Read data from the ADC                           */
            adcValue = convertToInt(readBuffer);                /* Convert the returned bytes into an integer value */

            /* UI updates must be invoked on the UI thread */
            var task = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                proximityStatus.Text = adcValue.ToString();         /* Display the value on screen                      */
                if(adcValue > 500) //&& lastAdcValue > 500
                {
                    if (!servoTriggered)
                    {
                        proximityRect.Fill = redBrush;
                        proximityStatus.Text = "TRIGGERED";
                        //turn on LEDs
                        ledPin.Write(GpioPinValue.High);
                        ledPin2.Write(GpioPinValue.High);
                        using (var hat = new Adafruit.Pwm.PwmController())
                        {
                            //pull servo linear actuator
                            hat.SetPulseParameters(15, servoMin, false);
                            Task.Delay(TimeSpan.FromSeconds(4)).Wait();
                            //return servo to start position
                            hat.SetPulseParameters(15, servoMax, false);
                            Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                            //Trigger arduino to scroll text on sign
                            signPin.Write(GpioPinValue.High);
                        }
                        servoTriggered = true;
                    }
                }
                //lastAdcValue = adcValue;
            });
        }

        /* Convert the raw ADC bytes to an integer */
        public int convertToInt(byte[] data)
        {
            int result = 0;
            switch (ADC_DEVICE)
            {
                case AdcDevice.MCP3002:
                    result = data[0] & 0x03;
                    result <<= 8;
                    result += data[1];
                    break;
                case AdcDevice.MCP3208:
                    result = data[1] & 0x0F;
                    result <<= 8;
                    result += data[2];
                    break;
                case AdcDevice.MCP3008:
                    result = data[1] & 0x03;
                    result <<= 8;
                    result += data[2];
                    break;
            }
            return result;
        }

        /* Trigger relay (with fan plugged in) after 2 seconds after button press */
        private void buttonpin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (e.Edge == GpioPinEdge.FallingEdge)
            {
                //wait then turn on fan
                Task.Delay(TimeSpan.FromSeconds(2)).Wait();
                relayPin.Write(GpioPinValue.High);
            }

            var task = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                if (e.Edge == GpioPinEdge.FallingEdge)
                {
                    buttonStatus.Text = "TRIGGERED";
                }
            });
        }

        /* Countdown for Solenoid */
        private void Timer_Tick(object sender, object e)
        {
            var task2 = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () => {
                solenoidStatus.Text = "Firing in " + countDown;
            });

            countDown--;
            
            if (countDown < 1)
            {
                solenoidPin.Write(GpioPinValue.High);
                var task3 = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () => {
                    solenoidStatus.Text = "FIRE";
                });
                twoSecondTimer.Stop();
                solenoidTimer.Start();
            }
        }

        /* Timer to release solenoid */
        private void Solenoid_Tick(object sender, object e)
        {
            solenoidPin.Write(GpioPinValue.Low);
            solenoidStatus.Text = "OFF";
            solenoidTimer.Stop();
        }

        /* SPI Init for ADC chip */
        private async Task InitSPI()
        {
            try
            {
                var settings = new SpiConnectionSettings(SPI_CHIP_SELECT_LINE);
                settings.ClockFrequency = 500000;   /* 0.5MHz clock rate                                        */
                settings.Mode = SpiMode.Mode0;      /* The ADC expects idle-low clock polarity so we use Mode0  */

                var controller = await SpiController.GetDefaultAsync();
                SpiADC = controller.GetDevice(settings);
            }

            catch (Exception ex)
            {
                throw new Exception("SPI Initialization Failed", ex);
            }
        }

        /* Start the whole contraption. Also enable the servo to respond to the proximity sensor */
        private void startbutton_Click(System.Object sender, RoutedEventArgs e)
        {
            startbutton.Content = "RUNNING";
            solenoidStatus.Text = "Firing in 2";
            twoSecondTimer.Start();
            servoTriggered = false;
        }

        /* Reset function, useful when working with rube goldberg devices. Disable
         * the servo so it doesn't accentally squirt whipped cream when proximity sensor is activated */
        private void resetbutton_Click(object sender, RoutedEventArgs e)
        {
            //turn off LEDs
            ledPin.Write(GpioPinValue.Low);
            ledPin2.Write(GpioPinValue.Low);
            //turn off solenoid
            solenoidPin.Write(GpioPinValue.Low);
            //reset statuses
            solenoidStatus.Text = "OFF";
            buttonStatus.Text = "OFF";
            //turn off relay
            relayPin.Write(GpioPinValue.Low);
            //reset solenoid status background color
            SolidColorBrush blueBrush = ColorToBrush("#1785d9");
            proximityRect.Fill = blueBrush;
            //reset countdown
            countDown = 2;
            //reset status
            startbutton.Content = "START";
            //disable servo from re-firing
            servoTriggered = true; //start sets it false
            //reset sign
            signPin.Write(GpioPinValue.Low);
        }

        /* Utility for converting hex to brush */
        public static SolidColorBrush ColorToBrush(string color)
        {
            color = color.Replace("#", "");
            if (color.Length == 6)
            {
                return new SolidColorBrush(Windows.UI.ColorHelper.FromArgb(255,
                    byte.Parse(color.Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
                    byte.Parse(color.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                    byte.Parse(color.Substring(4, 2), System.Globalization.NumberStyles.HexNumber)));
            }
            else
            {
                return null;
            }
        }

        /* Clean up on close */
        private void MainPage_Unloaded(object sender, object args)
        {
            ledPin.Write(GpioPinValue.Low);
            ledPin2.Write(GpioPinValue.Low);
            solenoidPin.Write(GpioPinValue.Low);
            solenoidStatus.Text = "OFF";
            buttonStatus.Text = "OFF";
            relayPin.Write(GpioPinValue.Low);
            
            if (SpiADC != null)
            {
                SpiADC.Dispose();
            }

            if (ledPin != null)
            {
                ledPin.Dispose();
            }

            if (ledPin2 != null)
            {
                ledPin2.Dispose();
            }

            if (solenoidPin != null)
            {
                solenoidPin.Dispose();
            }

            if (buttonPin != null)
            {
                buttonPin.Dispose();
            }

            if (relayPin != null)
            {
                relayPin.Dispose();
            }
        }
    }
}
