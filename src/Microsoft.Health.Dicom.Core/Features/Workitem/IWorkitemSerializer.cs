// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Threading.Tasks;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to serialize and deserialize workitem instance.
    /// </summary>
    public interface IWorkitemSerializer
    {
        /// <summary>
        /// Deserialize Workitem json to a <see cref="DicomDataset"/>
        /// </summary>
        /// <param name="stream">The stream to read the workitem instances from.</param>
        /// <param name="contentType">The content type.</param>
        /// <returns></returns>
        Task<T> DeserializeAsync<T>(Stream stream, string contentType);
    }
}
