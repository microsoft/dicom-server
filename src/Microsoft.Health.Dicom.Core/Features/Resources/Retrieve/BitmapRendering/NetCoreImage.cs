// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Drawing;
using Dicom.Imaging;
using Dicom.Imaging.Render;
using Dicom.IO;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Retrieve.BitmapRendering
{
    public sealed class NetCoreImage : ImageBase<Bitmap>
    {
        public NetCoreImage(int width, int height, PinnedIntArray pixels, Bitmap image)
            : base(width, height, pixels, image)
        {
        }

        public override void Render(int components, bool flipX, bool flipY, int rotation)
        {
            throw new NotImplementedException();
        }

        public override void DrawGraphics(IEnumerable<IGraphic> graphics)
        {
            throw new NotImplementedException();
        }

        public override IImage Clone()
        {
            throw new NotImplementedException();
        }
    }
}
