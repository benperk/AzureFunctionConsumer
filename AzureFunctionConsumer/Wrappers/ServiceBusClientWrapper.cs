using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureFunctionConsumer.Wrappers
{
    internal sealed class ServiceBusClientWrapper : IAsyncDisposable
    {

        ServiceBusClient _client;

        string _queueName;
        ServiceBusSender _queueSender;

        string _topicName;
        ServiceBusSender _topicSender;

        public ServiceBusClientWrapper(string connectionString)
        {
            _client = new ServiceBusClient(connectionString);
        }

        public async Task SendMessagesToQueue(string queueName, uint messageCount = 1)
            => await SendMessagesToQueue(queueName, ImmutableDictionary<string, object>.Empty, messageCount);

        public async Task SendMessagesToQueue(string queueName, IDictionary<string, object> customProperties, uint messageCount = 1)
        {
            if(_queueSender is null)
            {
                _queueSender = _client.CreateSender(queueName);
                _queueName = queueName;
            } else
            {
                if(_queueName != queueName)
                {
                    await _queueSender.CloseAsync();
                    _queueSender = _client.CreateSender(queueName);
                    _queueName = queueName;
                }
            }

            List<Task> messageTasks = new List<Task>();
            
            for(uint i = 0; i < messageCount; i++)
            {
                string messageBody = $"Message-{i}-{Guid.NewGuid().ToString("N")}-{DateTime.Now.Minute}";
                ServiceBusMessage message = new ServiceBusMessage(messageBody);

                foreach(var property in customProperties)
                    message.ApplicationProperties.Add(property);

                messageTasks.Add(_queueSender.SendMessageAsync(message).ContinueWith(_ =>
                {
                    Console.WriteLine($"Successfully sent message: {messageBody}");
                }));
            }

            await Task.WhenAll(messageTasks);
        }

        public async Task SendMessagesToTopic(string topicName, uint messageCount = 1) => 
            await SendMessagesToTopic(topicName, ImmutableDictionary<string, object>.Empty, messageCount);

        public async Task SendMessagesToTopic(string topicName, IDictionary<string, object> customProperties, uint messageCount = 1)
        {
            if (_queueSender is null)
            {
                _topicSender = _client.CreateSender(topicName);
                _topicName = topicName;
            }
            else
            {
                if (_topicName != topicName)
                {
                    await _queueSender.CloseAsync();
                    _topicSender = _client.CreateSender(topicName);
                    _topicName = topicName;
                }
            }

            List<Task> messageTasks = new List<Task>();

            for (uint i = 0; i < messageCount; i++)
            {
                string messageBody = $"Message-{i}-{Guid.NewGuid().ToString("N")}-{DateTime.Now.Minute}";
                ServiceBusMessage message = new ServiceBusMessage(messageBody);

                foreach (var property in customProperties)
                    message.ApplicationProperties.Add(property);

                messageTasks.Add(_topicSender.SendMessageAsync(message).ContinueWith(_ =>
                {
                    Console.WriteLine($"Successfully sent message: {messageBody}");
                }));
            }

            await Task.WhenAll(messageTasks);
        }

        public async ValueTask DisposeAsync()
        {
            await _client.DisposeAsync();
        }
    }
}
