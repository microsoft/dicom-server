// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.IO;

namespace Microsoft.Health.Dicom.Core
{
    public static class RecyclableMemoryStreamManagerAccessor
    {
        private static readonly Lazy<RecyclableMemoryStreamManager> SLazy = new Lazy<RecyclableMemoryStreamManager>();

        public static RecyclableMemoryStreamManager Instance => SLazy.Value;
    }
}
