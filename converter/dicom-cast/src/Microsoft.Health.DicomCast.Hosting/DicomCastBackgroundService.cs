// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.DicomCast.Core.Features.Worker;

namespace Microsoft.Health.DicomCast.Hosting
{
    public class DicomCastBackgroundService : BackgroundService
    {
        private readonly IDicomCastWorker _dicomCastWorker;

        public DicomCastBackgroundService(IDicomCastWorker dicomCastWorker)
        {
            EnsureArg.IsNotNull(dicomCastWorker, nameof(dicomCastWorker));

            _dicomCastWorker = dicomCastWorker;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
            => _dicomCastWorker.ExecuteAsync(stoppingToken);
    }
}
