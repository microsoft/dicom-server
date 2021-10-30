// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common
{
    internal class DicomInstancesManager : IAsyncDisposable
    {
        private readonly IDicomWebClient _dicomWebClient;
        private readonly HashSet<InstanceIdentifier> _instanceIds;

        public DicomInstancesManager(IDicomWebClient dicomWebClient)
        {
            _dicomWebClient = EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));
            _instanceIds = new HashSet<InstanceIdentifier>();
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var id in _instanceIds)
            {
                try
                {
                    await _dicomWebClient.DeleteInstanceAsync(id.StudyInstanceUid, id.SeriesInstanceUid, id.SopInstanceUid);
                }
                catch (DicomWebException)
                {

                }
            }
        }

        public async Task StoreAsync(DicomFile dicomFile, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomFile, nameof(dicomFile));
            _instanceIds.Add(dicomFile.Dataset.ToInstanceIdentifier());
            await _dicomWebClient.StoreAsync(dicomFile, cancellationToken: cancellationToken);
        }

    }
}
