// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.Fhir.Core.Features.Operations.Reindex;

namespace Microsoft.Health.Dicom.Api.Features.BackgroundServices
{
    public class CustomTagJobBackgroundService : BackgroundService
    {
        private readonly CustomTagJobWorker _customTagJobWorker;

        public CustomTagJobBackgroundService(CustomTagJobWorker customTagJobWorker)
        {
            _customTagJobWorker = customTagJobWorker;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await _customTagJobWorker.ExecuteAsync(cancellationToken);
        }
    }
}
