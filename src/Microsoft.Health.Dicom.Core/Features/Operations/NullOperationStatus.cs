// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Models.Operations;

namespace Microsoft.Health.Dicom.Core.Features.Operations
{
    internal sealed class NullOperationStatus : ICustomOperationStatus
    {
        public static ICustomOperationStatus Value { get; } = new NullOperationStatus();

        private NullOperationStatus()
        { }

        public DateTime? CreatedTime
        {
            get => null;
            set { }
        }

        public OperationProgress GetProgress()
            => new OperationProgress();
    }
}
