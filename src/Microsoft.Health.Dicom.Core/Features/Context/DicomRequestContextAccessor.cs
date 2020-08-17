// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using Microsoft.Health.Core.Features.Context;

namespace Microsoft.Health.Dicom.Core.Features.Context
{
    public class DicomRequestContextAccessor : IDicomRequestContextAccessor
    {
        private readonly AsyncLocal<IRequestContext> _dicomRequestContextCurrent = new AsyncLocal<IRequestContext>();

        public IRequestContext DicomRequestContext
        {
            get => _dicomRequestContextCurrent.Value;

            set => _dicomRequestContextCurrent.Value = value;
        }
    }
}
