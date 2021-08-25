// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Anonymizer.Core.Models
{
    public class AnonymizerEngineOptions
    {
        public AnonymizerEngineOptions(bool validateInput = false, bool validateOutput = false)
        {
            ValidateInput = validateInput;
            ValidateOutput = validateOutput;
        }

        public bool ValidateInput { get; }

        public bool ValidateOutput { get; }
    }
}
