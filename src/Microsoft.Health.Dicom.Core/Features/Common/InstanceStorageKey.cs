namespace Microsoft.Health.Dicom.Core.Features.Common;

public class InstanceStorageKey
{
    public long Watermark { get; init; }
    public long? InstanceKey { get; init; }
}