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
        /// <param name="searchItem">The DICOM tag path.</param>
        /// <returns>The DicomElements specified by the tag path.</returns>
        public static IEnumerable<DicomElement> GetLastPathElements(this DicomDataset dataset, DicomItem searchItem)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));
            EnsureArg.IsNotNull(searchItem, nameof(searchItem));

            var returnElements = new List<DicomElement>();

            // base cases
            if (searchItem == null) return returnElements;

            DicomItem item = dataset.GetDicomItem<DicomItem>(searchItem.Tag);
            if (item == null) return returnElements;

            if (item is DicomSequence)
            {
                DicomSequence sequence = (DicomSequence)item;

                var firstChild = sequence.Items.FirstOrDefault()?.FirstOrDefault();
                if (firstChild == null) return returnElements;

                returnElements.AddRange(sequence.Items.Select(x => x.GetLastPathElements(firstChild)).SelectMany(x => x));
            }
            else if (item is DicomElement)
            {
                returnElements.Add((DicomElement)item);
            }

            return returnElements;
        }
    }
}
