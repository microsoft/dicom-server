// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.Workitem.Model;

public sealed class RequirementDetail
{
    public RequirementDetail(DicomTag dicomTag, RequirementCode requirementCode, HashSet<RequirementDetail> sequenceRequirements = default)
    {
        DicomTag = dicomTag;
        RequirementCode = requirementCode;
        SequenceRequirements = sequenceRequirements;
    }

    public DicomTag DicomTag { get; }

    public RequirementCode RequirementCode { get; }

    public IReadOnlyCollection<RequirementDetail> SequenceRequirements { get; }
}
