// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Dicom.Imaging;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Retrieve.BitmapRendering
{
    public static class DicomImageExtensions
    {
        private static readonly Size ThumbnailSize = new Size(width: 100, height: 100);

        public static Bitmap ToBitmap(this DicomImage image, int frame = 0)
        {
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

        public static MemoryStream ToRenderedMemoryStream(this DicomImage dicomImage, ImageRepresentationModel imageRepresentation, int frame = 0, bool thumbnail = false)
        {
            var ms = new MemoryStream();

            try
            {
                using (var image = dicomImage.ToBitmap(frame))
                {
                    var bmp = image;
                    if (thumbnail)
                    {
                        var bmpResized = new Bitmap(ThumbnailSize.Width, ThumbnailSize.Height);
                        using (var graphics = Graphics.FromImage(bmpResized))
                        {
                            graphics.CompositingQuality = CompositingQuality.HighSpeed;
                            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            graphics.CompositingMode = CompositingMode.SourceCopy;
                            graphics.DrawImage(image, x: 0, y: 0, ThumbnailSize.Width, ThumbnailSize.Height);
                        }

                        bmp = bmpResized;
                    }

                    bmp.Save(ms, imageRepresentation.CodecInfo, imageRepresentation.EncoderParameters);
                }

                ms.Seek(0, SeekOrigin.Begin);
            }
            catch
            {
                // We catch all here because rendering may throw for a variety of reasons.
                // Most likely, this is a corrupt image
            }

            return ms;
        }
    }
}
