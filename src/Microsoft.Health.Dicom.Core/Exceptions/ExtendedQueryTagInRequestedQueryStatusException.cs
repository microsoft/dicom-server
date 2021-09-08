// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when the extended query tag already in requested query state.
    /// </summary>
    public class ExtendedQueryTagInRequestedQueryStatusException : DicomServerException
    {
        public ExtendedQueryTagInRequestedQueryStatusException(string tagPath, QueryTagQueryStatus queryStatus)
            : base(string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ExtendedQueryTagInRequestedQueryStatus, EnsureArg.IsNotNullOrWhiteSpace(tagPath), EnsureArg.EnumIsDefined(queryStatus)))
        {
        }
    }
}
