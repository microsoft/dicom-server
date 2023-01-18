// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Health.Dicom.Functions.Client.TaskHub;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests.TaskHub;

public class InstanceTableTests : TrackingTableTests
{
    protected override ValueTask<bool> ExistsAsync(TableServiceClient tableServiceClient, string taskHubName, CancellationToken cancellationToken)
        => new InstanceTable(tableServiceClient, taskHubName).ExistsAsync(cancellationToken);

    protected override string GetName(string taskHubName)
        => InstanceTable.GetName(taskHubName);
}
