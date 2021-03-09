// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using Microsoft.Health.Core.Features.Context;

namespace Microsoft.Health.Dicom.Core.Features.Context
{
    public class DicomRequestContextAccessor : GenericRequestContextAccessor<IDicomRequestContext>, IDicomRequestContextAccessor
    {
        private readonly AsyncLocal<IDicomRequestContext> _dicomRequestContextCurrent = new AsyncLocal<IDicomRequestContext>();

        public override IDicomRequestContext RequestContext
        {
            get => _dicomRequestContextCurrent.Value;

            set => _dicomRequestContextCurrent.Value = value;
        }
    }
}
