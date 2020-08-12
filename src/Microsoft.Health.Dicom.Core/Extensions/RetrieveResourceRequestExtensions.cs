// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using Dicom;
using Microsoft.Health.Abstractions.Exceptions;
using Microsoft.Health.Dicom.Core;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Api.Extensions
{
    public static class RetrieveResourceRequestExtensions
    {
        private static readonly ISet<string> AcceptableGetStudyMediaTypes = new HashSet<string>(new[] { KnownContentTypes.ApplicationDicom }, StringComparer.InvariantCultureIgnoreCase);
        private static readonly ISet<string> AcceptableGetSeriesMediaTypes = new HashSet<string>(new[] { KnownContentTypes.ApplicationDicom }, StringComparer.InvariantCultureIgnoreCase);
        private static readonly ISet<string> AcceptableGetInstanceMediaTypes = new HashSet<string>(new[] { KnownContentTypes.ApplicationDicom }, StringComparer.InvariantCultureIgnoreCase);
        private static readonly ISet<string> AcceptableGetFrameMediaTypes = new HashSet<string>(
            new[]
            {
                KnownContentTypes.ApplicationOctetStream,
                KnownContentTypes.ImageJpeg,
                KnownContentTypes.ImageDicomRle,
                KnownContentTypes.ImageJpegLs,
                KnownContentTypes.ImageJpeg2000,
                KnownContentTypes.ImageJpeg2000Part2,
            }, StringComparer.InvariantCultureIgnoreCase);

        private static readonly IDictionary<ResourceType, ISet<string>> AcceptableGetResourceMediaTypes = new Dictionary<ResourceType, ISet<string>>()
        {
            { ResourceType.Study, AcceptableGetStudyMediaTypes },
            { ResourceType.Series, AcceptableGetSeriesMediaTypes },
            { ResourceType.Instance, AcceptableGetInstanceMediaTypes },
            { ResourceType.Frames, AcceptableGetFrameMediaTypes },
        };

        // The first parameter of value indiate whether should get media type directly -- not a part of multipart/related
        // The second prameter of value indidate whethere should get media type from multipart/related
        private static readonly IDictionary<ResourceType, (bool, bool)> MediaTypeLocations = new Dictionary<ResourceType, (bool, bool)>()
        {
            { ResourceType.Study, (false, true) },
            { ResourceType.Series, (false, true) },
            { ResourceType.Instance, (true, false) },
            { ResourceType.Frames, (false, true) },
        };

        /// <summary>
        /// The default transfersyntax when it's missed from request
        /// </summary>
        private static readonly Dictionary<string, DicomTransferSyntax> DefaultTransferSyntaxes = new Dictionary<string, DicomTransferSyntax>(StringComparer.InvariantCultureIgnoreCase)
        {
            { KnownContentTypes.ApplicationOctetStream, DicomTransferSyntax.ExplicitVRLittleEndian },
            { KnownContentTypes.ImageJpeg, DicomTransferSyntax.JPEGProcess14SV1 },
            { KnownContentTypes.ImageDicomRle, DicomTransferSyntax.RLELossless },
            { KnownContentTypes.ImageJpegLs, DicomTransferSyntax.JPEGLSLossless },
            { KnownContentTypes.ImageJpeg2000, DicomTransferSyntax.JPEG2000Lossless },
            { KnownContentTypes.ImageJpeg2000Part2, DicomTransferSyntax.JPEG2000Part2MultiComponentLosslessOnly },
            { KnownContentTypes.VideoMpeg2, DicomTransferSyntax.MPEG2 },
            { KnownContentTypes.VideoMp4, DicomTransferSyntax.MPEG4AVCH264HighProfileLevel41 },
        };

        public static string GetTransferSyntax(this RetrieveResourceRequest request)
        {
            string mediaType;
            AcceptHeader acceptHeader = GetAcceptHeader(request, out mediaType);
            if (acceptHeader == null)
            {
                // TODO: localize
                throw new BadRequestException("Not able to find acceptable content type");
            }

            string syntaxUid = acceptHeader.GetTransferSyntax();
            bool originalTransferSyntaxRequested = AcceptHeader.IsOriginalTransferSyntaxRequested(syntaxUid);
            RetrieveRequestValidator.ValidateTransferSyntax(syntaxUid, originalTransferSyntaxRequested);

            // If transfer syntax is missing, return default one based on media type
            if (string.IsNullOrEmpty(syntaxUid))
            {
                if (!DefaultTransferSyntaxes.ContainsKey(mediaType))
                {
                    throw new UnsupportedMediaTypeException(string.Format(CultureInfo.InvariantCulture, DicomCoreResource.UnsupportedContentType, mediaType));
                }

                return DefaultTransferSyntaxes[mediaType].UID.UID;
            }

            return syntaxUid;
        }

        private static AcceptHeader GetAcceptHeader(RetrieveResourceRequest request, out string mediaType)
        {
            mediaType = null;
            ISet<string> acceptableMediaTypes = AcceptableGetResourceMediaTypes[request.ResourceType];
            var mediaTypeLocation = MediaTypeLocations[request.ResourceType];
            bool fromNonMultipart = mediaTypeLocation.Item1, fromMultipart = mediaTypeLocation.Item2;

            foreach (var header in request.AcceptHeaders)
            {
                if (header.IsMultipartRelated() && fromMultipart && acceptableMediaTypes.Contains(header.GetMediaTypeForMultipartRelated()))
                {
                    mediaType = header.GetMediaTypeForMultipartRelated();
                    return header;
                }

                if (!header.IsMultipartRelated() && fromNonMultipart && acceptableMediaTypes.Contains(header.MediaType))
                {
                    mediaType = header.MediaType;
                    return header;
                }
            }

            return null;
        }
    }
}
