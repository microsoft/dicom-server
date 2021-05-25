// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.ObjectModel;
using Hl7.Fhir.Model;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Container for holding the parsed Observations from a DicomDataset
    /// </summary>
    public class ParsedObservation
    {
        public Collection<Observation> DoseSummaries { get; } = new();
        public Collection<Observation> IrradiationEvents { get; } = new();
    }
}
