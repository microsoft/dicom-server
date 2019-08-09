// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom.Imaging;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Retrieve.BitmapRendering
{
    public sealed class NetCoreImageManager : ImageManager
    {
        public override bool IsDefault { get; }

        protected override IImage CreateImageImpl(int width, int height)
        {
            throw new NotImplementedException();
        }
    }
}
