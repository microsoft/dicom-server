// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Models.Update;

public class UpdateSpecification
{
    public UpdateSpecification(IReadOnlyList<string> studyInstanceUids, DicomDataset changeDataset)
    {
        StudyInstanceUids = studyInstanceUids;
        ChangeDataset = changeDataset;
    }

    public IReadOnlyList<string> StudyInstanceUids { get; }

    public DicomDataset ChangeDataset { get; }
}
