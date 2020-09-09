// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Health.Dicom.Client.Models;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Provides the context of a transaction.
    /// </summary>
    public class FhirTransactionContext : IFhirTransactionContext
    {
        public FhirTransactionContext(ChangeFeedEntry changeFeedEntry)
        {
            EnsureArg.IsNotNull(changeFeedEntry, nameof(changeFeedEntry));

            ChangeFeedEntry = changeFeedEntry;

            Request = new FhirTransactionRequest();
            Response = new FhirTransactionResponse();
        }

        /// <summary>
        /// Gets the change feed used for this transaction.
        /// </summary>
        public ChangeFeedEntry ChangeFeedEntry { get; }

        /// <summary>
        /// Gets the request.
        /// </summary>
        public FhirTransactionRequest Request { get; }

        /// <summary>
        /// Gets the response.
        /// </summary>
        public FhirTransactionResponse Response { get; }

        /// <summary>
        /// Gets or sets the utcDatetimeOffset for changefeedEntry dataset for this transaction.
        /// </summary>
        public TimeSpan UtcDateTimeOffset { get; set; }
    }
}
