// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Configs;

namespace Microsoft.Health.Dicom.Operations.Functions.Configs
{
    /// <summary>
    /// Represents configuration settings related to the Dicom functions.
    /// </summary>
    public class DicomFunctionsConfiguration
    {
        public const string SectionName = "DicomFunctions";
        public ReindexOperationConfiguration Reindex { get; } = new ReindexOperationConfiguration();
    }
}
