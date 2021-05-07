// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Common;
using Common.AppConfiguration;
using Common.KeyVault;
using Common.ServiceBus;
using Dicom;
using EnsureThat;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Dicom.Client;

namespace MessageHandler
{
    public static class Program
    {
        private static string s_serviceBusConnectionString;
        private static string s_topicName;

        private static ISubscriptionClient s_subscriptionClient;
        private static IDicomWebClient s_client;

        public static async Task Main()
        {
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

            KeyVaultSecret secret = client.GetSecret(KnownSecretNames.ServiceBusConnectionString);

            s_serviceBusConnectionString = secret.Value;

            secret = client.GetSecret(KnownSecretNames.AppConfigurationConnectionString);
            var builder = new ConfigurationBuilder();
            builder.AddAzureAppConfiguration(secret.Value);

            IConfigurationRoot config = builder.Build();
            var runType = config[KnownConfigurationNames.RunType];
            s_topicName = runType;

            s_subscriptionClient = new SubscriptionClient(s_serviceBusConnectionString, s_topicName, KnownSubscriptions.S1, ReceiveMode.PeekLock);
            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri(KnownApplicationUrls.DicomServerUrl),
            };

            SetupDicomWebClient(httpClient);

            // Register subscription message handler and receive messages in a loop
            RegisterOnMessageHandlerAndReceiveMessages();

            Thread.Sleep(TimeSpan.FromSeconds(10000));

            await s_subscriptionClient.CloseAsync();
        }

        private static void SetupDicomWebClient(HttpClient httpClient)
        {
            s_client = new DicomWebClient(httpClient);
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
            s_subscriptionClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
        }

        private static async Task ProcessMessagesAsync(Message message, CancellationToken token)
        {
            // Process the message.
            Console.WriteLine($"Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(message.Body)}");

            switch (s_topicName)
            {
                case KnownTopics.StowRs:
                case KnownTopics.StowRsTest:
                    await Stow(message, token);
                    break;
                case KnownTopics.WadoRs:
                case KnownTopics.WadoRsTest:
                    await Wado(message, token);
                    break;
                case KnownTopics.WadoRsMetadata:
                case KnownTopics.WadoRsMetadataTest:
                    await WadoMetadata(message, token);
                    break;
                case KnownTopics.Qido:
                case KnownTopics.QidoTest:
                    await Qido(message, token);
                    break;
                default:
                    System.Diagnostics.Trace.TraceError("Unsupported run type!");
                    break;
            }

            // Complete the message so that it is not received again.
            await s_subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
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

            DicomWebResponse<DicomDataset> response = await s_client.StoreAsync(new List<DicomFile>() { dicomFile }, cancellationToken: token);

            int statusCode = (int)response.StatusCode;
            if (statusCode != 409 && statusCode < 200 && statusCode > 299)
            {
                throw new HttpRequestException("Stow operation failed", null, response.StatusCode);
            }

            return;
        }

        private static async Task Wado(Message message, CancellationToken token)
        {
            (string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid) = ParseMessageForUids(message);

            if (sopInstanceUid != null)
            {
                await s_client.RetrieveInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, cancellationToken: token);
            }
            else if (seriesInstanceUid != null)
            {
                await s_client.RetrieveSeriesAsync(studyInstanceUid, seriesInstanceUid, cancellationToken: token);
            }
            else
            {
                await s_client.RetrieveStudyAsync(studyInstanceUid, cancellationToken: token);
            }

            return;
        }

        private static async Task WadoMetadata(Message message, CancellationToken token)
        {
            (string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid) = ParseMessageForUids(message);

            if (sopInstanceUid != null)
            {
                await s_client.RetrieveInstanceMetadataAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, cancellationToken: token);
            }
            else if (seriesInstanceUid != null)
            {
                await s_client.RetrieveSeriesMetadataAsync(studyInstanceUid, seriesInstanceUid, cancellationToken: token);
            }
            else
            {
                await s_client.RetrieveStudyMetadataAsync(studyInstanceUid, cancellationToken: token);
            }

            return;
        }

        private static async Task Qido(Message message, CancellationToken token)
        {
            var relativeUrl = new Uri(Encoding.UTF8.GetString(message.Body));
            await s_client.QueryAsync(relativeUrl, cancellationToken: token);
        }

        private static (string, string, string) ParseMessageForUids(Message message)
        {
            string messageBody = Encoding.UTF8.GetString(message.Body);
            string[] split = messageBody.Split(KnownSeparators.MessageSeparators, StringSplitOptions.RemoveEmptyEntries);

            string studyUid = split[0];
            string seriesUid = split.Length > 1 ? split[1] : null;
            string instanceUid = split.Length > 2 ? split[2] : null;

            return (studyUid, seriesUid, instanceUid);
        }

        public static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            EnsureArg.IsNotNull(exceptionReceivedEventArgs, nameof(exceptionReceivedEventArgs));

            System.Diagnostics.Trace.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
            ExceptionReceivedContext context = exceptionReceivedEventArgs.ExceptionReceivedContext;
            System.Diagnostics.Trace.WriteLine("Exception context for troubleshooting:");
            System.Diagnostics.Trace.WriteLine($"- Endpoint: {context.Endpoint}");
            System.Diagnostics.Trace.WriteLine($"- Entity Path: {context.EntityPath}");
            System.Diagnostics.Trace.WriteLine($"- Executing Action: {context.Action}");
            return Task.CompletedTask;
        }
    }
}
