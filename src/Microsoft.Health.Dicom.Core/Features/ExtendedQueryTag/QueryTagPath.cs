// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// Represents a path composed of multiple tags to model a specific tag in a sequence.
    /// </summary>
    public class QueryTagPath
    {
        public IList<DicomTag> Tags { get; }

        public DicomTag LastTag { get; private set; }

        public QueryTagPath(string tagPath = default)
        {
            Tags = new List<DicomTag>();
            AddPath(tagPath);
        }

        /// <summary>
        /// Parses a tag path string into a 
        /// </summary>
        /// <param name="tagPath"></param>
        public void AddPath(string tagPath)
        {
            if (string.IsNullOrEmpty(tagPath)) return;

            foreach (var tagSegment in tagPath.Split('.'))
            {
                var tag = DicomTag.Parse(tagSegment);
                Tags.Add(tag);
                LastTag = tag;
            }
        }
    }
}
