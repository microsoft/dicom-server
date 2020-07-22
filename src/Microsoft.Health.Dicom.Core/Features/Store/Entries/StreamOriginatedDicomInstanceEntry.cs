// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Core.Features.Store.Entries
{
    /// <summary>
    /// Represents a DICOM instance entry originated from stream.
    /// </summary>
    public sealed class StreamOriginatedDicomInstanceEntry : IDicomInstanceEntry
    {
        private readonly Stream _stream;
        private DicomFile _dicomFile;
        private int _dicomFileLoadedState = 0;
        private TaskCompletionSource<bool> _dicomFileLoadingCompletionSource = new TaskCompletionSource<bool>();

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamOriginatedDicomInstanceEntry"/> class.
        /// </summary>
        /// <param name="seekableStream">The stream.</param>
        /// <remarks>The <paramref name="seekableStream"/> must be seekable.</remarks>
        internal StreamOriginatedDicomInstanceEntry(Stream seekableStream)
        {
            // The stream must be seekable.
            EnsureArg.IsNotNull(seekableStream, nameof(seekableStream));
            EnsureArg.IsTrue(seekableStream.CanSeek, nameof(seekableStream));

            _stream = seekableStream;
        }

        /// <inheritdoc />
        public async ValueTask<DicomDataset> GetDicomDatasetAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref _dicomFileLoadedState, 1, 0) == 0)
            {
                // Load the file.
                _dicomFile = await DicomFile.OpenAsync(_stream, FileReadOption.SkipLargeTags);
                _dicomFileLoadingCompletionSource.SetResult(true);
            }

            await _dicomFileLoadingCompletionSource.Task;

            if (_dicomFile == null)
            {
                throw new InvalidInstanceException(DicomCoreResource.InvalidDicomInstance);
            }

            return _dicomFile.Dataset;
        }

        /// <inheritdoc />
        public ValueTask<Stream> GetStreamAsync(CancellationToken cancellationToken)
        {
            _stream.Seek(0, SeekOrigin.Begin);

            return new ValueTask<Stream>(_stream);
        }

        public async ValueTask DisposeAsync() => await _stream.DisposeAsync();
    }
}
