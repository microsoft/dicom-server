// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Data.Tables;

namespace Microsoft.Health.Dicom.Functions.Client.TaskHub;

internal class InstanceTableName : TrackingTable
{
    public InstanceTableName(TableServiceClient tableServiceClient)
        : base(tableServiceClient)
    { }

    protected override string GetName(string taskHubName)
        => taskHubName + "Instances";
}
