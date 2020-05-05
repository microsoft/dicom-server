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
    /// Provides functionality to find the appropriate <see cref="IInstanceEntryReader"/>.
    /// </summary>
    public class InstanceEntryReaderManager : IInstanceEntryReaderManager
    {
        private readonly IEnumerable<IInstanceEntryReader> _dicomInstanceEntryReaders;

        public InstanceEntryReaderManager(IEnumerable<IInstanceEntryReader> dicomInstanceEntryReaders)
        {
            EnsureArg.IsNotNull(dicomInstanceEntryReaders, nameof(dicomInstanceEntryReaders));

            _dicomInstanceEntryReaders = dicomInstanceEntryReaders;
        }

        /// <inheritdoc />
        public IInstanceEntryReader FindReader(string contentType)
        {
            EnsureArg.IsNotNull(contentType, nameof(contentType));

            return _dicomInstanceEntryReaders.FirstOrDefault(reader => reader.CanRead(contentType));
        }
    }
}
