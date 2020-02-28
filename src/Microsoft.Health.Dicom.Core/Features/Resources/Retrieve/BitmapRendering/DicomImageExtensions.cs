// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Dicom.Imaging;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Retrieve.BitmapRendering
{
    public static class DicomImageExtensions
    {
        public static Bitmap ToBitmap(this DicomImage image, int frame = 0)
        {
            EnsureArg.IsNotNull(image, nameof(image));

            var bytes = image.RenderImage(frame).AsBytes();
            var w = image.Width;
            var h = image.Height;
            var ch = 4;

            var bmp = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);

            BitmapData bmData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, bmp.PixelFormat);
            IntPtr pNative = bmData.Scan0;
            Marshal.Copy(bytes, 0, pNative, w * h * ch);
            bmp.UnlockBits(bmData);

            return bmp;
        }
    }
}
