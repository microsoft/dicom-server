// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Health.Dicom.Core.Shared;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// External representation of a extended query tag entry for add.
    /// </summary>
    public class AddExtendedQueryTagEntry : ExtendedQueryTagEntry, IValidatableObject
    {
        /// <summary>
        /// Level of this tag. Could be Study, Series or Instance.
        /// </summary>
        public string Level { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            string property = null;
            if (string.IsNullOrWhiteSpace(Path))
            {
                property = "Path";
                yield return new ValidationResult(string.Format(DicomCoreResource.AddExtendedQueryTagEntryPropertyNotSpecified, property), new[] { property });
            }

            if(string.IsNullOrWhiteSpace(Level))
            {
                property = "Level";
                yield return new ValidationResult(string.Format(DicomCoreResource.AddExtendedQueryTagEntryPropertyNotSpecified, property), new[] { property });
            }

            if(!Enum.TryParse(typeof(QueryTagLevel), Level, true, out object result))
            {
                property = "Level";
                yield return new ValidationResult(string.Format(DicomCoreResource.InvalidDicomTagLevel, Level), new[] { property });
            }
        }
    }
}
