// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public class RetrieveTransferSyntaxHandler : IRetrieveTransferSyntaxHandler
    {
        private static readonly IReadOnlyDictionary<ResourceType, AcceptableHeaderPatterns> AcceptablePatterns =
           new Dictionary<ResourceType, AcceptableHeaderPatterns>()
           {
                { ResourceType.Study, PatternsForGetStudy() },
                { ResourceType.Series, PatternsForGetSeries() },
                { ResourceType.Instance, PatternsForGetInstance() },
                { ResourceType.Frames, GetFrameAcceptablePatterns() },
           };

        private static AcceptableHeaderPatterns PatternsForGetStudy()
        {
            return new AcceptableHeaderPatterns(
                        new AcceptableHeaderPattern(
                        isMultipartRelated: true,
                        mediaType: KnownContentTypes.ApplicationDicom,
                        isTransferSyntaxMandatory: false,
                        transferSyntaxWhenMissing: string.Empty,
                        acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntaxUids.Original)));
        }

        private static AcceptableHeaderPatterns PatternsForGetSeries()
        {
            return new AcceptableHeaderPatterns(
                        new AcceptableHeaderPattern(
                        isMultipartRelated: true,
                        mediaType: KnownContentTypes.ApplicationDicom,
                        isTransferSyntaxMandatory: false,
                        transferSyntaxWhenMissing: string.Empty,
                        acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntaxUids.Original)));
        }

        private static AcceptableHeaderPatterns PatternsForGetInstance()
        {
            return new AcceptableHeaderPatterns(
                        new AcceptableHeaderPattern(
                        isMultipartRelated: false,
                        mediaType: KnownContentTypes.ApplicationDicom,
                        isTransferSyntaxMandatory: false,
                        transferSyntaxWhenMissing: string.Empty,
                        acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntaxUids.Original)));
        }

        private static AcceptableHeaderPatterns GetFrameAcceptablePatterns()
        {
            // Follow http://dicom.nema.org/medical/dicom/current/output/html/part18.html#sect_8.7.3
            return new AcceptableHeaderPatterns(
             new AcceptableHeaderPattern(
                 isMultipartRelated: true,
                 mediaType: KnownContentTypes.ApplicationOctetStream,
                 isTransferSyntaxMandatory: false,
                 transferSyntaxWhenMissing: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID,
                 acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntaxUids.Original, DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID)),
             new AcceptableHeaderPattern(
                 isMultipartRelated: true,
                 mediaType: KnownContentTypes.ImageJpeg,
                 isTransferSyntaxMandatory: false,
                 transferSyntaxWhenMissing: DicomTransferSyntax.JPEGProcess14SV1.UID.UID,
                 acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntax.JPEGProcess14SV1, DicomTransferSyntax.JPEGProcess1, DicomTransferSyntax.JPEGProcess2_4, DicomTransferSyntax.JPEGProcess14)),
             new AcceptableHeaderPattern(
                 isMultipartRelated: true,
                 mediaType: KnownContentTypes.ImageDicomRle,
                 isTransferSyntaxMandatory: false,
                 transferSyntaxWhenMissing: DicomTransferSyntax.RLELossless.UID.UID,
                 acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntax.RLELossless)),
             new AcceptableHeaderPattern(
                 isMultipartRelated: true,
                 mediaType: KnownContentTypes.ImageJpegLs,
                 isTransferSyntaxMandatory: false,
                 transferSyntaxWhenMissing: DicomTransferSyntax.JPEGLSLossless.UID.UID,
                 acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntax.JPEGLSLossless, DicomTransferSyntax.JPEGLSNearLossless)),
             new AcceptableHeaderPattern(
                 isMultipartRelated: true,
                 mediaType: KnownContentTypes.ImageJpeg2000,
                 isTransferSyntaxMandatory: false,
                 transferSyntaxWhenMissing: DicomTransferSyntax.JPEG2000Lossless.UID.UID,
                 acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntax.JPEG2000Lossless, DicomTransferSyntax.JPEG2000Lossy)),
             new AcceptableHeaderPattern(
                 isMultipartRelated: true,
                 mediaType: KnownContentTypes.ImageJpeg2000Part2,
                 isTransferSyntaxMandatory: false,
                 transferSyntaxWhenMissing: DicomTransferSyntax.JPEG2000Part2MultiComponentLosslessOnly.UID.UID,
                 acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntax.JPEG2000Part2MultiComponentLosslessOnly, DicomTransferSyntax.JPEG2000Part2MultiComponent)),
             new AcceptableHeaderPattern(
                 isMultipartRelated: true,
                 mediaType: KnownContentTypes.VideoMpeg2,
                 isTransferSyntaxMandatory: false,
                 transferSyntaxWhenMissing: DicomTransferSyntax.MPEG2.UID.UID,
                 acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntax.MPEG2, DicomTransferSyntax.MPEG2MainProfileHighLevel)),
             new AcceptableHeaderPattern(
                 isMultipartRelated: true,
                 mediaType: KnownContentTypes.VideoMp4,
                 isTransferSyntaxMandatory: false,
                 transferSyntaxWhenMissing: DicomTransferSyntax.MPEG4AVCH264HighProfileLevel41.UID.UID,
                 acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(
                     DicomTransferSyntax.MPEG4AVCH264HighProfileLevel41,
                     DicomTransferSyntax.MPEG4AVCH264BDCompatibleHighProfileLevel41,
                     DicomTransferSyntax.MPEG4AVCH264HighProfileLevel42For2DVideo,
                     DicomTransferSyntax.MPEG4AVCH264HighProfileLevel42For3DVideo,
                     DicomTransferSyntax.MPEG4AVCH264StereoHighProfileLevel42)));
        }

        private static ISet<string> GetAcceptableTransferSyntaxSet(params DicomTransferSyntax[] transferSyntaxes)
        {
            return GetAcceptableTransferSyntaxSet(transferSyntaxes.Select(item => item.UID.UID).ToArray());
        }

        private static ISet<string> GetAcceptableTransferSyntaxSet(params string[] transferSyntaxes)
        {
            return new HashSet<string>(transferSyntaxes, StringComparer.InvariantCultureIgnoreCase);
        }

        public string GetTransferSyntax(ResourceType resourceType, IEnumerable<AcceptHeader> acceptHeaders)
        {
            AcceptableHeaderPatterns patterns = AcceptablePatterns[resourceType];

            // get all accceptable headers and sort by quality descendently
            SortedDictionary<AcceptHeader, string> accepted = new SortedDictionary<AcceptHeader, string>(new AcceptHeaderQuantityComparer());
            foreach (AcceptHeader header in acceptHeaders)
            {
                AcceptableHeaderPattern acceptableHeaderPattern;
                string transfersyntax;
                if (patterns.TryGetMatchedPattern(header, out acceptableHeaderPattern, out transfersyntax))
                {
                    accepted.Add(header, transfersyntax);
                }
            }

            if (accepted.Count == 0)
            {
                // TODO: localize
                throw new BadRequestException("The requested content type and transfer syntax cannot be handled");
            }

            // Last elment has largest quantity
            return accepted.Last().Value;
        }
    }
}
