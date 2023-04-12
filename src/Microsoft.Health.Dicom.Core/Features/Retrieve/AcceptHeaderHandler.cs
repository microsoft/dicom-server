// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve;

public class AcceptHeaderHandler : IAcceptHeaderHandler
{
    protected internal static readonly IReadOnlyDictionary<ResourceType, List<AcceptHeaderDescriptor>>
        AcceptableDescriptors =
            new Dictionary<ResourceType, List<AcceptHeaderDescriptor>>()
            {
                { ResourceType.Study, DescriptorsForGetNonFrameResource(PayloadTypes.MultipartRelated) },
                { ResourceType.Series, DescriptorsForGetNonFrameResource(PayloadTypes.MultipartRelated) },
                { ResourceType.Instance, DescriptorsForGetNonFrameResource(PayloadTypes.SinglePartOrMultipartRelated) },
                { ResourceType.Frames, DescriptorsForGetFrame() },
            };

    private readonly IReadOnlyDictionary<ResourceType, List<AcceptHeaderDescriptor>> _acceptableDescriptors;

    private readonly ILogger<AcceptHeaderHandler> _logger;

    public AcceptHeaderHandler(ILogger<AcceptHeaderHandler> logger)
        : this(AcceptableDescriptors, logger)
    {
    }

    private AcceptHeaderHandler(
        IReadOnlyDictionary<ResourceType, List<AcceptHeaderDescriptor>> acceptableDescriptors,
        ILogger<AcceptHeaderHandler> logger)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(acceptableDescriptors, nameof(acceptableDescriptors));

        _acceptableDescriptors = acceptableDescriptors;
        _logger = logger;
    }

    /// <summary>
    /// Based on requested AcceptHeaders from users ordered by priority, create new AcceptHeader with valid
    /// TransferSyntax, leaving user input unmodified.
    /// </summary>
    /// <param name="resourceType">Used to understand if header properties are valid.</param>
    /// <param name="acceptHeaders">One or more headers as requested by user.</param>
    /// <returns>New accept header based on highest priority valid header requested.</returns>
    /// <exception cref="NotAcceptableException"></exception>
    public AcceptHeader GetValidAcceptHeader(ResourceType resourceType, IReadOnlyCollection<AcceptHeader> acceptHeaders)
    {
        EnsureArg.IsNotNull(acceptHeaders, nameof(acceptHeaders));
        List<AcceptHeader> orderedHeaders = acceptHeaders.OrderByDescending(x => x.Quality).ToList();

        _logger.LogInformation(
            "Getting transfer syntax for retrieving {ResourceType} with accept headers {AcceptHeaders}.",
            resourceType,
            string.Join(";", orderedHeaders));

        List<AcceptHeaderDescriptor> descriptors = _acceptableDescriptors[resourceType];

        // since these are ordered by quality already, we can return the first acceptable header
        foreach (AcceptHeader header in orderedHeaders)
        {
            foreach (AcceptHeaderDescriptor descriptor in descriptors)
            {
                if (descriptor.IsAcceptable(header))
                {
                    return new AcceptHeader(
                        header.MediaType,
                        header.PayloadType,
                        descriptor.GetTransferSyntax(header),
                        header.Quality);
                }
            }
        }

        //  none were valid
        throw new NotAcceptableException(DicomCoreResource.NotAcceptableHeaders);
    }

    private static List<AcceptHeaderDescriptor> DescriptorsForGetNonFrameResource(PayloadTypes payloadTypes)
    {
        return new List<AcceptHeaderDescriptor>
        {
            new AcceptHeaderDescriptor(
                payloadType: payloadTypes,
                mediaType: KnownContentTypes.ApplicationDicom,
                isTransferSyntaxMandatory: false,
                transferSyntaxWhenMissing: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID,
                acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntaxUids.Original,
                    DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID, DicomTransferSyntax.JPEG2000Lossless.UID.UID))
        };
    }

    private static List<AcceptHeaderDescriptor> DescriptorsForGetFrame()
    {
        return new List<AcceptHeaderDescriptor>
        {
            new AcceptHeaderDescriptor(
                payloadType: PayloadTypes.SinglePartOrMultipartRelated,
                mediaType: KnownContentTypes.ApplicationOctetStream,
                isTransferSyntaxMandatory: false,
                transferSyntaxWhenMissing: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID,
                acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntaxUids.Original,
                    DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID)),
            new AcceptHeaderDescriptor(
                payloadType: PayloadTypes.MultipartRelated,
                mediaType: KnownContentTypes.ImageJpeg2000,
                isTransferSyntaxMandatory: false,
                transferSyntaxWhenMissing: DicomTransferSyntax.JPEG2000Lossless.UID.UID,
                acceptableTransferSyntaxes: GetAcceptableTransferSyntaxSet(DicomTransferSyntax.JPEG2000Lossless))
        };
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
