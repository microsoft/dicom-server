// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Hl7.Fhir.Model;

namespace Microsoft.Health.DicomCast.Core.Features.Fhir
{
    /// <summary>
    /// Utility for creating an identifier for <see cref="ImagingStudy"/>.
    /// </summary>
    public static class IdentifierUtility
    {
        private const string DicomIdentifierSystem = "urn:dicom:uid";

        /// <summary>
        /// Creates an <see cref="Identifier"/> that represents the study specified by <paramref name="studyInstanceUid"/>.
        /// </summary>
        /// <remarks>
        /// The identifier is generated based on the rules specified by https://www.hl7.org/fhir/imagingstudy.html#notes.
        /// </remarks>
        /// <param name="studyInstanceUid">The study instance UID.</param>
        /// <returns>The <see cref="Identifier"/> that represents this study.</returns>
        public static Identifier CreateIdentifier(string studyInstanceUid)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));

            return new Identifier(DicomIdentifierSystem, $"urn:oid:{studyInstanceUid}");
        }
    }
}
