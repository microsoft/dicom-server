// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Common
{
    /// <summary>
    /// Provides functionality to manage UPS-RS workitems.
    /// </summary>
    public interface IWorkitemStore
    {
        Task AddWorkitemAsync(WorkitemInstanceIdentifier identifier, DicomDataset dataset, CancellationToken cancellationToken);


        Task<DicomDataset> GetWorkitemAsync(WorkitemInstanceIdentifier identifier, CancellationToken cancellationToken);
    }
}
