// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Store;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    /// <summary>
    /// Full Validation on Dicom dataset.
    /// </summary>
    internal class DatasetFullValidation : IDatasetValidation
    {
        public void Validate(DicomDataset dataset)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));
            foreach (var item in dataset)
            {
                item.ValidateDicomItem();
            }
        }
    }
}
