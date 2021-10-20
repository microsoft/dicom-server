// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public class FhirResourceBuilder
    {
        public static ImagingStudy CreateNewImagingStudy(string studyInstanceUid, List<string> seriesInstanceUidList, List<string> sopInstanceUidList, string patientResourceId, string source = "defaultSouce")
        {
            // Create a new ImagingStudy
            ImagingStudy study = new ImagingStudy
            {
                Id = "123",
                Status = ImagingStudy.ImagingStudyStatus.Available,
                Subject = new ResourceReference(patientResourceId),
                Meta = new Meta()
                {
                    VersionId = "1",
                    Source = source,
                },
            };

            foreach (string seriesInstanceUid in seriesInstanceUidList)
            {
                ImagingStudy.SeriesComponent series = new ImagingStudy.SeriesComponent()
                {
                    Uid = seriesInstanceUid,
                };

                foreach (string sopInstanceUid in sopInstanceUidList)
                {
                    ImagingStudy.InstanceComponent instance = new ImagingStudy.InstanceComponent()
                    {
                        Uid = sopInstanceUid,
                    };

                    series.Instance.Add(instance);
                }

                study.Series.Add(series);
            }

            study.Identifier.Add(IdentifierUtility.CreateIdentifier(studyInstanceUid));

            return study;
        }

        public static Endpoint CreateEndpointResource(string id = null, string name = null, string connectionSystem = null, string connectionCode = null, string address = null)
        {
            return new Endpoint()
            {
                Id = id ?? "1234",
                Name = name ?? FhirTransactionConstants.EndpointName,
                Status = Endpoint.EndpointStatus.Active,
                ConnectionType = new Coding()
                {
                    System = connectionSystem ?? FhirTransactionConstants.EndpointConnectionTypeSystem,
                    Code = connectionCode ?? FhirTransactionConstants.EndpointConnectionTypeCode,
                },
                Address = address ?? "https://dicom/",
            };
        }
    }
}
