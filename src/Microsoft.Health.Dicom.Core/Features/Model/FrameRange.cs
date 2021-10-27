// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------


namespace Microsoft.Health.Dicom.Core.Features.Model
{
    public class FrameRange
    {
        public FrameRange(long offset, long length)
        {
            Offset = offset;
            Length = length;
        }

        public long Offset { get; }
        public long Length { get; }
    }
}
