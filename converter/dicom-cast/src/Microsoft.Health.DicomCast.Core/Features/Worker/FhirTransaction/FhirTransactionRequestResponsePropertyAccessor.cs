// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Provides delegate to getting the <see cref="FhirTransactionRequestEntry"/> and setting the <see cref="FhirTransactionResponseEntry"/> for a given property.
    /// </summary>
    public struct FhirTransactionRequestResponsePropertyAccessor : IEquatable<FhirTransactionRequestResponsePropertyAccessor>
    {
        public FhirTransactionRequestResponsePropertyAccessor(
            string propertyName,
            Func<FhirTransactionRequest, FhirTransactionRequestEntry> requestEntryGetter,
            Action<FhirTransactionResponse, FhirTransactionResponseEntry> responseEntrySetter)
        {
            EnsureArg.IsNotNullOrWhiteSpace(propertyName, nameof(propertyName));
            EnsureArg.IsNotNull(requestEntryGetter, nameof(requestEntryGetter));
            EnsureArg.IsNotNull(responseEntrySetter, nameof(responseEntrySetter));

            PropertyName = propertyName;
            RequestEntryGetter = requestEntryGetter;
            ResponseEntrySetter = responseEntrySetter;
        }

        /// <summary>
        /// Gets the property name.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Gets the property getter for <see cref="FhirTransactionRequestEntry"/>.
        /// </summary>
        public Func<FhirTransactionRequest, FhirTransactionRequestEntry> RequestEntryGetter { get; }

        /// <summary>
        /// Gets the property setter for <see cref="FhirTransactionResponseEntry"/>.
        /// </summary>
        public Action<FhirTransactionResponse, FhirTransactionResponseEntry> ResponseEntrySetter { get; }

        public static bool operator ==(FhirTransactionRequestResponsePropertyAccessor left, FhirTransactionRequestResponsePropertyAccessor right)
        {
            if (ReferenceEquals(left, default))
            {
                if (ReferenceEquals(right, default))
                {
                    return true;
                }

                return false;
            }

            return left.Equals(right);
        }

        public static bool operator !=(FhirTransactionRequestResponsePropertyAccessor left, FhirTransactionRequestResponsePropertyAccessor right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is FhirTransactionRequestResponsePropertyAccessor other))
            {
                return false;
            }

            return Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PropertyName, RequestEntryGetter.Method, ResponseEntrySetter.Method);
        }

        public bool Equals(FhirTransactionRequestResponsePropertyAccessor other)
        {
            if (ReferenceEquals(other, default))
            {
                return false;
            }
            else if (ReferenceEquals(this, other))
            {
                return true;
            }
            else if (GetType() != other.GetType())
            {
                return false;
            }
            else
            {
                return string.Equals(PropertyName, other.PropertyName, StringComparison.Ordinal) &&
                    Equals(RequestEntryGetter, other.RequestEntryGetter) &&
                    Equals(ResponseEntrySetter, other.ResponseEntrySetter);
            }
        }
    }
}
