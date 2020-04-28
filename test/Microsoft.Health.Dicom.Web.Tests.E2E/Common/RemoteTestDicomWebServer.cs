// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common
{
    /// <summary>
    /// Represents a <see cref="TestDicomWebServer"/> that resides out of process that we will
    /// communicate with over TCP/IP.
    /// </summary>
    public class RemoteTestDicomWebServer : TestDicomWebServer
    {
        public RemoteTestDicomWebServer(string environmentUrl)
            : base(new Uri(environmentUrl))
        {
        }

        public override HttpMessageHandler CreateMessageHandler()
        {
            return new HttpClientHandler();
        }
    }
}
