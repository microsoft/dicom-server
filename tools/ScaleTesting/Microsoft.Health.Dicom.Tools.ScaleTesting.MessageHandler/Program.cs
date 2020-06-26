// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Dicom;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Dicom.Tools.ScaleTesting.Common;

namespace Microsoft.Health.Dicom.Tools.ScaleTesting.MessageHandler
{
    public class Program
    {
        private static string _serviceBusConnectionString;
        private static string _topicName;
        private const string SubscriptionName = "s1";

        private static ISubscriptionClient subscriptionClient;

        private const string WebServerUrl = "http://dicom-server-ii.azurewebsites.net";
        private static DicomWebClient client;

        private static string[] _separators = new string[] { "\t", "  ", " " };

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
            var client = new SecretClient(new Uri("https://dicom-client.vault.azure.net/"), new DefaultAzureCredential(), options);

            KeyVaultSecret secret = client.GetSecret("ServiceBusConnectionString");

            _serviceBusConnectionString = secret.Value;

            secret = client.GetSecret("AppConfiguration");
            var builder = new ConfigurationBuilder();
            builder.AddAzureAppConfiguration(secret.Value);

            var config = builder.Build();
            var runType = config["RunType"];
            _topicName = runType;

            subscriptionClient = new SubscriptionClient(_serviceBusConnectionString, _topicName, SubscriptionName, ReceiveMode.PeekLock);

            SetupDicomWebClient();

            // Register subscription message handler and receive messages in a loop
            RegisterOnMessageHandlerAndReceiveMessages();

            Console.ReadKey();

            await subscriptionClient.CloseAsync();
        }

        private static void SetupDicomWebClient()
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(WebServerUrl);

            client = new DicomWebClient(httpClient, new Microsoft.IO.RecyclableMemoryStreamManager());
        }

        public static void RegisterOnMessageHandlerAndReceiveMessages()
        {
            // Configure the message handler options in terms of exception handling, number of concurrent messages to deliver, etc.
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                // Maximum number of concurrent calls to the callback ProcessMessagesAsync(), set to 1 for simplicity.
                // Set it according to how many messages the application wants to process in parallel.
                MaxConcurrentCalls = 10,

                // Indicates whether MessagePump should automatically complete the messages after returning from User Callback.
                // False below indicates the Complete will be handled by the User Callback as in `ProcessMessagesAsync` below.
                AutoComplete = false,
                MaxAutoRenewDuration = TimeSpan.FromMinutes(2),
            };

            // Register the function that processes messages.
            subscriptionClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
        }

        private static async Task ProcessMessagesAsync(Message message, CancellationToken token)
        {
            // Process the message.
            Console.WriteLine($"Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(message.Body)}");

            switch (_topicName)
            {
                case "stow-rs":
                    await Stow(message, token);
                    break;
                case "wado-rs":
                    await Wado(message, token);
                    break;
                case "wado-rs-metadata":
                    await WadoMetadata(message, token);
                    break;
                case "qido-test":
                    await Qido(message, token);
                    break;
                default:
                    System.Diagnostics.Trace.TraceError("Unsupported run type!");
                    break;
            }

            // Complete the message so that it is not received again.
            await subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
        }

        private static async Task Stow(Message message, CancellationToken token)
        {
            PatientInstance pI = JsonSerializer.Deserialize<PatientInstance>(Encoding.UTF8.GetString(message.Body));

            // 400, 400, 100 - 16MB
            // 100, 100, 100 - 1MB
            // 100, 100, 50 - 0.5MB
            DicomFile dicomFile = Samples.CreateRandomDicomFileWithPixelData(
                        pI,
                        rows: 100,
                        columns: 100,
                        frames: 50);

            DicomWebResponse<DicomDataset> response = await client.StoreAsync(new List<DicomFile>() { dicomFile }, cancellationToken: token);

            int statusCode = (int)response.StatusCode;
            if (statusCode != 409 && statusCode < 200 && statusCode > 299)
            {
                throw new Exception();
            }

            return;
        }

        private static async Task Wado(Message message, CancellationToken token)
        {
            (string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid) = ParseMessageForUids(message);

            if (sopInstanceUid != null)
            {
                await client.RetrieveInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, cancellationToken: token);
            }
            else if (seriesInstanceUid != null)
            {
                await client.RetrieveSeriesAsync(studyInstanceUid, seriesInstanceUid, cancellationToken: token);
            }
            else
            {
                await client.RetrieveStudyAsync(studyInstanceUid, cancellationToken: token);
            }

            return;
        }

        private static async Task WadoMetadata(Message message, CancellationToken token)
        {
            (string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid) = ParseMessageForUids(message);

            if (sopInstanceUid != null)
            {
                await client.RetrieveInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, cancellationToken: token);
            }
            else if (seriesInstanceUid != null)
            {
                await client.RetrieveSeriesMetadataAsync(studyInstanceUid, seriesInstanceUid, cancellationToken: token);
            }
            else
            {
                await client.RetrieveStudyMetadataAsync(studyInstanceUid, cancellationToken: token);
            }

            return;
        }

        private static async Task Qido(Message message, CancellationToken token)
        {
            string relativeUrl = Encoding.UTF8.GetString(message.Body);
            await client.QueryWithBadRequest(relativeUrl, cancellationToken: token);
            return;
        }

        private static (string, string, string) ParseMessageForUids(Message message)
        {
            string messageBody = Encoding.UTF8.GetString(message.Body);
            string[] split = messageBody.Split(_separators, StringSplitOptions.RemoveEmptyEntries);

            string studyUid = split[0];
            string seriesUid = split.Count() > 1 ? split[1] : null;
            string instanceUid = split.Count() > 2 ? split[2] : null;

            return (studyUid, seriesUid, instanceUid);
        }

        public static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            System.Diagnostics.Trace.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
            System.Diagnostics.Trace.WriteLine("Exception context for troubleshooting:");
            System.Diagnostics.Trace.WriteLine($"- Endpoint: {context.Endpoint}");
            System.Diagnostics.Trace.WriteLine($"- Entity Path: {context.EntityPath}");
            System.Diagnostics.Trace.WriteLine($"- Executing Action: {context.Action}");
            return Task.CompletedTask;
        }
    }
}
