// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;

namespace TestTagPath
{
    /// <summary>
    /// Represents a path composed of multiple tags to model a specific tag in a sequence.
    /// </summary>
    public class QueryTagPath
    {
        private readonly List<DicomTag> _tags = new List<DicomTag>();

        /// <summary>
        /// Make a tag path from a string.
        /// </summary>
        /// <param name="tagPath"></param>
        public QueryTagPath(string? tagPath = default)
        {
            if (!string.IsNullOrEmpty(tagPath))
            {
                AddPath(tagPath);
            }
        }

        /// <summary>
        /// Make a tag path from Dicom tags.
        /// </summary>
        /// <param name="tags"></param>
        public QueryTagPath(IEnumerable<DicomTag> tags)
        {
            _tags.AddRange(tags);
        }

        public IReadOnlyList<DicomTag> Tags => _tags.AsReadOnly();

        /// <summary>
        /// Adds segment(s) parsed from a string as tags in the path.
        /// </summary>
        /// <param name="tagPath"></param>
        public QueryTagPath AddPath(string tagPath)
        {
            if (string.IsNullOrEmpty(tagPath)) return this;

            var tags = tagPath.Split('.').Select(x => DicomTag.Parse(x));
            AddPath(tags);

            return this;
        }

        /// <summary>
        /// Adds a tag to the path.
        /// </summary>
        /// <param name="tag"></param>
        public QueryTagPath AddPath(DicomTag tag)
        {
            _tags.Add(tag);
            return this;
        }

        /// <summary>
        /// Adds tags to the path.
        /// </summary>
        /// <param name="tags"></param>
        public QueryTagPath AddPath(IEnumerable<DicomTag> tags)
        {
            _tags.AddRange(tags);
            return this;
        }
    }
}
