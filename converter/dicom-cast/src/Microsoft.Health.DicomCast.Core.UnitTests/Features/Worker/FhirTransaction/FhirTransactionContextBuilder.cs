// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using FellowOakDicom;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public class FhirTransactionContextBuilder
    {
        public static FhirTransactionContext DefaultFhirTransactionContext(DicomDataset metadata = null)
        {
            var context = new FhirTransactionContext(ChangeFeedGenerator.Generate(metadata: metadata ?? DicomCastDatasetGenerator.CreateDicomDataset()))
            {
                UtcDateTimeOffset = TimeSpan.Zero,
            };

            context.Request.Patient = FhirTransactionRequestEntryGenerator.GenerateDefaultCreateRequestEntry<Patient>();
            context.Request.ImagingStudy = FhirTransactionRequestEntryGenerator.GenerateDefaultCreateRequestEntry<ImagingStudy>();
            context.Request.Endpoint = FhirTransactionRequestEntryGenerator.GenerateDefaultCreateRequestEntry<Endpoint>();

            return context;
        }
    }
}
