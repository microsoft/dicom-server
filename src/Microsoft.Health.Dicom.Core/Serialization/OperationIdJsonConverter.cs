// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Models.Operations;

namespace Microsoft.Health.Dicom.Core.Serialization
{
    internal class OperationIdJsonConverter : JsonGuidConverter
    {
        public OperationIdJsonConverter()
            : base(OperationId.FormatSpecifier, exactMatch: false)
        {
        }
    }
}
