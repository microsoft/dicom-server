// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    /// <summary>
    /// Cache current custom tag entries.
    /// </summary>
    public class IndexableDicomTag
    {
        public IndexableDicomTag(DicomTag tag, DicomVR vr, CustomTagLevel level, bool isCustomTag)
        {
            EnsureArg.IsNotNull(tag, nameof(tag));
            EnsureArg.IsNotNull(vr, nameof(vr));

            Tag = tag;
            VR = vr;
            IsCustomTag = isCustomTag;
            Level = level;
        }

        public DicomTag Tag { get; }

        public DicomVR VR { get; }

        public CustomTagLevel Level { get; }

        public bool IsCustomTag { get; }
    }
}
