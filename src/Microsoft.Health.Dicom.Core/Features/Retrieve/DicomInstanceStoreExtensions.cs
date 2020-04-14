// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public static class DicomInstanceStoreExtensions
    {
        public static async Task<IEnumerable<DicomInstanceIdentifier>> GetStudyInstancesToRetrieve(
                this IDicomInstanceStore dicomInstanceStore,
                string studyInstanceUid,
                CancellationToken cancellationToken)
        {
            IEnumerable<DicomInstanceIdentifier> instancesToRetrieve = await dicomInstanceStore.GetInstanceIdentifiersInStudyAsync(
                        studyInstanceUid,
                        cancellationToken);

            if (!instancesToRetrieve.Any())
            {
                throw new DicomInstanceNotFoundException();
            }

            return instancesToRetrieve;
        }

        public static async Task<IEnumerable<DicomInstanceIdentifier>> GetSeriesInstancesToRetrieve(
                this IDicomInstanceStore dicomInstanceStore,
                string studyInstanceUid,
                string seriesInstanceUid,
                CancellationToken cancellationToken)
        {
            IEnumerable<DicomInstanceIdentifier> instancesToRetrieve = await dicomInstanceStore.GetInstanceIdentifiersInSeriesAsync(
                        studyInstanceUid,
                        seriesInstanceUid,
                        cancellationToken);

            if (!instancesToRetrieve.Any())
            {
                throw new DicomInstanceNotFoundException();
            }

            return instancesToRetrieve;
        }

        public static async Task<IEnumerable<DicomInstanceIdentifier>> GetInstancesToRetrieve(
                this IDicomInstanceStore dicomInstanceStore,
                string studyInstanceUid,
                string seriesInstanceUid,
                string sopInstanceUid,
                CancellationToken cancellationToken)
        {
            IEnumerable<DicomInstanceIdentifier> instancesToRetrieve = await dicomInstanceStore.GetInstanceIdentifierAsync(
                        studyInstanceUid,
                        seriesInstanceUid,
                        sopInstanceUid,
                        cancellationToken);

            if (!instancesToRetrieve.Any())
            {
                throw new DicomInstanceNotFoundException();
            }

            return instancesToRetrieve;
        }
    }
}
