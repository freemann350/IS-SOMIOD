using RestSharp;
using System;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace Barrier
{
    public partial class Barrier : Form
    {
        string baseURI = "http://localhost:2245";
        string appName = "Barrier";
        string containerName = "barrier";
        string subscriptionName = "barrierSubscription";
        RestClient client = null;

        private void checkIfAppExists()
        {
            var request = new RestRequest("api/somiod/" + appName, Method.Get)
            {
                RequestFormat = DataFormat.Xml
            };

            RestResponse response;
            do
            {
                response = client.Execute(request);

                if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.BadRequest)
                    registerApp();

            } while (response.StatusCode == HttpStatusCode.BadRequest);
        }

        private void registerApp()
        {
            DialogResult result = 0;
            XmlDocument doc = new XmlDocument();
            XmlElement application = doc.CreateElement("Application");
            XmlElement name = doc.CreateElement("name");
            name.InnerText = appName;
            application.AppendChild(name);
            doc.AppendChild(application);

            string xmlBody = doc.OuterXml.Replace("\r\n", "").Replace("\r", "").Replace("\n", "");

            do
            {
                var request = new RestRequest("api/somiod/", Method.Post)
                {
                    RequestFormat = DataFormat.Xml
                };

                request.AddParameter("application/xml", xmlBody, ParameterType.RequestBody);
                RestResponse response = client.Execute(request);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    result = MessageBox.Show("Error '" + response.StatusCode + "', retry?", "Error connecting", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                }

            } while (result == DialogResult.Yes);
        }

        private void checkIfContainerExists()
        {
            var request = new RestRequest("api/somiod/" + appName + "/" + containerName, Method.Get)
            {
                RequestFormat = DataFormat.Xml
            };

            RestResponse response;
            do
            {
                response = client.Execute(request);

                if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.BadRequest)
                    registerContainer();

            } while (response.StatusCode == HttpStatusCode.BadRequest);
        }

        private void registerContainer()
        {
            DialogResult result = 0;

            XmlDocument doc = new XmlDocument();
            XmlElement application = doc.CreateElement("Container");
            XmlElement name = doc.CreateElement("name");
            name.InnerText = containerName;
            application.AppendChild(name);
            doc.AppendChild(application);

            string xmlBody = doc.OuterXml.Replace("\r\n", "").Replace("\r", "").Replace("\n", "");

            do
            {

                var request = new RestRequest("api/somiod/" + appName, Method.Post)
                {
                    RequestFormat = DataFormat.Xml
                };

                request.AddParameter("application/xml", xmlBody, ParameterType.RequestBody);
                RestResponse response = client.Execute(request);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    result = MessageBox.Show("Error '" + response.StatusCode + "', retry?", "Error connecting", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                }

            } while (result == DialogResult.Yes);
        }

        private void checkIfSubscriptionExists()
        {
            var request = new RestRequest("api/somiod/" + appName + "/" + containerName + "/subscriptions/" + subscriptionName, Method.Get)
            {
                RequestFormat = DataFormat.Xml
            };

            RestResponse response;
            do
            {
                response = client.Execute(request);

                if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.BadRequest)
                    registerSubscription();

            } while (response.StatusCode == HttpStatusCode.BadRequest);
        }

        private void registerSubscription()
        {
            DialogResult result = 0;

            XmlDocument doc = new XmlDocument();
            XmlElement requestBody = doc.CreateElement("Request");
            XmlElement resType = doc.CreateElement("res_type");
            resType.InnerText = "subscription";
            XmlElement xmlData = doc.CreateElement("xmlData");
            XmlElement containerSubscriptions = doc.CreateElement("ContainerSubscriptions");

            XmlElement name = doc.CreateElement("name");
            name.InnerText = subscriptionName;

            XmlElement eventType = doc.CreateElement("event_type");
            eventType.InnerText = "CREATE";

            XmlElement endpoint = doc.CreateElement("endpoint");
            endpoint.InnerText = "mqtt://192.168.1.1";

            containerSubscriptions.AppendChild(name);
            containerSubscriptions.AppendChild(eventType);
            containerSubscriptions.AppendChild(endpoint);
            xmlData.AppendChild(containerSubscriptions);
            requestBody.AppendChild(resType);
            requestBody.AppendChild(xmlData);
            doc.AppendChild(requestBody);

            string xmlBody = doc.OuterXml.Replace("\r\n", "").Replace("\r", "").Replace("\n", "");

            do
            {

                var request = new RestRequest("api/somiod/" + appName + "/" + containerName, Method.Post)
                {
                    RequestFormat = DataFormat.Xml
                };

                request.AddParameter("application/xml", xmlBody, ParameterType.RequestBody);
                RestResponse response = client.Execute(request);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    result = MessageBox.Show("Error '" + response.StatusCode + "', retry?", "Error connecting", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                }

            } while (result == DialogResult.Yes);
        }

        void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string state = Encoding.UTF8.GetString(e.Message);
            SetText(state);
        }

        public Barrier()
        {
            InitializeComponent();
        }

        private void Barrier_Shown(object sender, EventArgs e)
        {
            client = new RestClient(baseURI);

            checkIfAppExists();
            checkIfContainerExists();
            checkIfSubscriptionExists();
            MqttClient mClient = new MqttClient("127.0.0.1");
            string[] mStrTopicsInfo = { subscriptionName };

            mClient.Connect(Guid.NewGuid().ToString());
            if (!mClient.IsConnected)
            {
                MessageBox.Show("Error connecting to message broker...");
                return;
            }

            mClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            byte[] qosLevels = {
                MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE
            };
            mClient.Subscribe(mStrTopicsInfo, qosLevels);
        }

        delegate void SetTextCallback(string text);

        private void SetText(string text)
        {
            if (this.lblState.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                XDocument xmlDocument = XDocument.Parse(text);
                string stateValue = xmlDocument.Root.Element("state")?.Value;


                if (stateValue == "OFF") 
                { 
                    this.lblState.Text = "The gate is closed";
                    pbBarrier.Image = Properties.Resources.barrier_closed;
                }

                if (stateValue == "ON")
                {
                    this.lblState.Text = "The gate is open";
                    pbBarrier.Image = Properties.Resources.barrier_open;
                }
            }
        }

    }
}
