// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Drawing;
using Dicom.Imaging;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Retrieve.BitmapRendering
{
    /// <summary>
    /// Convenience class for non-generic access to <see cref="NetCoreImage"/> image objects. Copied from fo-dicom 4.0.2
    /// </summary>
    public static class NetCoreImageExtensions
    {
        /// <summary>
        /// Convenience method to access WinForms <see cref="IImage"/> instance as WinForms <see cref="Bitmap"/>.
        /// The returned <see cref="Bitmap"/> will be disposed when the <see cref="IImage"/> is disposed.
        /// </summary>
        /// <param name="image"><see cref="IImage"/> object.</param>
        /// <returns><see cref="Bitmap"/> contents of <paramref name="image"/>.</returns>
        [Obsolete("use AsClonedBitmap or AsSharedBitmap instead.")]
        public static Bitmap AsBitmap(this IImage image)
        {
            return image.As<Bitmap>();
        }

        /// <summary>
        /// Convenience method to access WinForms <see cref="IImage"/> instance as WinForms <see cref="Bitmap"/>.
        /// The returned <see cref="Bitmap"/> is cloned and must be disposed by caller.
        /// </summary>
        /// <param name="iimage"><see cref="IImage"/> object.</param>
        /// <returns><see cref="Bitmap"/> contents of <paramref name="image"/>.</returns>
        public static Bitmap AsClonedBitmap(this IImage iimage)
        {
            return iimage.As<Bitmap>()?.Clone() as Bitmap;
        }

        /// <summary>
        /// Convenience method to access WinForms <see cref="IImage"/> instance as WinForms <see cref="Bitmap"/>.
        /// The returned <see cref="Bitmap"/> will be disposed when the <see cref="IImage"/> is disposed.
        /// </summary>
        /// <param name="iimage"><see cref="IImage"/> object.</param>
        /// <returns><see cref="Bitmap"/> contents of <paramref name="image"/>.</returns>
        public static Bitmap AsSharedBitmap(this IImage iimage)
        {
            return iimage.As<Bitmap>();
        }
    }
}
