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
    /// 
    /// </summary>
    public sealed class WorkitemSerializer : IWorkitemSerializer
    {
        /// <inheritdoc/>
        public async Task<IEnumerable<DicomDataset>> DeserializeAsync(Stream stream, string contentType)
        {
            EnsureArg.IsNotNull(stream, nameof(stream));
            EnsureArg.IsNotEmptyOrWhiteSpace(contentType, nameof(contentType));

            if (!string.Equals(contentType, KnownContentTypes.ApplicationJson, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnsupportedMediaTypeException(contentType);
            }

            var serializerOptions = new JsonSerializerOptions();
            serializerOptions.Converters.Add(new DicomJsonConverter());

            using (var streamReader = new StreamReader(stream))
            {
                string json = await streamReader.ReadToEndAsync().ConfigureAwait(false);

                IEnumerable<DicomDataset> datasets = JsonSerializer.Deserialize<IEnumerable<DicomDataset>>(json, serializerOptions);

                return datasets;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IWorkitemSerializer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        Task<IEnumerable<DicomDataset>> DeserializeAsync(Stream stream, string contentType);
    }
}
