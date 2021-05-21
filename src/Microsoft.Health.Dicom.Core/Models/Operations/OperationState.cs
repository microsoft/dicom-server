// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Operations
{
    public class OperationState
    {
        public string Name { get; set; }

        public string Id { get; set; }

        public DateTime CreatedTime { get; set; }

        public OperationStatus Status { get; set; }

        public string ErrorMessage { get; set; }
    }
}
