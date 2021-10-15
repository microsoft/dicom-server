// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Core.Features.Context;

namespace Microsoft.Health.Dicom.Core.Features.Context
{
    public interface IDicomRequestContext : IRequestContext
    {
        string StudyInstanceUid { get; set; }

        string SeriesInstanceUid { get; set; }

        string SopInstanceUid { get; set; }

        bool IsTranscodeRequested { get; set; }

        long BytesTranscoded { get; set; }

        long ResponseSize { get; set; }

        string PartitionId { get; set; }
    }
}
