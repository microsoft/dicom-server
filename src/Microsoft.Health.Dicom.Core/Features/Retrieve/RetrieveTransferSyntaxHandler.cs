// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
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
                { ResourceType.Study, DescriptorsForGetNonFrameResource(PayloadTypes.MultipartRelated) },
                { ResourceType.Series, DescriptorsForGetNonFrameResource(PayloadTypes.MultipartRelated) },
                { ResourceType.Instance, DescriptorsForGetNonFrameResource(PayloadTypes.SinglePartOrMultipartRelated) },
                { ResourceType.Frames, DescriptorsForGetFrame() },
           };

        private readonly IReadOnlyDictionary<ResourceType, AcceptHeaderDescriptors> _acceptableDescriptors;

        private readonly ILogger<RetrieveTransferSyntaxHandler> _logger;

        public RetrieveTransferSyntaxHandler(ILogger<RetrieveTransferSyntaxHandler> logger)
            : this(AcceptableDescriptors, logger)
        {
        }

        public RetrieveTransferSyntaxHandler(IReadOnlyDictionary<ResourceType, AcceptHeaderDescriptors> acceptableDescriptors, ILogger<RetrieveTransferSyntaxHandler> logger)
        {
            EnsureArg.IsNotNull(logger, nameof(logger));
            EnsureArg.IsNotNull(acceptableDescriptors, nameof(acceptableDescriptors));

            _acceptableDescriptors = acceptableDescriptors;
            _logger = logger;
        }

        public string GetTransferSyntax(ResourceType resourceType, IEnumerable<AcceptHeader> acceptHeaders, out AcceptHeaderDescriptor acceptHeaderDescriptor)
        {
            EnsureArg.IsNotNull(acceptHeaders, nameof(acceptHeaders));

            _logger.LogInformation("Getting transfer syntax for retrieving {ResourceType} with accept headers {AcceptHeaders}.", resourceType, string.Join(";", acceptHeaders));

            // TODO: disable multiple accept headers, will fully implement it later (https://microsofthealth.visualstudio.com/Health/_workitems/edit/75782)
            if (acceptHeaders.Count() > 1)
            {
                throw new NotAcceptableException(DicomCoreResource.NotSupportMultipleAcceptHeaders);
            }

            AcceptHeaderDescriptors descriptors = _acceptableDescriptors[resourceType];
            acceptHeaderDescriptor = null;

            // get all accceptable headers and sort by quality (ascendently)
            var accepted = new SortedDictionary<AcceptHeader, string>(new AcceptHeaderQualityComparer());
            foreach (AcceptHeader header in acceptHeaders)
            {
                if (descriptors.TryGetMatchedDescriptor(header, out acceptHeaderDescriptor, out string transfersyntax))
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

        private static AcceptHeaderDescriptors DescriptorsForGetNonFrameResource(PayloadTypes payloadTypes)
        {
            return new AcceptHeaderDescriptors(
                      new AcceptHeaderDescriptor(
                      payloadType: payloadTypes,
                      mediaType: KnownContentTypes.ApplicationDicom,
                      isTransferSyntaxMandatory: false,
                      transferSyntaxWhenMissing: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID,
                      acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntaxUids.Original, DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID, DicomTransferSyntax.JPEG2000Lossless.UID.UID)));
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
