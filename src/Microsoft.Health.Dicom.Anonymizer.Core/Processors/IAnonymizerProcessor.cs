// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    public interface IAnonymizerProcessor
    {
        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context);

        bool IsSupported(DicomItem item);
    }
}
