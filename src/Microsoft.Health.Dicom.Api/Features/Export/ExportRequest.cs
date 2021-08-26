// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------


using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Api.Features.Export
{
    public class ExportRequest
    {
        public string DestinationBlobConnectionString { get; set; }

        public string DestinationBlobContainerName { get; set; }

        public string CohortId { get; set; }

        public IReadOnlyCollection<string> Instances { get; set; }

    }
}
