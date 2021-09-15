// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    internal class QueryParams
    {
        public bool FuzzyMatch { get; set; }

        public int Offset { get; set; }

        public int Limit { get; set; }

        public bool AllValue { get; set; }
    }
}
