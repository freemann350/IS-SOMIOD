using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using uPLibrary.Networking.M2Mqtt;

namespace SOMIOD.App_Start
{
    public class MqttPublisher
    {
        private readonly MqttClient mqttClient;

        public MqttPublisher(string brokerAddress)
        {
            mqttClient = new MqttClient(brokerAddress); 
        }

        public void ConnectAndPublish(string topic, string message)
        {
            try
            {
                mqttClient.Connect(Guid.NewGuid().ToString());

                if (mqttClient.IsConnected)
                {
                    mqttClient.Publish(topic, System.Text.Encoding.UTF8.GetBytes(message));
                }
                else
                {
                    Console.WriteLine("MQTT Client not Connected.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Connecting/publishing MQTT message: {ex.Message}");
            }
        }
    }
}