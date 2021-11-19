// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Common;
using Common.KeyVault;
using EnsureThat;
using Microsoft.Azure.ServiceBus;

namespace MessageUploader
{
    public static class Program
    {
        private static string s_serviceBusConnectionString;
        private static string s_topicName;
        private static ITopicClient s_topicClient;
        private static string[] s_file;

        public static async Task Main(string[] args)
        {
            EnsureArg.IsNotNull(args, nameof(args));

            var options = new SecretClientOptions()
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

            KeyVaultSecret secret = await client.GetSecretAsync(KnownSecretNames.ServiceBusConnectionString);

            s_serviceBusConnectionString = secret.Value;
            s_topicName = args[0];
            s_topicClient = new TopicClient(s_serviceBusConnectionString, s_topicName);
            string filePath = args[1];

            int start = int.Parse(args[2]);
            int end = int.Parse(args[3]);

            s_file = File.ReadLines(filePath).Skip(start).Take(end - start).ToArray();

            Console.WriteLine("======================================================");
            Console.WriteLine("Press ENTER key to exit after sending all the messages.");
            Console.WriteLine("======================================================");

            // Send messages.
            await SendAllMessagesAsync();

            await s_topicClient.CloseAsync();
        }

        private static async Task SendAllMessagesAsync()
        {
            try
            {
                int count = 0;
                foreach (string line in s_file)
                {
                    // Create a new message to send to the topic
                    var message = new Message(Encoding.UTF8.GetBytes(line));

                    // Write the body of the message to the console
                    Console.WriteLine($"Sending message: {Encoding.UTF8.GetString(message.Body)}" + $" count = {count}");

                    // Send the message to the topic
                    await s_topicClient.SendAsync(message);

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
