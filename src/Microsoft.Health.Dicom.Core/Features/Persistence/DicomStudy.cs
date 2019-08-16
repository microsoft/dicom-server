// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Core.Features.Persistence
{
    public class DicomStudy
    {
        private const StringComparison EqualsStringComparison = StringComparison.Ordinal;

        [JsonConstructor]
        public DicomStudy(string studyInstanceUID)
        {
            // Run the instance identifiers through the regular expression check.
            EnsureArg.Matches(studyInstanceUID, DicomIdentifierValidator.IdentifierRegex, nameof(studyInstanceUID));

            StudyInstanceUID = studyInstanceUID;
        }

        public string StudyInstanceUID { get; }

        public override bool Equals(object obj)
        {
            if (obj is DicomStudy identity)
            {
                return StudyInstanceUID.Equals(identity.StudyInstanceUID, EqualsStringComparison);
            }

            return false;
        }

        public override int GetHashCode()
            => StudyInstanceUID.GetHashCode(EqualsStringComparison);
    }
}
