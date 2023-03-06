// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Models.Update;
public class UpdateSpecification
{
    public int PartitionKey { get; set; }
    public string Id { get; set; }
    public Level UpdateLevel { get; set; }
    public object Dataset { get; set; }
}

public enum Level
{
    Study,
    Series,
    Instance
}
