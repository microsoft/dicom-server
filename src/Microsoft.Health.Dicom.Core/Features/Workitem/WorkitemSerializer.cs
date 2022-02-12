// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using FellowOakDicom.Serialization;
using Microsoft.Health.Abstractions.Exceptions;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to serialize and deserialize workitem instance.
    /// </summary>
    public sealed class WorkitemSerializer : IWorkitemSerializer
    {
        /// <inheritdoc/>
        public async Task<IEnumerable<DicomDataset>> DeserializeAsync(Stream stream, string contentType)
        {
            EnsureArg.IsNotNull(stream, nameof(stream));
            EnsureArg.IsNotEmptyOrWhiteSpace(contentType, nameof(contentType));

            if (!string.Equals(contentType, KnownContentTypes.ApplicationJson, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(contentType, KnownContentTypes.ApplicationDicomJson, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnsupportedMediaTypeException(contentType);
            }

            var serializerOptions = new JsonSerializerOptions();
            serializerOptions.Converters.Add(new DicomJsonConverter(autoValidate: false));

            using (var streamReader = new StreamReader(stream))
            {
                string json = await streamReader.ReadToEndAsync().ConfigureAwait(false);

                IEnumerable<DicomDataset> datasets = JsonSerializer.Deserialize<IEnumerable<DicomDataset>>(json, serializerOptions);

                return datasets;
            }
        }
    }
}
