// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class DeleteCustomTagService : IDeleteCustomTagService
    {
        private readonly ICustomTagStore _customTagStore;
        private readonly IDicomTagParser _dicomTagParser;
        private readonly ILogger<DeleteCustomTagService> _logger;

        /// <summary>
        /// Mapping from CustomTagVR to IndexType
        /// </summary>
        private static readonly IReadOnlyDictionary<string, CustomTagIndexType> CustomTagVRAndIndexTypeMapping = new Dictionary<string, CustomTagIndexType>()
            {
                { DicomVR.AE.Code, CustomTagIndexType.StringIndex },
                { DicomVR.AS.Code, CustomTagIndexType.StringIndex },
                { DicomVR.AT.Code, CustomTagIndexType.LongIndex },
                { DicomVR.CS.Code, CustomTagIndexType.StringIndex },
                { DicomVR.DA.Code, CustomTagIndexType.DateTimeIndex },
                { DicomVR.DS.Code, CustomTagIndexType.StringIndex },
                { DicomVR.DT.Code, CustomTagIndexType.DateTimeIndex },
                { DicomVR.FL.Code, CustomTagIndexType.DoubleIndex },
                { DicomVR.FD.Code, CustomTagIndexType.DoubleIndex },
                { DicomVR.IS.Code, CustomTagIndexType.StringIndex },
                { DicomVR.LO.Code, CustomTagIndexType.StringIndex },
                { DicomVR.PN.Code, CustomTagIndexType.StringIndex },
                { DicomVR.SH.Code, CustomTagIndexType.StringIndex },
                { DicomVR.SL.Code, CustomTagIndexType.LongIndex },
                { DicomVR.SS.Code, CustomTagIndexType.LongIndex },
                { DicomVR.TM.Code, CustomTagIndexType.DateTimeIndex },
                { DicomVR.UI.Code, CustomTagIndexType.StringIndex },
                { DicomVR.UL.Code, CustomTagIndexType.LongIndex },
                { DicomVR.US.Code, CustomTagIndexType.LongIndex },
            };

        public DeleteCustomTagService(ICustomTagStore customTagStore, IDicomTagParser dicomTagParser, ILogger<DeleteCustomTagService> logger)
        {
            EnsureArg.IsNotNull(customTagStore, nameof(customTagStore));
            EnsureArg.IsNotNull(dicomTagParser, nameof(dicomTagParser));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _customTagStore = customTagStore;
            _dicomTagParser = dicomTagParser;
            _logger = logger;
        }

        public async Task DeleteCustomTagAsync(string tagPath, CancellationToken cancellationToken)
        {
            DicomTag[] tags;
            if (!_dicomTagParser.TryParse(tagPath, out tags, supportMultiple: false))
            {
                throw new InvalidCustomTagPathException(
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.InvalidCustomTag, tagPath ?? string.Empty));
            }

            string normalizedPath = tags[0].GetPath();

            CustomTagEntry customTagEntry = await _customTagStore.GetCustomTagAsync(normalizedPath, cancellationToken);

            await _customTagStore.DeleteCustomTagAsync(normalizedPath, CustomTagVRAndIndexTypeMapping[customTagEntry.VR], cancellationToken);
        }
    }
}
