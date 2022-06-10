// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Export;
using Microsoft.Health.Dicom.Core.Models.Export;
using Microsoft.Health.Dicom.Functions.Export;
using Microsoft.Health.Operations.Functions.DurableTask;
using NSubstitute;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Export;

public partial class ExportDurableFunctionTests
{
    private const ExportSourceType SourceType = ExportSourceType.Identifiers;
    private const ExportDestinationType DestinationType = ExportDestinationType.AzureBlob;

    private readonly ExportDurableFunction _function;
    private readonly IExportSourceProvider _sourceProvider;
    private readonly IExportSinkProvider _sinkProvider;
    private readonly ExportOptions _options;
    private readonly IServiceProvider _serviceProvider;

    public ExportDurableFunctionTests()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();
        _sourceProvider = Substitute.For<IExportSourceProvider>();
        _sinkProvider = Substitute.For<IExportSinkProvider>();
        _options = new ExportOptions
        {
            MaxParallelThreads = 1,
            RetryOptions = new ActivityRetryOptions { MaxNumberOfAttempts = 5 }
        };

        _sourceProvider.Type.Returns(SourceType);
        _sinkProvider.Type.Returns(DestinationType);
        _function = new ExportDurableFunction(
            new ExportSourceFactory(_serviceProvider, new IExportSourceProvider[] { _sourceProvider }),
            new ExportSinkFactory(_serviceProvider, new IExportSinkProvider[] { _sinkProvider }),
            Options.Create(_options));
    }
}
