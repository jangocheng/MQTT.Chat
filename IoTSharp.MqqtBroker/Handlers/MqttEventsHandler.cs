﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Server;

namespace IoT.MqqtBroker
{
    internal class MqttEventsHandler
    {
    
        public static ILogger<MqttEventsHandler> Logger { get; internal set; }
        public static IMqttServer Server { get; internal set; }

        static long clients = 0;
        internal static void Server_ClientConnected(object sender, MqttClientConnectedEventArgs e)
        {
            Logger.LogInformation($"客户端[{e.ClientId}]已连接");
            clients++;
            Task.Run(() => ((IMqttServer)sender).PublishAsync("$SYS/broker/clients/total", clients.ToString()));
        }
     static    DateTime uptime =DateTime.MinValue;
        internal static void Server_Started(object sender, EventArgs e)
        {
            Logger.LogInformation($"服务器已启动");
            uptime = DateTime.Now;
        }

        internal static void Server_Stopped(object sender, EventArgs e)
        {
            Logger.LogInformation($"服务器已终止");
        }
        static Dictionary<string, int> lstTopics = new Dictionary<string, int>();
        static long received = 0;
        internal static void Server_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            Logger.LogInformation($"服务器收到客户端{e.ClientId}的消息: Topic=[{e.ApplicationMessage.Topic }],Retain=[{e.ApplicationMessage.Retain}],QualityOfServiceLevel=[{e.ApplicationMessage.QualityOfServiceLevel}]");
            if (!lstTopics.ContainsKey(e.ApplicationMessage.Topic))
            {
                lstTopics.Add(e.ApplicationMessage.Topic, 1);
                Task.Run(() => ((IMqttServer)sender).PublishAsync("$SYS/broker/subscriptions/count", lstTopics.Count.ToString()));
            }
            else
            {
                lstTopics[e.ApplicationMessage.Topic]++;
            }
            received += e.ApplicationMessage.Payload.Length;

        }
        static long Subscribed;
        internal static void Server_ClientSubscribedTopic(object sender, MqttClientSubscribedTopicEventArgs e)
        {
            Logger.LogInformation($"客户端[{e.ClientId}]订阅[{e.TopicFilter}]");
            if (e.TopicFilter.Topic.StartsWith("$SYS/"))
            {
                if (e.TopicFilter.Topic.StartsWith("$SYS/broker/version"))
                {
                    var mename = typeof(MqttEventsHandler).Assembly.GetName();
                    var mqttnet = typeof(MqttClientSubscribedTopicEventArgs).Assembly.GetName();
                    Task.Run(() => ((IMqttServer)sender).PublishAsync("$SYS/broker/version", $"{mename.Name}V{mename.Version.ToString()},{mqttnet.Name}.{mqttnet.Version.ToString()}"));
                }
                else if (e.TopicFilter.Topic.StartsWith("$SYS/broker/uptime"))
                {
                    Task.Run(() => ((IMqttServer)sender).PublishAsync("$SYS/broker/uptime", uptime.ToString()));
                }
            }
            else
            {
                Subscribed++;
                Task.Run(() => ((IMqttServer)sender).PublishAsync("$SYS/broker/subscriptions/count", Subscribed.ToString()));
            }


        }

        internal static void Server_ClientUnsubscribedTopic(object sender, MqttClientUnsubscribedTopicEventArgs e)
        {
            Logger.LogInformation($"客户端[{e.ClientId}]取消订阅[{e.TopicFilter}]");
            if (!e.TopicFilter.StartsWith("$SYS/"))
            {
                Subscribed--;
                Task.Run(() => ((IMqttServer)sender).PublishAsync("$SYS/broker/subscriptions/count", Subscribed.ToString()));
            }
        }

        internal static void MqttConnectionValidatorContext(MqttConnectionValidatorContext obj)
        {
            Logger.LogInformation($"ClientId={obj.ClientId},Endpoint={obj.Endpoint},Username={obj.Username}，Password={obj.Password},WillMessage={obj.WillMessage?.ConvertPayloadToString()}");
            obj.ReturnCode = MQTTnet.Protocol.MqttConnectReturnCode.ConnectionAccepted;
        }
    }
}