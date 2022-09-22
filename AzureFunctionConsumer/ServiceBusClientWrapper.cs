using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace AzureFunctionConsumer
{
    /// <summary>
    /// Wrapper class for ServiceBusClient and ServiceBusSender that enables the
    /// sending of messages to Service Bus queues and topics.
    /// </summary>
    internal sealed class ServiceBusClientWrapper : IAsyncDisposable
    {

        ServiceBusClient _client;

        string _queueName;
        ServiceBusSender _queueSender;

        string _topicName;
        ServiceBusSender _topicSender;

        /// <summary>
        /// Creates a new ServiceBusClientWrapper from a connection string.
        /// </summary>
        /// <param name="connectionString">Azure ServiceBus connection string.</param>
        public ServiceBusClientWrapper(string connectionString)
        {
            _client = new ServiceBusClient(connectionString);
        }

        /// <summary>
        /// Sends a number of pre-defined placeholder messages to a specified ServiceBus Queue.
        /// </summary>
        /// <param name="queueName">The Queue to send the messages to.</param>
        /// <param name="messageCount">The number of messages to send. 1 by default.</param>
        /// <returns></returns>
        public async Task SendMessagesToQueue(string queueName, uint messageCount = 1)
            => await SendMessagesToQueue(queueName, ImmutableDictionary<string, object>.Empty, messageCount);

        /// <summary>
        /// Sends a number of pre-defined placeholder messages to a specified ServiceBus Queue.
        /// </summary>
        /// <param name="queueName">The Queue to send the messages to.</param>
        /// <param name="customProperties">
        /// Any custom properties to be added to the message. These do not affect
        /// the message contents itself. See https://learn.microsoft.com/en-us/rest/api/servicebus/message-headers-and-properties#message-properties
        /// for more information.</param>
        /// <param name="messageCount">The number of messages to send. 1 by default.</param>
        /// <returns></returns>
        public async Task SendMessagesToQueue(string queueName, IDictionary<string, object> customProperties, uint messageCount = 1)
        {
            /// This conditional exists to handle the creation of a new ServiceBusSender.
            /// If there is a ServiceBusSender already created for the queue we're currently attempting to send messages to,
            /// we don't want to re-create the sender, as these are typically recommended to endure for the lifetime of the application.
            /// This program's requirements necessitate the ability to send messages to multiple different queues/topics within the lifetime of the application,
            /// so we cannot have a single ServiceBusSender instance, but this logic is an attempt at following best practices.
            /// See https://learn.microsoft.com/en-us/rest/api/servicebus/message-headers-and-properties#message-properties
            /// for more information on this.
            if (_queueSender is null)
            {
                _queueSender = _client.CreateSender(queueName);
                _queueName = queueName;
            }
            else
            {
                if (_queueName != queueName)
                {
                    await _queueSender.CloseAsync();
                    _queueSender = _client.CreateSender(queueName);
                    _queueName = queueName;
                }
            }

            /// Using a collection of tasks here for when we need to send multiple messages at once. This allows us to
            /// launch the tasks in parallel and continue execution once they all have completed.
            List<Task> messageTasks = new List<Task>();

            for (uint i = 0; i < messageCount; i++)
            {
                string messageBody = $"Message-{i}-{Guid.NewGuid().ToString("N")}-{DateTime.Now.Minute}";
                ServiceBusMessage message = new ServiceBusMessage(messageBody);

                foreach (var property in customProperties)
                    message.ApplicationProperties.Add(property);

                messageTasks.Add(_queueSender.SendMessageAsync(message).ContinueWith(_ =>
                {
                    Console.WriteLine($"Successfully sent message: {messageBody}");
                }));
            }

            await Task.WhenAll(messageTasks);
        }

        /// <summary>
        /// Sends a number of pre-defined placeholder messages to a specified ServiceBus Topic.
        /// </summary>
        /// <param name="topicName">The Topic to send the messages to.</param>
        /// <param name="messageCount">The number of messages to send. 1 by default.</param>
        /// <returns></returns>
        public async Task SendMessagesToTopic(string topicName, uint messageCount = 1) =>
            await SendMessagesToTopic(topicName, ImmutableDictionary<string, object>.Empty, messageCount);

        /// <summary>
        /// Sends a number of pre-defined placeholder messages to a specified ServiceBus Topic.
        /// </summary>
        /// <param name="topicName">The Topic to send the messages to.</param>
        /// <param name="customProperties">
        /// Any custom properties to be added to the message. These do not affect
        /// the message contents itself. For topics, these properties allow for an easy way to filter messages
        /// for subscriptions. See https://learn.microsoft.com/en-us/rest/api/servicebus/message-headers-and-properties#message-properties
        /// for more information.</param>
        /// <param name="messageCount">The number of messages to send. 1 by default.</param>
        /// <returns></returns>
        public async Task SendMessagesToTopic(string topicName, IDictionary<string, object> customProperties, uint messageCount = 1)
        {
            /// This conditional exists to handle the creation of a new ServiceBusSender.
            /// If there is a ServiceBusSender already created for the queue we're currently attempting to send messages to,
            /// we don't want to re-create the sender, as these are typically recommended to endure for the lifetime of the application.
            /// This program's requirements necessitate the ability to send messages to multiple different queues/topics within the lifetime of the application,
            /// so we cannot have a single ServiceBusSender instance, but this logic is an attempt at following best practices.
            /// See https://learn.microsoft.com/en-us/rest/api/servicebus/message-headers-and-properties#message-properties
            /// for more information on this.
            if (_topicSender is null)
            {
                _topicSender = _client.CreateSender(topicName);
                _topicName = topicName;
            }
            else
            {
                if (_topicName != topicName)
                {
                    await _topicSender.CloseAsync();
                    _topicSender = _client.CreateSender(topicName);
                    _topicName = topicName;
                }
            }

            /// Using a collection of tasks here for when we need to send multiple messages at once. This allows us to
            /// launch the tasks in parallel and continue execution once they all have completed.
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
