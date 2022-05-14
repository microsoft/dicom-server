// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.Dicom.Client.Models;

/// <summary>
/// Represents an identifier for a DICOM study, series, or SOP instance.
/// </summary>
public sealed class DicomIdentifier : IEquatable<DicomIdentifier>
{
    /// <summary>
    /// Gets the unique identifier for the study.
    /// </summary>
    /// <value>The value of the study instance UID attribute (0020,000D).</value>
    public string StudyInstanceUid { get; }

    /// <summary>
    /// Gets the unique identifier for the series.
    /// </summary>
    /// <remarks>
    /// May be <see langword="null"/> for studies.
    /// </remarks>
    /// <value>The value of the series instance UID attribute (0020,000E).</value>
    public string SeriesInstanceUid { get; }

    /// <summary>
    /// Gets the unique identifier for the SOP instance.
    /// </summary>
    /// <remarks>
    /// May be <see langword="null"/> for studies or series.
    /// </remarks>
    /// <value>The value of the SOP instance UID attribute (0008,0018).</value>
    public string SopInstanceUid { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DicomIdentifier"/> structure based on the given identifiers.
    /// </summary>
    /// <param name="studyInstanceUid">The unique identifier for the study.</param>
    /// <param name="seriesInstanceUid">The optional unique identifier for the series.</param>
    /// <param name="sopInstanceUid">The optional unique identifier for the SOP instance.</param>
    /// <exception cref="ArgumentNullException">
    /// <para><paramref name="studyInstanceUid"/> is <see langword="null"/> or white space</para>
    /// <para>-or-</para>
    /// <para>
    /// <paramref name="seriesInstanceUid"/> is <see langword="null"/> or white space, but
    /// <paramref name="sopInstanceUid"/> has a value.
    /// </para>
    /// </exception>
    public DicomIdentifier(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
    {
        StudyInstanceUid = EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
        if (string.IsNullOrWhiteSpace(seriesInstanceUid) && !string.IsNullOrWhiteSpace(sopInstanceUid))
            throw new ArgumentNullException(nameof(seriesInstanceUid));

        SeriesInstanceUid = seriesInstanceUid;
        SopInstanceUid = sopInstanceUid;
    }

    /// <summary>
    /// Returns a value indicating whether this instance is equal to a specified object.
    /// </summary>
    /// <param name="obj">The object to compare to this instance.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="obj"/> is an instance of <see cref="DicomIdentifier"/> and
    /// equals the value of this instance; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool Equals(object obj)
        => obj is DicomIdentifier other && Equals(other);

    /// <summary>
    /// Returns a value indicating whether the value of this instance is equal to the value of the
    /// specified <see cref="DicomIdentifier"/> instance.
    /// </summary>
    /// <param name="other">The object to compare to this instance.</param>
    /// <returns>
    /// <see langword="true"/> if the <paramref name="other"/> parameter equals the
    /// value of this instance; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(DicomIdentifier other)
        => other != null
            && string.Equals(StudyInstanceUid, other.StudyInstanceUid, StringComparison.Ordinal)
            && string.Equals(SeriesInstanceUid, other.SeriesInstanceUid, StringComparison.Ordinal)
            && string.Equals(SopInstanceUid, other.SopInstanceUid, StringComparison.Ordinal);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode()
        => HashCode.Combine(StudyInstanceUid, SeriesInstanceUid, SopInstanceUid);

    /// <summary>
    /// Converts the value of the current <see cref="DicomIdentifier"/> structure to its equivalent string representation.
    /// </summary>
    /// <returns>A string representation of the value of the current <see cref="DicomIdentifier"/> structure.</returns>
    public override string ToString()
    {
        if (SopInstanceUid != null)
            return StudyInstanceUid + '/' + SeriesInstanceUid + '/' + SopInstanceUid;
        else if (SeriesInstanceUid != null)
            return StudyInstanceUid + '/' + SeriesInstanceUid;
        else
            return StudyInstanceUid;
    }

    /// <summary>
    /// Creates a new <see cref="DicomIdentifier"/> structure for the given study.
    /// </summary>
    /// <param name="uid">The unique identifier for the study.</param>
    /// <returns>A <see cref="DicomIdentifier"/> structure for the given study.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="uid"/> is <see langword="null"/>.</exception>
    public static DicomIdentifier ForStudy(string uid)
        => new DicomIdentifier(uid, null, null);

    /// <summary>
    /// Creates a new <see cref="DicomIdentifier"/> structure for the given series.
    /// </summary>
    /// <param name="studyInstanceUid">The unique identifier for the study.</param>
    /// <param name="seriesInstanceUid">The unique identifier for the series.</param>
    /// <returns>A <see cref="DicomIdentifier"/> structure for the given series.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="studyInstanceUid"/> or <paramref name="seriesInstanceUid"/> is <see langword="null"/>.
    /// </exception>
    public static DicomIdentifier ForSeries(string studyInstanceUid, string seriesInstanceUid)
        => new DicomIdentifier(studyInstanceUid, EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid)), null);

    /// <summary>
    /// Creates a new <see cref="DicomIdentifier"/> structure for the given SOP instance.
    /// </summary>
    /// <param name="studyInstanceUid">The unique identifier for the study.</param>
    /// <param name="seriesInstanceUid">The unique identifier for the series.</param>
    /// <param name="sopInstanceUid">The unique identifier for the SOP instance.</param>
    /// <returns>A <see cref="DicomIdentifier"/> structure for the given SOP instance.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="studyInstanceUid"/>, <paramref name="seriesInstanceUid"/>, <paramref name="sopInstanceUid"/>
    /// is <see langword="null"/>.
    /// </exception>
    public static DicomIdentifier ForInstance(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        => new DicomIdentifier(
            studyInstanceUid,
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid)),
            EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUid, nameof(sopInstanceUid)));
}
