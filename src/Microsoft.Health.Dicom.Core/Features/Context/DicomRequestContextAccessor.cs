// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;

namespace Microsoft.Health.Dicom.Core.Features.Context
{
    public class DicomRequestContextAccessor : IDicomRequestContextAccessor
    {
        private readonly AsyncLocal<IDicomRequestContext> _dicomRequestContextCurrent = new AsyncLocal<IDicomRequestContext>();

        public IDicomRequestContext DicomRequestContext
        {
            get => _dicomRequestContextCurrent.Value;

            set => _dicomRequestContextCurrent.Value = value;
        }
    }
}
