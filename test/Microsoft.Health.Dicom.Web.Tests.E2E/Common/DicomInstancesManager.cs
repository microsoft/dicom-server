// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common
{
    internal class DicomInstancesManager : IAsyncDisposable
    {

        private readonly IDicomWebClient _dicomWebClient;
        private readonly ConcurrentBag<DicomInstanceId> _instanceIds;

        public DicomInstancesManager(IDicomWebClient dicomWebClient)
        {
            _dicomWebClient = EnsureArg.IsNotNull(dicomWebClient, nameof(dicomWebClient));
            _instanceIds = new ConcurrentBag<DicomInstanceId>();
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var id in _instanceIds)
            {
                try
                {
                    await _dicomWebClient.DeleteInstanceAsync(id.StudyInstanceUid, id.SeriesInstanceUid, id.SopInstanceUid, id.PartitionName);
                }
                catch (DicomWebException)
                {

                }
            }
        }

        public async Task<DicomWebResponse<DicomDataset>> StoreAsync(DicomFile dicomFile, string studyInstanceUid = default, string partitionName = default, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomFile, nameof(dicomFile));
            _instanceIds.Add(DicomInstanceId.FromInstanceIdentifier(dicomFile.Dataset.ToInstanceIdentifier(), partitionName));
            return await _dicomWebClient.StoreAsync(dicomFile, studyInstanceUid, partitionName, cancellationToken);
        }

        public async Task<DicomWebResponse<DicomDataset>> StoreAsync(HttpContent content, string partitionName = default, CancellationToken cancellationToken = default, DicomInstanceId instanceId = default)
        {
            EnsureArg.IsNotNull(content, nameof(content));
            // Null instanceId indiates Store will fail
            if (instanceId != null)
            {
                _instanceIds.Add(instanceId);
            }
            return await _dicomWebClient.StoreAsync(content, partitionName, cancellationToken);
        }

        public async Task<DicomWebResponse<DicomDataset>> StoreAsync(IEnumerable<DicomFile> dicomFiles, string studyInstanceUid = default, string partitionName = default, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(dicomFiles, nameof(dicomFiles));
            foreach (var file in dicomFiles)
            {
                _instanceIds.Add(DicomInstanceId.FromInstanceIdentifier(file.Dataset.ToInstanceIdentifier(), partitionName));
            }

            return await _dicomWebClient.StoreAsync(dicomFiles, studyInstanceUid, partitionName, cancellationToken);
        }
    }
}
