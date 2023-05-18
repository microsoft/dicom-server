// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.Health.Dicom.Core.Exceptions;

public class ItemNotFoundException : ConditionalExternalException
{
    public ItemNotFoundException(Exception innerException, bool isExternal = false)
        : base(isExternal ? string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ExternalDataStoreOperationFailed, innerException?.Message) : DicomCoreResource.ItemNotFound, innerException, isExternal)
    {
    }
}
