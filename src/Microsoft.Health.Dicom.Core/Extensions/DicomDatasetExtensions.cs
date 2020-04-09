// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features;

namespace Microsoft.Health.Dicom.Core.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="DicomDataset"/>.
    /// </summary>
    public static class DicomDatasetExtensions
    {
        private const string DateFormat = "yyyyMMdd";

        private static readonly HashSet<DicomVR> DicomBulkDataVr = new HashSet<DicomVR>
        {
            DicomVR.OB,
            DicomVR.OD,
            DicomVR.OF,
            DicomVR.OL,
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
        /// Gets the DA VR value as <see cref="DateTime"/>.
        /// </summary>
        /// <param name="dicomDataset">The dataset to get the VR value from.</param>
        /// <param name="dicomTag">The DICOM tag.</param>
        /// <returns>An instance of <see cref="DateTime"/> if the value exists and comforms to the DA format; otherwise <c>null</c>.</returns>
        public static DateTime? GetStringDateAsDateTime(this DicomDataset dicomDataset, DicomTag dicomTag)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            string stringDate = dicomDataset.GetSingleValueOrDefault<string>(dicomTag, default);

            if (stringDate == null ||
                !DateTime.TryParseExact(stringDate, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime result))
            {
                return null;
            }

            return result;
        }

        /// <summary>
        /// Removes DICOM elements who's VR types are marked as Bulk in the DicomBulkDataVr enum.
        /// </summary>
        /// <param name="dicomDataset">Dataset that has bulk metadata</param>
        public static void RemoveBulkDataVrs(this DicomDataset dicomDataset)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            var tagsToRemove = new List<DicomTag>();
            foreach (DicomItem item in dicomDataset)
            {
                if (item.ValueRepresentation == DicomVR.SQ && item is DicomSequence sequence)
                {
                    foreach (DicomDataset sequenceDataset in sequence.Items)
                    {
                        RemoveBulkDataVrs(sequenceDataset);
                    }
                }
                else if (DicomBulkDataVr.Contains(item.ValueRepresentation))
                {
                    tagsToRemove.Add(item.Tag);
                }
            }

            dicomDataset.Remove(tagsToRemove.ToArray());
        }

        /// <summary>
        /// Creates an instance of <see cref="DicomInstanceIdentifier"/> from <see cref="DicomDataset"/>.
        /// </summary>
        /// <param name="dicomDataset">The DICOM dataset to get the identifiers from.</param>
        /// <returns>An instance of <see cref="DicomInstanceIdentifier"/> representing the <paramref name="dicomDataset"/>.</returns>
        public static DicomInstanceIdentifier ToDicomInstanceIdentifier(this DicomDataset dicomDataset)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            // Note: Here we 'GetSingleValueOrDefault' and let the constructor validate the identifier.
            return new DicomInstanceIdentifier(
                dicomDataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
                dicomDataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
                dicomDataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty));
        }

        /// <summary>
        /// Creates an instance of <see cref="VersionedDicomInstanceIdentifier"/> from <see cref="DicomDataset"/>.
        /// </summary>
        /// <param name="dicomDataset">The DICOM dataset to get the identifiers from.</param>
        /// <param name="version">The version.</param>
        /// <returns>An instance of <see cref="DicomInstanceIdentifier"/> representing the <paramref name="dicomDataset"/>.</returns>
        public static VersionedDicomInstanceIdentifier ToVersionedDicomInstanceIdentifier(this DicomDataset dicomDataset, long version)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            // Note: Here we 'GetSingleValueOrDefault' and let the constructor validate the identifier.
            return new VersionedDicomInstanceIdentifier(
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
    }
}
