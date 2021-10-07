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

        private const string DateTimeOffsetFormat = "hhmm";

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
        /// <remarks>If expectedVR is provided, and not match, will return null.</remarks>
        /// <returns>An instance of <see cref="DateTime"/> if the value exists and comforms to the DA format; otherwise <c>null</c>.</returns>
        public static DateTime? GetStringDateAsDate(this DicomDataset dicomDataset, DicomTag dicomTag, DicomVR expectedVR = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            string stringDate = dicomDataset.GetSingleValueOrDefault<string>(dicomTag, expectedVR: expectedVR);
            return DateTime.TryParseExact(stringDate, DateFormatDA, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result) ? result : null;
        }

        /// <summary>
        /// Gets the DT VR values as local <see cref="DateTime"/> and UTC <see cref="DateTime"/>.
        /// </summary>
        /// <param name="dicomDataset">The dataset to get the VR value from.</param>
        /// <param name="dicomTag">The DICOM tag.</param>
        /// <param name="expectedVR">Expected VR of the element.</param>
        /// <remarks>If expectedVR is provided, and not match, will return null.</remarks>
        /// <returns>A <see cref="Tuple{T1, T2}"/> of (<see cref="Nullable{T}"/> <see cref="DateTime"/>, <see cref="Nullable{T}"/> <see cref="DateTime"/>) representing local date time and utc date time respectively is returned if the value exists and conforms to the DT format; othewise <c>null</c> is returned if value is empty; or <c>null</c> for each of the DateTimes if value is provided.</returns>
        public static Tuple<DateTime?, DateTime?> GetStringDateTimeAsLocalAndUtcDateTimes(this DicomDataset dicomDataset, DicomTag dicomTag, DicomVR expectedVR = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            string stringDateTime = dicomDataset.GetSingleValueOrDefault<string>(dicomTag, expectedVR: expectedVR);

            if (string.IsNullOrEmpty(stringDateTime))
            {
                return null;
            }

            DateTime? localDateTime = GetStringDateTimeAsLocalDateTime(dicomTag, stringDateTime);
            DateTime? utcDateTime = GetStringDateTimeAsUtcDateTime(dicomDataset, dicomTag, stringDateTime);

            return new Tuple<DateTime?, DateTime?>(localDateTime, utcDateTime);
        }

        /// <summary>
        /// Gets the UTC DT VR value as <see cref="DateTime"/>.
        /// If offset is present in the DT value, value is considered to be in UTC timezone.
        /// If offset is not present, DT value is considered to be in local.
        ///     If TimezoneOffsetFromUTC is present, this offset value is used to convert local timezone to UTC.
        ///     Else, local value is returned as the UTC value as well.
        /// </summary>
        /// <param name="dicomDataset">The dataset to get the VR value from.</param>
        /// <param name="dicomTag">The DICOM tag.</param>
        /// <param name="stringDateTime">String date time value.</param>
        /// <remarks>If expectedVR is provided, and not match, will return null.</remarks>
        /// <returns>An instance of <see cref="DateTime"/> containing UTC date time if the value exists and conforms to the DT format; othewise <c>null</c>.</returns>
        private static DateTime? GetStringDateTimeAsUtcDateTime(this DicomDataset dicomDataset, DicomTag dicomTag, string stringDateTime)
        {
            DateTime? result;
            int offsetIndex = stringDateTime.IndexOfAny(new char[] { '-', '+' });

            // If offset is present, time is considered to be present in UTC.
            if (offsetIndex != -1)
            {
                result = GetDateTime(dicomTag, stringDateTime.Substring(0, offsetIndex));
            }
            else
            {
                // If offset is not present, check if TimezoneOffsetFromUTC value is present.
                string offset = dicomDataset.GetSingleValueOrDefault<string>(DicomTag.TimezoneOffsetFromUTC, expectedVR: DicomVR.SH);

                // If timezone offset from UTC information is not found, we store the provided value for UTC as well.
                if (string.IsNullOrEmpty(offset))
                {
                    result = GetDateTime(dicomTag, stringDateTime);
                }
                else
                {
                    // timezone offset from UTC information is found. Need to use this to convert date time from local timezone to UTC.

                    bool isNegativeOffset = offset[0] == '-';

                    // Eliminating the positive or negative sign from the offset.
                    offset = offset.Substring(1);

                    // If valid offset is present, apply that offset to the given datetime value and return the updated DateTime.
                    if (!string.IsNullOrEmpty(offset) && TimeSpan.TryParseExact(offset, DateTimeOffsetFormat, null, out TimeSpan offsetTimeSpan))
                    {
                        // Since converting from local to UTC, offset needs to be applied in the reverse direction.
                        // E.g. if offset is +0300, 3 hours should be subtracted from local to get the UTC date time.
                        result = GetDateTime(dicomTag, stringDateTime);

                        if (result.HasValue)
                        {
                            if (isNegativeOffset)
                            {
                                result = result.Value.Add(offsetTimeSpan);
                            }
                            else
                            {
                                result = result.Value.Subtract(offsetTimeSpan);
                            }
                        }
                    }
                    else
                    {
                        result = GetDateTime(dicomTag, stringDateTime);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the Local DT VR value as <see cref="DateTime"/>.
        /// If offset is not present in the DT value, value is considered to be in local timezone.
        /// If offset is present, DT value is considered to be in UTC and is converted to local timezone by applying the offset.
        /// </summary>
        /// <param name="stringDateTime">String date time value.</param>
        /// <param name="dicomTag">The DICOM Tag.</param>
        /// <remarks>If expectedVR is provided, and not match, will return null.</remarks>
        /// <returns>An instance of <see cref="DateTime"/> containing local datetime value if the value exists and conforms to the DT format; othewise <c>null</c>.</returns>
        private static DateTime? GetStringDateTimeAsLocalDateTime(DicomTag dicomTag, string stringDateTime)
        {
            DateTime? result;
            int offsetIndex = stringDateTime.IndexOfAny(new char[] { '-', '+' });

            // If offset is not present, time is considered to be present in local time.
            if (offsetIndex == -1)
            {
                result = GetDateTime(dicomTag, stringDateTime);
            }
            else
            {
                // If offset is present, the datetime value is considered to be in UTC.
                // Need to convert this date time to local by applying the offset.

                string offset = stringDateTime.Substring(offsetIndex);
                bool isNegativeOffset = offset[0] == '-';

                // Eliminating the positive or negative sign from the offset.
                offset = offset.Substring(1);

                // If valid offset is present, apply that offset to the given datetime value and return the updated DateTime.
                if (!string.IsNullOrEmpty(offset) && TimeSpan.TryParseExact(offset, DateTimeOffsetFormat, null, out TimeSpan offsetTimeSpan))
                {
                    // Since converting from UTC to local, offset will be applied without any modification.
                    result = GetDateTime(dicomTag, stringDateTime.Substring(0, offsetIndex));

                    if (result.HasValue)
                    {
                        if (isNegativeOffset)
                        {
                            result = result.Value.Subtract(offsetTimeSpan);
                        }
                        else
                        {
                            result = result.Value.Add(offsetTimeSpan);
                        }
                    }
                }
                else
                {
                    result = GetDateTime(dicomTag, stringDateTime);
                }
            }

            return result;
        }

        /// <summary>
        /// Get <see cref="DateTime" /> object after parsing the input <c>stringDateTime</c>.
        /// </summary>
        /// <param name="dicomTag">the DICOM tag.</param>
        /// <param name="stringDateTime">String date time value.</param>
        /// <returns>An instance of <see cref="DateTime"/> if the value exists and conforms to the DT format; otherwise <c>null</c>.</returns>
        private static DateTime? GetDateTime(DicomTag dicomTag, string stringDateTime)
        {
            DateTime? result;

            try
            {
                DicomDateTime dicomDateTime = new DicomDateTime(dicomTag, new string[] { stringDateTime });
                result = dicomDateTime.Get<DateTime>();
            }
            catch (Exception)
            {
                result = null;
            }

            return result;
        }

        /// <summary>
        /// Gets the TM VR value as <see cref="long"/>.
        /// </summary>
        /// <param name="dicomDataset">The dataset to get the VR value from.</param>
        /// <param name="dicomTag">The DICOM tag.</param>
        /// <param name="expectedVR">Expected VR of the element.</param>
        /// <remarks>If expectedVR is provided, and not match, will return null.</remarks>
        /// <returns>A long value representing the ticks if the value exists and conforms to the TM format; othewise <c>null</c>.</returns>
        public static long? GetStringTimeAsLong(this DicomDataset dicomDataset, DicomTag dicomTag, DicomVR expectedVR = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            string stringTime = dicomDataset.GetSingleValueOrDefault<string>(dicomTag, expectedVR: expectedVR);

            long? result;
            try
            {
                DicomTime dicomTime = new DicomTime(dicomTag, new string[] { stringTime });
                result = dicomTime.Get<DateTime>().Ticks;
            }
            catch (Exception)
            {
                result = null;
            }

            return result;
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
                    throw ElementValidationExceptionFactory.CreateUnexpectedVRException(dicomElement.Tag.GetFriendlyName(), dicomElement.ValueRepresentation, queryTag.VR);
                }

                minimumValidator.Validate(dicomElement);
            }
        }
    }
}
