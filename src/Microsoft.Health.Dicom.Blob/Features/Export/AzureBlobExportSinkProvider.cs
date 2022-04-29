// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Blob.Features.Export;

internal sealed class AzureBlobExportSinkProvider : IExportSinkProvider
{
    public ExportDestinationType Type => ExportDestinationType.AzureBlob;

    public IExportSink Create(IServiceProvider provider, IConfiguration config, Guid operationId)
    {
        EnsureArg.IsNotNull(provider, nameof(provider));
        EnsureArg.IsNotNull(config, nameof(config));

        AzureBlobExportOptions options = config.Get<AzureBlobExportOptions>();
        return new AzureBlobExportSink(
            provider.GetRequiredService<IFileStore>(),
            options.GetBlobContainerClient(provider.GetRequiredService<IOptionsMonitor<BlobClientOptions>>().Get("Export")),
            Options.Create(new AzureBlobExportFormatOptions
            {
                ErrorEncoding = Encoding.UTF8,
                ErrorFile = RelativeUriPath.Combine(ExportFilePattern.Format(options.Folder ?? string.Empty, operationId), "errors.json"),
                FilePattern = options.FilePattern,
                OperationId = operationId,
            }),
            provider.GetRequiredService<IOptions<BlobOperationOptions>>());
    }

    public void Validate(IConfiguration config)
    {
        EnsureArg.IsNotNull(config, nameof(config));

        // Validate
        AzureBlobExportOptions options = config.Get<AzureBlobExportOptions>();
        List<ValidationResult> results = options.Validate(new ValidationContext(this)).ToList();

        if (results.Count > 0)
            throw new ValidationException(results.First().ErrorMessage);

        // Post-processing
        config[nameof(AzureBlobExportOptions.FilePattern)] = ParsePattern(options.FilePattern, nameof(ExportFilePattern));
        config[nameof(AzureBlobExportOptions.Folder)] = ParsePattern(options.Folder, nameof(ExportFilePattern), ExportPatternPlaceholders.Operation);
    }

    private static string ParsePattern(string pattern, string name, ExportPatternPlaceholders placeholders = ExportPatternPlaceholders.All)
    {
        try
        {
            return ExportFilePattern.Parse(pattern.Trim(), placeholders);
        }
        catch (FormatException fe)
        {
            throw new ValidationException(string.Format(CultureInfo.CurrentCulture, DicomBlobResource.InvalidPattern, pattern, name), fe);
        }
    }
}
