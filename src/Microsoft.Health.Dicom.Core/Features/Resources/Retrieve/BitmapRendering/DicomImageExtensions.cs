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
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Retrieve.BitmapRendering
{
    public static class DicomImageExtensions
    {
        // DICOM spec does not define the thumbnail size. This choice is arbitrary and might be made a
        // configuration constant in the future
        private static readonly Size ThumbnailSize = new Size(width: 200, height: 200);

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

        public static MemoryStream ToRenderedMemoryStream(this DicomImage dicomImage, ImageRepresentationModel imageRepresentation, int frame = 0, bool thumbnail = false)
        {
            EnsureArg.IsNotNull(dicomImage, nameof(dicomImage));

            Bitmap image = null;
            Bitmap bmpResized = null;

            var ms = new MemoryStream();
            try
            {
                image = dicomImage.ToBitmap(frame);
                var bmp = image;

                if (thumbnail)
                {
                    // Scale factor to preserve aspect ratio and fit within the thumbnail square
                    float scale = Math.Min(
                        (float)ThumbnailSize.Width / bmp.Width,
                        (float)ThumbnailSize.Height / bmp.Height);
                    var w = (int)(bmp.Width * scale);
                    var h = (int)(bmp.Height * scale);

                    bmpResized = new Bitmap(ThumbnailSize.Width, ThumbnailSize.Height);

                    using (var graphics = Graphics.FromImage(bmpResized))
                    {
                        graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        graphics.CompositingQuality = CompositingQuality.HighSpeed;
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.CompositingMode = CompositingMode.SourceCopy;

                        // Paint the background black
                        graphics.FillRectangle(
                            new SolidBrush(Color.Black),
                            new RectangleF(x: 0, y: 0, ThumbnailSize.Width, ThumbnailSize.Height));

                        // Draw image in the middle
                        graphics.DrawImage(
                            bmp,
                            x: (ThumbnailSize.Width - w) / 2,
                            y: (ThumbnailSize.Height - h) / 2,
                            width: w,
                            height: h);

                        bmp = bmpResized;
                    }
                }

                bmp.Save(ms, imageRepresentation.CodecInfo, imageRepresentation.EncoderParameters);

                ms.Seek(0, SeekOrigin.Begin);
            }
            catch
            {
                // We catch all here because rendering may throw for a variety of reasons.
                // Most likely, this is a corrupt image or there has been a codec error.
                // Presently the intention is to return an empty stream
                // TODO: for future enhancements - put more detailed error analysis here.
                // If this is an issue to users, maybe return a default placeholder image
            }
            finally
            {
                image?.Dispose();
                bmpResized?.Dispose();
            }

            return ms;
        }
    }
}
