// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Web.Tests.E2E.Clients;
using Microsoft.Health.Fhir.Tests.Common.FixtureParameters;
using DicomStartup = Microsoft.Health.Dicom.Web.Startup;
using DicomTestFixture = Microsoft.Health.Dicom.Web.Tests.E2E.Rest;

namespace Microsoft.Health.Dicom.DynamicFhir.Tests.E2E.Rest
{
    public class DicomFhirIntegrationTestFixture<TStartup> : IDisposable
    {
        private DicomTestFixture.HttpIntegrationTestFixture<DicomStartup> _dicomFixture;
        private HttpIntegrationTestFixture<TStartup> _fhirFixture;
        private DicomWebClient _dicomWebClient;
        private const string _testResourceId = "42";

        public DicomFhirIntegrationTestFixture(DataStore dataStore, Format format)
        {
            _dicomFixture = new DicomTestFixture.HttpIntegrationTestFixture<DicomStartup>();
            _fhirFixture = new HttpIntegrationTestFixture<TStartup>(dataStore, format);
            _dicomWebClient = new DicomWebClient(_dicomFixture.HttpClient);
        }

        public HttpIntegrationTestFixture<TStartup> FhirFixture
        {
            get { return _fhirFixture; }
        }

        public DicomTestFixture.HttpIntegrationTestFixture<DicomStartup> DicomFixture
        {
            get { return _dicomFixture; }
        }

        public DicomWebClient DicomClient
        {
            get { return _dicomWebClient; }
        }

        public Task<string> PostNewSampleStudyAsync()
        {
            return Task.FromResult(_testResourceId);
        }

        public void Dispose()
        {
            _dicomFixture?.Dispose();
            _fhirFixture?.Dispose();
            _dicomFixture = null;
            _fhirFixture = null;
        }
    }
}
