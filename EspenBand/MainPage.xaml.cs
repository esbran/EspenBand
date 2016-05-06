using Microsoft.Band;
using Windows.UI.Xaml.Controls;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Band.Sensors;
using System.Text;
using Newtonsoft.Json;


namespace iClassroom
{
    public sealed partial class MainPage : Page
    {
        private IBandClient _bandClient;
        private IBandInfo _bandInfo;
        private string connectionString = "Endpoint=sb://esbranbandhub.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedSecretValue=teefCtKAq7DJjBcNU6UjuPeIGTr7xik0swYoixHmHiE=";
        private string eventHubNamespace = "esbranBandHub";
        private string eventHubName = "esbranbandhub";
        private string policyName = "appkey";
        private string key = "tB8+auPxHjzE1dDl7SltFLuQdyO1mpluP6xCip9kdtA=";
        private string partitionkey = "esbranparkey";
        private Sensor _sensor;

        public MainPage()
        {
            this.InitializeComponent();
            setupConnection();
            
            Loaded += OnLoaded;
        }


        private Amqp.Session session;
        private Amqp.Connection connection;
        private Amqp.SenderLink senderlink;
        private void setupConnection()
        {
            Amqp.Address address = new Amqp.Address(
                string.Format("{0}.servicebus.windows.net", eventHubNamespace),
                5671, policyName, key);
            connection = new Amqp.Connection(address);
            session = new Amqp.Session(connection);
            senderlink = new Amqp.SenderLink(session,
                    string.Format("send-link:{0}", eventHubName), eventHubName);
            _sensor = new Sensor();
            
        }

        int HrData = 0;
        int GsrData = 0;
        int TmpData = 0;
        
        
        private void SendMsg()
        {
            _sensor.sensor = "Band";
            _sensor.Time = System.DateTime.UtcNow;

            //string sensordata = "{ \"sensor\":\"Band\", \"Hr\":" + HrData +
            //     ", \"Gsr\":" + GsrData +
            //     ", \"Tmp\":" + TmpData +
            //     ", \"Time\":" + System.DateTime.UtcNow.ToString() + "}";
            string sensordata = JsonConvert.SerializeObject(_sensor);
            try
            {
                
                Amqp.Message message = new Amqp.Message()
                {
                    BodySection = new Amqp.Framing.Data()
                    {
                        Binary = System.Text.Encoding.UTF8.GetBytes(sensordata)
                    }
                };

                message.MessageAnnotations = new Amqp.Framing.MessageAnnotations();
                message.MessageAnnotations[new Amqp.Types.Symbol("x-opt-partition-key")] =
                   string.Format("pk:", partitionkey);

                senderlink.Send(message);
            }catch (Exception e)
            { var a = e; }
        }


        private async void OnLoaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {

            if (_bandClient != null)
                return;

            var bands = await BandClientManager.Instance.GetBandsAsync();
            _bandInfo = bands.First();

            _bandClient = await BandClientManager.Instance.ConnectAsync(_bandInfo);

            var uc = _bandClient.SensorManager.HeartRate.GetCurrentUserConsent();
            bool isConsented = false;
            if (uc == UserConsent.NotSpecified)
            {
                isConsented = await _bandClient.SensorManager.HeartRate.RequestUserConsentAsync();
            }

            if (isConsented || uc == UserConsent.Granted)
            {
                _bandClient.SensorManager.HeartRate.ReadingChanged += async (obj, ev) =>
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                                        {
                                            HeartRateDisplay.Text = ev.SensorReading.HeartRate.ToString();
                                            _sensor.Hr = ev.SensorReading.HeartRate;
                                            SendMsg();
                                        });

                };
                await _bandClient.SensorManager.HeartRate.StartReadingsAsync();

            }
            if (isConsented || uc == UserConsent.Granted)
            {
                _bandClient.SensorManager.Gsr.ReadingChanged += async (obj, ev) =>
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        GsrDisplay.Text = ev.SensorReading.Resistance.ToString();
                        _sensor.Gsr = ev.SensorReading.Resistance;
                        SendMsg();
                    });
                };
                await _bandClient.SensorManager.Gsr.StartReadingsAsync();
            }
            if (isConsented || uc == UserConsent.Granted)
            {
                _bandClient.SensorManager.SkinTemperature.ReadingChanged += async (obj, ev) =>
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        TmpDisplay.Text = ev.SensorReading.Temperature.ToString();
                        _sensor.Tmp = ev.SensorReading.Temperature;
                        SendMsg();
                    });
                };

                await _bandClient.SensorManager.SkinTemperature.StartReadingsAsync();
            }

        }
    }

}
