// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Models.Common;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Features.Export;

internal sealed class IdentifierExportSourceProvider : IExportSourceProvider
{
    public ExportSourceType Type => ExportSourceType.Identifiers;

    public IExportSource Create(IServiceProvider provider, IConfiguration config, PartitionEntry partition)
    {
        throw new NotImplementedException();
    }

    public void Validate(IConfiguration config)
    {
        EnsureArg.IsNotNull(config, nameof(config));

        IdentifierExportOptions options = config.Get<IdentifierExportOptions>();
        if ((options.Values?.Count).GetValueOrDefault() == 0)
            throw new ValidationException("No identifiers found.");

        try
        {
            foreach (string value in options.Values)
            {
                if (value == null)
                    throw new ValidationException(string.Format(CultureInfo.CurrentCulture, DicomCoreResource.InvalidDicomIdentifier, value));

                DicomIdentifier.Parse(value);
            }
        }
        catch (FormatException ex)
        {
            throw new ValidationException(ex.Message, ex.InnerException);
        }
    }
}
