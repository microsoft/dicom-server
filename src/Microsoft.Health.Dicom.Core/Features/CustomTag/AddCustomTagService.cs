// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Messages.CustomTag;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class AddCustomTagService : IAddCustomTagService
    {
        private readonly ICustomTagStore _customTagStore;
        private readonly ICustomTagEntryValidator _customTagEntryValidator;
        private readonly ILogger<AddCustomTagService> _logger;
        private const int DeleteTop = 10000;

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

        /// <summary>
        /// Mapping from IndexType to the delete function
        /// </summary>
        private readonly IReadOnlyDictionary<CustomTagIndexType, Func<string, int, CancellationToken, Task<long>>> indexTypeAndDeleteFunctionMapping;

        /// <summary>
        /// Max records are deleted in each transaction.
        /// </summary>
        private const int MaxDeleteRecordCount = 10000;

        /// <summary>
        /// Delay between each delete transaction.
        /// </summary>
        private const int DelayBetweenDeleteTransaction = 100;

        public CustomTagService(ICustomTagStore customTagStore, ICustomTagEntryValidator customTagEntryValidator, IDicomTagParser dicomTagParser, ILogger<CustomTagService> logger)
        {
            EnsureArg.IsNotNull(customTagStore, nameof(customTagStore));
            EnsureArg.IsNotNull(customTagEntryValidator, nameof(customTagEntryValidator));
            EnsureArg.IsNotNull(dicomTagParser, nameof(dicomTagParser));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _customTagStore = customTagStore;
            _customTagEntryValidator = customTagEntryValidator;
            _dicomTagParser = dicomTagParser;
            _logger = logger;

            indexTypeAndDeleteFunctionMapping = new Dictionary<CustomTagIndexType, Func<string, int, CancellationToken, Task<long>>>()
            {
                { CustomTagIndexType.StringIndex, _customTagStore.DeleteCustomTagStringIndexAsync },
                { CustomTagIndexType.LongIndex, _customTagStore.DeleteCustomTagLongIndexAsync },
                { CustomTagIndexType.DoubleIndex, _customTagStore.DeleteCustomTagDoubleIndexAsync },
                { CustomTagIndexType.DateTimeIndex, _customTagStore.DeleteCustomTagDateTimeIndexAsync },
                { CustomTagIndexType.PersonNameIndex, _customTagStore.DeleteCustomTagPersonNameIndexAsync },
            };
        }

        public async Task<AddCustomTagResponse> AddCustomTagAsync(IEnumerable<CustomTagEntry> customTags, CancellationToken cancellationToken)
        {
            _customTagEntryValidator.ValidateCustomTags(customTags);

            IEnumerable<CustomTagEntry> result = customTags.Select(item =>
            {
                CustomTagEntry normalized = item.Normalize();
                normalized.Status = CustomTagStatus.Added;
                return normalized;
            });

            await _customTagStore.AddCustomTagsAsync(result, cancellationToken);

            // Current solution is synchronouse, no job uri is generated, so always return emtpy.
            return new AddCustomTagResponse(job: string.Empty);
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

            IEnumerable<CustomTagEntry> customTagEntries = await _customTagStore.GetCustomTagsAsync(normalizedPath, cancellationToken);
            if (customTagEntries.Count() == 0)
            {
                throw new CustomTagNotFoundException(
                   string.Format(CultureInfo.InvariantCulture, DicomCoreResource.CustomTagNotFound, tagPath ?? string.Empty));
            }

            CustomTagEntry customTagEntry = customTagEntries.First();

            await _customTagStore.StartDeleteCustomTagAsync(normalizedPath, cancellationToken);
            CustomTagIndexType indexType = CustomTagVRAndIndexTypeMapping[customTagEntry.VR];
            var deleteFunc = indexTypeAndDeleteFunctionMapping[indexType];
            long affectedRows;
            do
            {
                affectedRows = await deleteFunc.Invoke(normalizedPath, MaxDeleteRecordCount, cancellationToken);
                await Task.Delay(DelayBetweenDeleteTransaction);
            }
            while (affectedRows == MaxDeleteRecordCount);

            await _customTagStore.CompleteDeleteCustomTagAsync(normalizedPath, cancellationToken);
        }
    }
}
