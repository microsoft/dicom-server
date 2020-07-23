// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.ServiceBus;
using Microsoft.Health.Dicom.Tools.ScaleTesting.Common;
using Microsoft.Health.Dicom.Tools.ScaleTesting.Common.KeyVault;

namespace Microsoft.Health.Dicom.Tools.ScaleTesting.MessageUploader
{
    public class Program
    {
        private static string _serviceBusConnectionString;
        private static string _topicName;
        private static ITopicClient topicClient;
        private static string[] file;

        public static async Task Main(string[] args)
        {
            SecretClientOptions options = new SecretClientOptions()
            {
                Retry =
                {
                    Delay = TimeSpan.FromSeconds(2),
                    MaxDelay = TimeSpan.FromSeconds(16),
                    MaxRetries = 5,
                    Mode = RetryMode.Exponential,
                },
            };
            var client = new SecretClient(new Uri(KnownApplicationUrls.KeyVaultUrl), new DefaultAzureCredential(), options);

            KeyVaultSecret secret = client.GetSecret(KnownSecretNames.ServiceBusConnectionString);

            _serviceBusConnectionString = secret.Value;
            _topicName = args[0];
            topicClient = new TopicClient(_serviceBusConnectionString, _topicName);
            string filePath = args[1];
            file = File.ReadAllLines(filePath);

            int start = int.Parse(args[2]);
            int end = int.Parse(args[3]);

            Console.WriteLine("======================================================");
            Console.WriteLine("Press ENTER key to exit after sending all the messages.");
            Console.WriteLine("======================================================");

            // Send messages.
            await SendMessagesAsync(start, end);

            // await SendMessagesAsync();
            // await SendAllMessagesAsync();

            await topicClient.CloseAsync();
        }

        private static async Task SendMessagesAsync(int start = 0, int end = 1000)
        {
            try
            {
                for (int i = start; i < end; i++)
                {
                    string line = file[i];

                    // Create a new message to send to the topic
                    var message = new Message(Encoding.UTF8.GetBytes(line));

                    // Write the body of the message to the console
                    Console.WriteLine($"Sending message: {Encoding.UTF8.GetString(message.Body)}" + $" i = {i}");

                    // Send the message to the topic
                    await topicClient.SendAsync(message);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
            }
        }

        private static async Task SendAllMessagesAsync()
        {
            try
            {
                int count = 0;
                foreach (string line in file)
                {
                    // Create a new message to send to the topic
                    var message = new Message(Encoding.UTF8.GetBytes(line));

                    // Write the body of the message to the console
                    Console.WriteLine($"Sending message: {Encoding.UTF8.GetString(message.Body)}" + $" count = {count}");

                    // Send the message to the topic
                    await topicClient.SendAsync(message);

                    count++;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
            }
        }
    }
}
