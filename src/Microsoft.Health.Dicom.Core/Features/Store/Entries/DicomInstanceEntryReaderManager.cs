// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Store.Entries
{
    /// <summary>
    /// Provides functionality to find the appropriate <see cref="IDicomInstanceEntryReader"/>.
    /// </summary>
    public class DicomInstanceEntryReaderManager : IDicomInstanceEntryReaderManager
    {
        private readonly IEnumerable<IDicomInstanceEntryReader> _dicomInstanceEntryReaders;

        public DicomInstanceEntryReaderManager(IEnumerable<IDicomInstanceEntryReader> dicomInstanceEntryReaders)
        {
            EnsureArg.IsNotNull(dicomInstanceEntryReaders, nameof(dicomInstanceEntryReaders));

            _dicomInstanceEntryReaders = dicomInstanceEntryReaders;
        }

        /// <inheritdoc />
        public IDicomInstanceEntryReader FindReader(string contentType)
        {
            EnsureArg.IsNotNull(contentType, nameof(contentType));

            return _dicomInstanceEntryReaders.FirstOrDefault(reader => reader.CanRead(contentType));
        }
    }
}
