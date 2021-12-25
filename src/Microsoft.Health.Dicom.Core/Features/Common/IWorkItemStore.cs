// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Workitem;

namespace Microsoft.Health.Dicom.Core.Features.Common
{
    /// <summary>
    /// Provides functionalities managing the DICOM instance work-item.
    /// </summary>
    public interface IWorkitemStore
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="partitionKey"></param>
        /// <param name="workitemDataset"></param>
        /// <param name="queryTags"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<long> AddWorkitemAsync(int partitionKey, WorkitemDataset workitemDataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken);
    }
}
