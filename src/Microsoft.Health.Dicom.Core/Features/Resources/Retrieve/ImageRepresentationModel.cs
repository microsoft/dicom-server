// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Retrieve
{
    public class ImageRepresentationModel
    {
        public static readonly ImageRepresentationModel Jpeg = new ImageRepresentationModel()
        {
            CodecInfo = ImageCodecInfo.GetImageEncoders().First(x => x.MimeType == "image/jpeg"),
            EncoderParameters = new EncoderParameters(1) { Param = { [0] = new EncoderParameter(Encoder.Quality, 90L) } },
        };

        public static readonly ImageRepresentationModel Png = new ImageRepresentationModel
        {
            CodecInfo = ImageCodecInfo.GetImageEncoders().First(x => x.MimeType == "image/png"),
            EncoderParameters = null,
        };

        private static readonly List<ImageRepresentationModel> SupportedRepresentations =
            new List<ImageRepresentationModel>
            {
                Jpeg,
                Png,
            };

        public ImageCodecInfo CodecInfo { get; private set; }

        public EncoderParameters EncoderParameters { get; private set; }

        public static ImageRepresentationModel Parse(string mediaType)
        {
            return SupportedRepresentations.Single(x => x.CodecInfo.MimeType.Equals(mediaType, StringComparison.InvariantCulture));
        }
    }
}
