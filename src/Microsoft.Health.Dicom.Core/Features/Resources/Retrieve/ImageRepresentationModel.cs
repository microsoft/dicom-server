// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;

namespace Microsoft.Health.Dicom.Core.Features.Resources
{
    public class ImageRepresentationModel
    {
        public static readonly ImageRepresentationModel JPEG = new ImageRepresentationModel()
        {
            CodecInfo = ImageCodecInfo.GetImageEncoders().FirstOrDefault(x => x.MimeType == "image/jpeg"),
            EncoderParameters = new EncoderParameters(1) { Param = { [0] = new EncoderParameter(Encoder.Quality, 90L) } },
        };

        public static readonly ImageRepresentationModel PNG = new ImageRepresentationModel
        {
            CodecInfo = ImageCodecInfo.GetImageEncoders().FirstOrDefault(x => x.MimeType == "image/png"),
            EncoderParameters = null,
        };

        private static readonly List<ImageRepresentationModel> SupportedRepresenations = new List<ImageRepresentationModel>();

        static ImageRepresentationModel()
        {
            SupportedRepresenations.Add(JPEG);
            SupportedRepresenations.Add(PNG);
        }

        public ImageCodecInfo CodecInfo { get; private set; }

        public EncoderParameters EncoderParameters { get; private set; }

        public static ImageRepresentationModel Parse(string mediaType)
        {
            return SupportedRepresenations.Where(x => x.CodecInfo.MimeType.Equals(mediaType, StringComparison.InvariantCulture)).Single();
        }
    }
}
