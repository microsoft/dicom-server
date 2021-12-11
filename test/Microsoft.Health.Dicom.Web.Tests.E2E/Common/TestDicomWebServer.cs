// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using EnsureThat;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common
{
    /// <summary>
    /// Represents a Dicom server for end-to-end testing.
    /// </summary>
    public abstract class TestDicomWebServer : IDisposable
    {
        protected TestDicomWebServer(Uri baseAddress)
            : this(baseAddress, NullHost.Instance)
        { }

        protected TestDicomWebServer(Uri baseAddress, IHost webJobsHost)
        {
            BaseAddress = EnsureArg.IsNotNull(baseAddress, nameof(baseAddress));
            WebJobsHost = EnsureArg.IsNotNull(webJobsHost, nameof(webJobsHost));
        }

        public Uri BaseAddress { get; }

        public IHost WebJobsHost { get; }

        public abstract HttpMessageHandler CreateMessageHandler();

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                WebJobsHost.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
