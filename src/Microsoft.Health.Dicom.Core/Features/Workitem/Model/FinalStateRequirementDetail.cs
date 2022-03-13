// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Features.Workitem.Model;

public sealed class FinalStateRequirementDetail
{
    public FinalStateRequirementDetail(DicomTag dicomTag, FinalStateRequirementCode requirementCode, HashSet<FinalStateRequirementDetail> sequenceRequirements = default)
    {
        DicomTag = dicomTag;
        RequirementCode = requirementCode;
        SequenceRequirements = sequenceRequirements;
    }

    public DicomTag DicomTag { get; }

    public FinalStateRequirementCode RequirementCode { get; }

    public IReadOnlyCollection<FinalStateRequirementDetail> SequenceRequirements { get; }
}
