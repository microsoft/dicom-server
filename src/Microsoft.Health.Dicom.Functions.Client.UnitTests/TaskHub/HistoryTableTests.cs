// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Data.Tables;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Health.Dicom.Functions.Client.TaskHub;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests.TaskHub;

public class HistoryTableTests : TrackingTableTests
{
    protected override ValueTask<bool> ExistsAsync(TableServiceClient tableServiceClient, string tableName, CancellationToken cancellationToken)
        => new HistoryTable(tableServiceClient, tableName).ExistsAsync(cancellationToken);

    protected override string GetName(string taskHubName)
        => HistoryTable.GetName(taskHubName);
}
