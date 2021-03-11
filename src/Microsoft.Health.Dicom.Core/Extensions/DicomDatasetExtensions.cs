// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="DicomDataset"/>.
    /// </summary>
    public static class DicomDatasetExtensions
    {
        private static readonly HashSet<DicomVR> DicomBulkDataVr = new HashSet<DicomVR>
        {
            DicomVR.OB,
            DicomVR.OD,
            DicomVR.OF,
            DicomVR.OL,
            DicomVR.OV,
            DicomVR.OW,
            DicomVR.UN,
        };

        /// <summary>
        /// Gets a single value if the value exists; otherwise the default value for the type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="dicomDataset">The dataset to get the VR value from.</param>
        /// <param name="dicomTag">The DICOM tag.</param>
        /// <returns>The value if the value exists; otherwise, the default value for the type <typeparamref name="T"/>.</returns>
        public static T GetSingleValueOrDefault<T>(this DicomDataset dicomDataset, DicomTag dicomTag)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            return dicomDataset.GetSingleValueOrDefault<T>(dicomTag, default);
        }

        /// <summary>
        /// Get the AT VR value as <see cref="long"/>.
        /// </summary>
        /// <param name="dicomDataset">The dicom dataset.</param>
        /// <param name="dicomTag">The dicom tag.</param>
        /// <returns>The value.</returns>
        public static long? GetAttributeTagValueAsLong(this DicomDataset dicomDataset, DicomTag dicomTag)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            DicomTag tag = dicomDataset.GetSingleValueOrDefault<DicomTag>(dicomTag, default);
            if (tag == null)
            {
                return null;
            }

            return ((long)tag.Group << 16) + tag.Element;
        }

        /// <summary>
        /// Gets the DA VR value as <see cref="DateTime"/>.
        /// </summary>
        /// <param name="dicomDataset">The dataset to get the VR value from.</param>
        /// <param name="dicomTag">The DICOM tag.</param>
        /// <returns>An instance of <see cref="DateTime"/> if the value exists and comforms to the DA format; otherwise <c>null</c>.</returns>
        public static DateTime? GetStringDateAsDate(this DicomDataset dicomDataset, DicomTag dicomTag)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            string stringDate = dicomDataset.GetSingleValueOrDefault<string>(dicomTag, default);
            return DicomElementMinimumValidation.TryParseDA(stringDate, out DateTime result) ? result : null;
        }

        /// <summary>
        /// Gets the DT VR value as <see cref="DateTime"/>.
        /// </summary>
        /// <param name="dicomDataset">The dataset to get the VR value from.</param>
        /// <param name="dicomTag">The DICOM tag.</param>
        /// <returns>An instance of <see cref="DateTime"/> if the value exists and comforms to the DA format; otherwise <c>null</c>.</returns>
        public static DateTime? GetStringDateAsDateTime(this DicomDataset dicomDataset, DicomTag dicomTag)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            string stringDate = dicomDataset.GetSingleValueOrDefault<string>(dicomTag, default);
            return DicomElementMinimumValidation.TryParseDT(stringDate, out DateTime result) ? result : null;
        }

        /// <summary>
        /// Creates a new copy of DICOM dataset with items of VR types considered to be bulk data removed.
        /// </summary>
        /// <param name="dicomDataset">The DICOM dataset.</param>
        /// <returns>A copy of the <paramref name="dicomDataset"/> with items of VR types considered  to be bulk data removed.</returns>
        public static DicomDataset CopyWithoutBulkDataItems(this DicomDataset dicomDataset)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            return CopyDicomDatasetWithoutBulkDataItems(dicomDataset);

            DicomDataset CopyDicomDatasetWithoutBulkDataItems(DicomDataset dicomDatasetToCopy)
            {
                return new DicomDataset(dicomDatasetToCopy
                    .Select(dicomItem =>
                    {
                        if (DicomBulkDataVr.Contains(dicomItem.ValueRepresentation))
                        {
                            // If the VR is bulk data type, return null so it can be filtered out later.
                            return null;
                        }
                        else if (dicomItem.ValueRepresentation == DicomVR.SQ)
                        {
                            // If the VR is sequence, then process each item within the sequence.
                            DicomSequence sequenceToCopy = (DicomSequence)dicomItem;

                            return new DicomSequence(
                                sequenceToCopy.Tag,
                                sequenceToCopy.Select(itemToCopy => itemToCopy.CopyWithoutBulkDataItems()).ToArray());
                        }
                        else
                        {
                            // The VR is not bulk data, return it.
                            return dicomItem;
                        }
                    })
                    .Where(dicomItem => dicomItem != null));
            }
        }

        /// <summary>
        /// Creates an instance of <see cref="InstanceIdentifier"/> from <see cref="DicomDataset"/>.
        /// </summary>
        /// <param name="dicomDataset">The DICOM dataset to get the identifiers from.</param>
        /// <returns>An instance of <see cref="InstanceIdentifier"/> representing the <paramref name="dicomDataset"/>.</returns>
        public static InstanceIdentifier ToInstanceIdentifier(this DicomDataset dicomDataset)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            // Note: Here we 'GetSingleValueOrDefault' and let the constructor validate the identifier.
            return new InstanceIdentifier(
                dicomDataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
                dicomDataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
                dicomDataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty));
        }

        /// <summary>
        /// Creates an instance of <see cref="VersionedInstanceIdentifier"/> from <see cref="DicomDataset"/>.
        /// </summary>
        /// <param name="dicomDataset">The DICOM dataset to get the identifiers from.</param>
        /// <param name="version">The version.</param>
        /// <returns>An instance of <see cref="InstanceIdentifier"/> representing the <paramref name="dicomDataset"/>.</returns>
        public static VersionedInstanceIdentifier ToVersionedInstanceIdentifier(this DicomDataset dicomDataset, long version)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            // Note: Here we 'GetSingleValueOrDefault' and let the constructor validate the identifier.
            return new VersionedInstanceIdentifier(
                dicomDataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
                dicomDataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
                dicomDataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty),
                version);
        }

        /// <summary>
        /// Adds value to the <paramref name="dicomDataset"/> if <paramref name="value"/> is not null.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="dicomDataset">The dataset to add value to.</param>
        /// <param name="dicomTag">The DICOM tag.</param>
        /// <param name="value">The value to add.</param>
        public static void AddValueIfNotNull<T>(this DicomDataset dicomDataset, DicomTag dicomTag, T value)
            where T : class
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(dicomTag, nameof(dicomTag));

            if (value != null)
            {
                dicomDataset.Add(dicomTag, value);
            }
        }

        /// <summary>
        /// Get matching DicomTags for index Tag from Dicom Dataset.
        /// </summary>
        /// <remarks>If indextag not exist in dataset, should not return.</remarks>
        /// <param name="dicomDataset">The dicom dataset.</param>
        /// <param name="indexTags">The index Tags.</param>
        /// <returns>Mapping between IndexTag and DicomTag.</returns>
        public static IDictionary<IndexTag, DicomTag> GetMatchingDicomTags(this DicomDataset dicomDataset, IEnumerable<IndexTag> indexTags)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(indexTags, nameof(indexTags));
            IDictionary<IndexTag, DicomTag> result = new Dictionary<IndexTag, DicomTag>();
            Dictionary<string, IndexTag> privateTags = new Dictionary<string, IndexTag>();
            foreach (IndexTag indexTag in indexTags)
            {
                if (indexTag.Tag.IsPrivate)
                {
                    privateTags.Add(indexTag.Tag.GetPath(), indexTag);
                }
                else
                {
                    if (dicomDataset.Contains(indexTag.Tag))
                    {
                        result.Add(indexTag, indexTag.Tag);
                    }
                }
            }

            // Process Private tags
            if (privateTags.Count != 0)
            {
                // IndexTag don't have privateCreator for private tag, need to fill that part from DicomDataset.
                foreach (DicomItem item in dicomDataset)
                {
                    if (item.Tag.IsPrivate)
                    {
                        string tagPath = item.Tag.GetPath();
                        if (privateTags.ContainsKey(tagPath))
                        {
                            IndexTag indexTag = privateTags[tagPath];
                            if (indexTag.VR == item.ValueRepresentation)
                            {
                                result.Add(indexTag, item.Tag);
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}
