using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using RestSharp;
using System.Xml;

namespace BarrierControl
{
    public partial class BarrierControl : Form
    {
        string baseURI = "http://localhost:2245";
        string masterAppName = "BarrierControl";
        string slaveAppName = "Barrier";
        string slaveContainerName = "barrier";
        RestClient client = null;

        private void checkIfAppExists()
        {
            var request = new RestRequest("api/somiod/" + masterAppName, Method.Get)
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
            DialogResult result=0;
            XmlDocument doc = new XmlDocument();
            XmlElement application = doc.CreateElement("Application");
            XmlElement name = doc.CreateElement("name");
            name.InnerText = masterAppName;
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
                    result = MessageBox.Show("Error '"+ response.StatusCode + "', retry?", "Error connecting", MessageBoxButtons.YesNo,MessageBoxIcon.Error);
                }

            } while (result == DialogResult.Yes);
        }

        public BarrierControl()
        {
            InitializeComponent();
        }

        private void BarrierControl_Shown(object sender, EventArgs e)
        {
            client = new RestClient(baseURI);
            checkIfAppExists();
        }

        private string CreateXML(string name, Boolean state)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement request_body = doc.CreateElement("Request");
            XmlElement resType = doc.CreateElement("res_type");
            resType.InnerText = "data";
            XmlElement xmlData = doc.CreateElement("xmlData");
            XmlElement containerData = doc.CreateElement("ContainerData");
            XmlElement containerName = doc.CreateElement("name");
            containerName.InnerText = name;
            XmlElement content = doc.CreateElement("content");
            content.InnerText = state ? "<lampControl><state>ON</state></lampControl>" : "<lampControl><state>OFF</state></lampControl>";

            containerData.AppendChild(containerName);
            containerData.AppendChild(content);
            xmlData.AppendChild(containerData);
            request_body.AppendChild(resType);
            request_body.AppendChild(xmlData);
            doc.AppendChild(request_body);

            return doc.OuterXml.Replace("\r\n", "").Replace("\r", "").Replace("\n", "");
        }

        private async void btnOpen_Click(object sender, EventArgs e)
        {
            btnOpen.Enabled = false; 
            btnClose.Enabled = false;

            DialogResult result = 0;
            string xmlBody = CreateXML("BarrierOPEN", true);

            RestResponse response;
            do
            {
                var request = new RestRequest("api/somiod/" + slaveAppName + "/" + slaveContainerName, Method.Post)
                {
                    RequestFormat = DataFormat.Xml
                };

                request.AddParameter("application/xml", xmlBody, ParameterType.RequestBody);
                response = client.Execute(request);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    result = MessageBox.Show("Error '" + response.StatusCode + "', retry?\nERROR: \n" + response.Content, "Error connecting", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                }

            } while (result == DialogResult.Yes);

            if (response.StatusCode == HttpStatusCode.OK)
                pbBarrier.Image = Properties.Resources.barrier_open;

            await Task.Delay(1000);
            btnOpen.Enabled = true;
            btnClose.Enabled = true;
        }

        private async void btnClose_Click(object sender, EventArgs e)
        {
            btnOpen.Enabled = false;
            btnClose.Enabled = false;

            DialogResult result = 0;
            string xmlBody = CreateXML("BarrierClose", false);

            RestResponse response;
            do
            {
                var request = new RestRequest("api/somiod/" + slaveAppName + "/" + slaveContainerName, Method.Post)
                {
                    RequestFormat = DataFormat.Xml
                };

                request.AddParameter("application/xml", xmlBody, ParameterType.RequestBody);
                response = client.Execute(request);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    result = MessageBox.Show("Error '" + response.StatusCode + "', retry?", "Error connecting", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                }

            } while (result == DialogResult.Yes);

            if (response.StatusCode == HttpStatusCode.OK)
                pbBarrier.Image = Properties.Resources.barrier_closed;

            await Task.Delay(1000);
            btnOpen.Enabled = true;
            btnClose.Enabled = true;
        }
    }
}
