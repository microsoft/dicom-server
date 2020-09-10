// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public class RetrieveTransferSyntaxHandler : IRetrieveTransferSyntaxHandler
    {
        private static readonly IReadOnlyDictionary<ResourceType, AcceptHeaderDescriptors> AcceptableDescriptors =
        new Dictionary<ResourceType, AcceptHeaderDescriptors>()
        {
                { ResourceType.Study, DescriptorsForGetStudy() },
                { ResourceType.Series, DescriptorsForGetSeries() },
                { ResourceType.Instance, DescriptorsForGetInstance() },
                { ResourceType.Frames, DescriptorsForGetFrame() },
        };

        private readonly IReadOnlyDictionary<ResourceType, AcceptHeaderDescriptors> _acceptableDescriptors;

        public RetrieveTransferSyntaxHandler()
            : this(AcceptableDescriptors)
        {
        }

        public RetrieveTransferSyntaxHandler(IReadOnlyDictionary<ResourceType, AcceptHeaderDescriptors> acceptableDescriptors) => _acceptableDescriptors = acceptableDescriptors;

        public string GetTransferSyntax(ResourceType resourceType, IEnumerable<AcceptHeader> acceptHeaders, out AcceptHeaderDescriptor acceptableHeaderDescriptor)
        {
            EnsureArg.IsNotNull(acceptHeaders, nameof(acceptHeaders));
            AcceptHeaderDescriptors descriptors = _acceptableDescriptors[resourceType];
            acceptableHeaderDescriptor = null;

            // get all accceptable headers and sort by quality (ascendently)
            SortedDictionary<AcceptHeader, string> accepted = new SortedDictionary<AcceptHeader, string>(new AcceptHeaderQualityComparer());
            foreach (AcceptHeader header in acceptHeaders)
            {
                string transfersyntax;
                if (descriptors.TryGetMatchedDescriptor(header, out acceptableHeaderDescriptor, out transfersyntax))
                {
                    accepted.Add(header, transfersyntax);
                }
            }

            if (accepted.Count == 0)
            {
                throw new NotAcceptableException(DicomCoreResource.NotAcceptableHeaders);
            }

            // Last elment has largest quality
            return accepted.Last().Value;
        }

        private static AcceptHeaderDescriptors DescriptorsForGetStudy()
        {
            return new AcceptHeaderDescriptors(
                        new AcceptHeaderDescriptor(
                        payloadType: PayloadTypes.MultipartRelated,
                        mediaType: KnownContentTypes.ApplicationDicom,
                        isTransferSyntaxMandatory: true,
                        transferSyntaxWhenMissing: string.Empty,
                        acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntaxUids.Original)));
        }

        private static AcceptHeaderDescriptors DescriptorsForGetSeries()
        {
            return new AcceptHeaderDescriptors(
                        new AcceptHeaderDescriptor(
                        payloadType: PayloadTypes.MultipartRelated,
                        mediaType: KnownContentTypes.ApplicationDicom,
                        isTransferSyntaxMandatory: true,
                        transferSyntaxWhenMissing: string.Empty,
                        acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntaxUids.Original)));
        }

        private static AcceptHeaderDescriptors DescriptorsForGetInstance()
        {
            return new AcceptHeaderDescriptors(
                        new AcceptHeaderDescriptor(
                        payloadType: PayloadTypes.SinglePart,
                        mediaType: KnownContentTypes.ApplicationDicom,
                        isTransferSyntaxMandatory: true,
                        transferSyntaxWhenMissing: string.Empty,
                        acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntaxUids.Original)));
        }

        private static AcceptHeaderDescriptors DescriptorsForGetFrame()
        {
            return new AcceptHeaderDescriptors(
             new AcceptHeaderDescriptor(
                 payloadType: PayloadTypes.MultipartRelated,
                 mediaType: KnownContentTypes.ApplicationOctetStream,
                 isTransferSyntaxMandatory: false,
                 transferSyntaxWhenMissing: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID,
                 acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntaxUids.Original, DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID)),
             new AcceptHeaderDescriptor(
                 payloadType: PayloadTypes.MultipartRelated,
                 mediaType: KnownContentTypes.ImageJpeg2000,
                 isTransferSyntaxMandatory: false,
                 transferSyntaxWhenMissing: DicomTransferSyntax.JPEG2000Lossless.UID.UID,
                 acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntax.JPEG2000Lossless)));
        }

        private static ISet<string> GetAcceptableTransferSyntaxSet(params DicomTransferSyntax[] transferSyntaxes)
        {
            return GetAcceptableTransferSyntaxSet(transferSyntaxes.Select(item => item.UID.UID).ToArray());
        }

        private static ISet<string> GetAcceptableTransferSyntaxSet(params string[] transferSyntaxes)
        {
            return new HashSet<string>(transferSyntaxes, StringComparer.InvariantCultureIgnoreCase);
        }
    }
}
