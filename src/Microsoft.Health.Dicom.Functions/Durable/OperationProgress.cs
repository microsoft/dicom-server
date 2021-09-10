// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Functions.Durable
{
    internal class OperationProgress
    {
        public int PercentComplete { get; set; }

        public IReadOnlyCollection<string> ResourceIds { get; set; }
    }
}
