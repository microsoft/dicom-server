// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.CustomTag;

namespace Microsoft.Health.Fhir.Core.Features.Operations.Reindex
{
    public class ReindexJobTask : ICustomTagJobTask
    {
        public async Task ExecuteAsync(CustomTagJob customTagJob, CancellationToken cancellationToken = default)
        {
            await Task.Run(() => { throw new System.NotImplementedException(); });
        }
    }
}
