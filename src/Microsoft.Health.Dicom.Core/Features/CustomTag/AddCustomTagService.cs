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

        public AddCustomTagService(ICustomTagStore customTagStore, ICustomTagEntryValidator customTagEntryValidator, ILogger<AddCustomTagService> logger)
        {
            EnsureArg.IsNotNull(customTagStore, nameof(customTagStore));
            EnsureArg.IsNotNull(customTagEntryValidator, nameof(customTagEntryValidator));
            EnsureArg.IsNotNull(dicomTagParser, nameof(dicomTagParser));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _customTagStore = customTagStore;
            _customTagEntryValidator = customTagEntryValidator;
            _dicomTagParser = dicomTagParser;
            _logger = logger;
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

        public async Task DeleteCustomTagAsync(string tagPath, CancellationToken cancellationToken = default)
        {
            DicomTag[] tags;
            if (!_dicomTagParser.TryParse(tagPath, out tags, supportMultiple: false))
            {
                throw new InvalidCustomTagPathException(
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.InvalidCustomTagPath, tagPath == null ? string.Empty : tagPath));
            }

            string path = tags[0].GetPath();

            CustomTagStoreEntry customTagStoreEntry = await _customTagStore.GetCustomTagAsync(path, cancellationToken);

            if (customTagStoreEntry == null)
            {
                throw new Exception("Not Found");
            }

            await _customTagStore.StartDeleteCustomTagAsync(customTagStoreEntry.Key, cancellationToken);

            CustomTagIndexType indexType = GetIndexType(DicomVR.Parse(customTagStoreEntry.VR));
            if (indexType == CustomTagIndexType.Unknown)
            {
                throw new ApplicationException("Unknwon error.");
            }

            long affectedRows;

            do
            {
                switch (indexType)
                {
                    case CustomTagIndexType.StringIndex:
                        affectedRows = await _customTagStore.DeleteCustomTagStringIndexAsync(customTagStoreEntry.Key, DeleteTop, cancellationToken);
                        break;
                    case CustomTagIndexType.LongIndex:
                        affectedRows = await _customTagStore.DeleteCustomTagLongIndexAsync(customTagStoreEntry.Key, DeleteTop, cancellationToken);
                        break;
                    case CustomTagIndexType.DoubleIndex:
                        affectedRows = await _customTagStore.DeleteCustomTagDoubleIndexAsync(customTagStoreEntry.Key, DeleteTop, cancellationToken);
                        break;
                    case CustomTagIndexType.DateTimeIndex:
                        affectedRows = await _customTagStore.DeleteCustomTagDateTimeIndexAsync(customTagStoreEntry.Key, DeleteTop, cancellationToken);
                        break;
                    case CustomTagIndexType.PersonNameIndex:
                        affectedRows = await _customTagStore.DeleteCustomTagPersonNameIndexAsync(customTagStoreEntry.Key, DeleteTop, cancellationToken);
                        break;
                    case CustomTagIndexType.Unknown:
                    default:
                        throw new ApplicationException("Unknwon error.");
                }
            }
            while (affectedRows == DeleteTop);

            await _customTagStore.CompleteDeleteCustomTagAsync(customTagStoreEntry.Key, cancellationToken);
        }

        private static CustomTagIndexType GetIndexType(DicomVR vr)
        {
            Dictionary<DicomVR, CustomTagIndexType> mapping = new Dictionary<DicomVR, CustomTagIndexType>()
            {
                { DicomVR.AE, CustomTagIndexType.StringIndex },
                { DicomVR.AS, CustomTagIndexType.StringIndex },
                { DicomVR.AT,  CustomTagIndexType.LongIndex },
                { DicomVR.CS,   CustomTagIndexType.StringIndex },
                { DicomVR.DA,   CustomTagIndexType.DateTimeIndex },
                { DicomVR.DS,  CustomTagIndexType.StringIndex },
                { DicomVR.DT,  CustomTagIndexType.DateTimeIndex },
                { DicomVR.FL,  CustomTagIndexType.DoubleIndex },
                { DicomVR.FD,  CustomTagIndexType.DoubleIndex },
                { DicomVR.IS,   CustomTagIndexType.StringIndex },
                { DicomVR.LO,   CustomTagIndexType.StringIndex },
                { DicomVR.PN,   CustomTagIndexType.StringIndex },
                { DicomVR.SH,  CustomTagIndexType.StringIndex },
                { DicomVR.SL,   CustomTagIndexType.LongIndex },
                { DicomVR.SS,   CustomTagIndexType.LongIndex },
                { DicomVR.TM,   CustomTagIndexType.DateTimeIndex },
                { DicomVR.UI,   CustomTagIndexType.StringIndex },
                { DicomVR.UL,   CustomTagIndexType.LongIndex },
                { DicomVR.US,   CustomTagIndexType.LongIndex },
            };

            return mapping.GetValueOrDefault(vr, CustomTagIndexType.Unknown);
        }
    }
}
