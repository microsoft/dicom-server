// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
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

        private const string DateFormatDA = "yyyyMMdd";

        /// <summary>
        /// Gets a single value if the value exists; otherwise the default value for the type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="dicomDataset">The dataset to get the VR value from.</param>
        /// <param name="dicomTag">The DICOM tag.</param>
        /// <param name="expectedVR">Expected VR of the element.</param>
        /// <remarks>If expectedVR is provided, and not match, will return default<typeparamref name="T"/></remarks>
        /// <returns>The value if the value exists; otherwise, the default value for the type <typeparamref name="T"/>.</returns>
        public static T GetSingleValueOrDefault<T>(this DicomDataset dicomDataset, DicomTag dicomTag, DicomVR expectedVR = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            // If VR doesn't match, return default(T)
            if (expectedVR != null && dicomDataset.GetDicomItem<DicomElement>(dicomTag)?.ValueRepresentation != expectedVR)
            {
                return default;
            }

            return dicomDataset.GetSingleValueOrDefault<T>(dicomTag, default);
        }

        /// <summary>
        /// Gets the DA VR value as <see cref="DateTime"/>.
        /// </summary>
        /// <param name="dicomDataset">The dataset to get the VR value from.</param>
        /// <param name="dicomTag">The DICOM tag.</param>
        /// <param name="expectedVR">Expected VR of the element.</param>
        /// /// <remarks>If expectedVR is provided, and not match, will return null.</remarks>
        /// <returns>An instance of <see cref="DateTime"/> if the value exists and comforms to the DA format; otherwise <c>null</c>.</returns>
        public static DateTime? GetStringDateAsDate(this DicomDataset dicomDataset, DicomTag dicomTag, DicomVR expectedVR = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            string stringDate = dicomDataset.GetSingleValueOrDefault<string>(dicomTag, expectedVR: expectedVR);
            return DateTime.TryParseExact(stringDate, DateFormatDA, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result) ? result : null;
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

            static DicomDataset CopyDicomDatasetWithoutBulkDataItems(DicomDataset dicomDatasetToCopy)
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
        /// Validate query tag in Dicom dataset.
        /// </summary>
        /// <param name="dataset">The dicom dataset.</param>
        /// <param name="queryTag">The query tag.</param>
        /// <param name="minimumValidator">The minimum validator.</param>
        public static void ValidateQueryTag(this DicomDataset dataset, QueryTag queryTag, IElementMinimumValidator minimumValidator)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));
            EnsureArg.IsNotNull(queryTag, nameof(queryTag));
            EnsureArg.IsNotNull(minimumValidator, nameof(minimumValidator));
            DicomElement dicomElement = dataset.GetDicomItem<DicomElement>(queryTag.Tag);

            if (dicomElement != null)
            {
                if (dicomElement.ValueRepresentation != queryTag.VR)
                {

                    throw new DicomElementValidationException(
                            queryTag.Tag.GetFriendlyName(),
                            queryTag.VR,
                            string.Format(
                                CultureInfo.InvariantCulture,
                                DicomCoreResource.MismatchVR,
                                queryTag.Tag,
                                queryTag.VR,
                                dicomElement.ValueRepresentation));

                }

                minimumValidator.Validate(dicomElement);

            }
        }
    }
}
