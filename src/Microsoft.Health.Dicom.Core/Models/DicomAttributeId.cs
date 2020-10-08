// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Models
{
    public class DicomAttributeId
    {
        public DicomAttributeId(IReadOnlyList<DicomTag> path)
        {
            Path = path;
            IsPrivate = path.Any(x => x.IsPrivate);
        }

        public IReadOnlyList<DicomTag> Path { get; }

        public bool IsPrivate { get; }

        public override string ToString()
        {
            return string.Join(".", Path.Select(item => GetPathForTag(item)));
        }

        private static string GetPathForTag(DicomTag tag)
        {
            return To4Hex(tag.Group) + To4Hex(tag.Element);
        }

        private static string To4Hex(int num)
        {
            return string.Format("{0:X4}", num);
        }
    }
}
