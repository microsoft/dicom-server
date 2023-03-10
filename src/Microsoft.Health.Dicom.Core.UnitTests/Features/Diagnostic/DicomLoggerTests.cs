// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Features.Diagnostic;
using Microsoft.Health.Dicom.Core.Features.Model;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Diagnostic;

public class DicomLoggerTests
{
    private readonly IDicomForwardingLogger _dicomForwardingLogger;
    private readonly ILogger<IDicomForwardingLogger> _logger = Substitute.For<ILogger<IDicomForwardingLogger>>();

    public DicomLoggerTests()
    {
        _dicomForwardingLogger = new DicomForwardingLogger(_logger);
    }

    [Fact]
    public void GivenAnAuditLog_WhenLogAudit_ThenExpectInformationLogged()
    {
        IReadOnlyDictionary<string, string> customHeaders = new Dictionary<string, string>() { { "A", "B" } };

        _dicomForwardingLogger.LogAudit(AuditAction.Executed, customHeaders);

        var expected = "Audit Log: { Executed, Custom Headers: { [A, B] } }";
        _logger.ReceivedWithAnyArgs(1).LogInformation(expected);
    }

    [Fact]
    public void GivenADiagnosticLog_WhenLogDiagnostic_ThenExpectInformationLogged()
    {
        IReadOnlyDictionary<string, string> customHeaders = new Dictionary<string, string>() { { "A", "B" } };

        _dicomForwardingLogger.LogDiagnostic(
            "A message.",
            new InstanceIdentifier("1", "2", "3"));

        var expected = "Diagnostic Log: { A message., dicomAdditionalInformation_studyInstanceUID=1;dicomAdditionalInformation_seriesInstanceUID=2;dicomAdditionalInformation_sopInstanceUID=3 }";
        _logger.ReceivedWithAnyArgs(1).LogInformation(expected);
    }
}
