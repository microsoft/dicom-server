// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class CustomTagEntryFormalizer : ICustomTagEntryFormalizer
    {
        private readonly IDicomTagParser _dicomTagParser;

        public CustomTagEntryFormalizer(IDicomTagParser dicomTagParser)
        {
            EnsureArg.IsNotNull(dicomTagParser, nameof(dicomTagParser));
            _dicomTagParser = dicomTagParser;
        }

        public CustomTagEntry Formalize(CustomTagEntry customTagEntry)
        {
            DicomTag[] tags;
            if (!_dicomTagParser.TryParse(customTagEntry.Path, out tags, supportMultiple: false))
            {
                // not a valid dicom tag path
                throw new CustomTagEntryValidationException(
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.InvalidCustomTag, customTagEntry));
            }

            DicomTag tag = tags[0];
            string path = tag.GetPath();
            string vr = customTagEntry.VR;

            // when VR is not specified for standard tag,
            if (!tag.IsPrivate && tag.DictionaryEntry != DicomDictionary.UnknownTag)
            {
                if (string.IsNullOrWhiteSpace(vr))
                {
                    vr = tag.GetDefaultVR()?.Code;
                }
            }

            vr = vr.ToUpperInvariant();

            return new CustomTagEntry(path, vr, customTagEntry.Level, customTagEntry.Status);
        }
    }
}
