// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Health.Operations.Functions.DurableTask;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Functions.DeleteExtendedQueryTag;

public class DeleteExtendedQueryTagCheckpoint : DeleteExtendedQueryTagInput, IOrchestrationCheckpoint
{
    public DateTime? CreatedTime => throw new NotImplementedException();

    public int? PercentComplete => throw new NotImplementedException();

    public IReadOnlyCollection<string> ResourceIds => throw new NotImplementedException();

    public object GetResults(JToken output) => throw new NotImplementedException();
}
