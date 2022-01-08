// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using EnsureThat;

namespace TestTagPath
{
    public static class DicomSequenceExtensions
    {
        /// <summary>
        /// Gets DicomElements from a DicomDataset by following a QueryTagPath/>.
        /// </summary>
        /// <param name="dataset">The DicomSequence to be traversed.</param>
        /// <param name="tagPath">The DICOM tag path.</param>
        /// <returns>The DicomItems specified by the tag path.</returns>
        public static IEnumerable<DicomElement> GetLastPathElements(this DicomDataset dataset, QueryTagPath tagPath)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));
            EnsureArg.IsNotNull(tagPath, nameof(tagPath));

            var returnElements = new List<DicomElement>();

            // base case
            if (tagPath.Tags.Count == 0) return returnElements;

            DicomItem item = dataset.GetDicomItem<DicomItem>(tagPath.Tags[0]);

            if (item == null) return returnElements;

            var newTagPath = new QueryTagPath(tagPath.Tags.Skip(1));

            if (item is DicomSequence)
            {
                DicomSequence sequence = (DicomSequence)item;

                returnElements.AddRange(sequence.Items.Select(x => x.GetLastPathElements(newTagPath)).SelectMany(x => x));
            }
            else if (item is DicomElement)
            {
                returnElements.Add((DicomElement)item);
            }

            return returnElements;
        }
    }
}
