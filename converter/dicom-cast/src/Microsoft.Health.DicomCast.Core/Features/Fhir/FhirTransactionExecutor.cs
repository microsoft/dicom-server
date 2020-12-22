// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Exceptions;
using Microsoft.Health.Fhir.Client;

namespace Microsoft.Health.DicomCast.Core.Features.Fhir
{
    /// <summary>
    /// Provides functionality to execute a transaction in a FHIR server.
    /// </summary>
    public class FhirTransactionExecutor : IFhirTransactionExecutor
    {
        private readonly IFhirClient _fhirClient;

        public FhirTransactionExecutor(IFhirClient fhirClient)
        {
            EnsureArg.IsNotNull(fhirClient, nameof(fhirClient));

            _fhirClient = fhirClient;
        }

        /// <inheritdoc/>
        public async Task<Bundle> ExecuteTransactionAsync(Bundle bundle, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(bundle, nameof(bundle));

            Bundle responseBundle;

            try
            {
                responseBundle = await _fhirClient.PostBundleAsync(bundle, cancellationToken);
            }
            catch (FhirException ex)
            {
                if (ex.StatusCode == HttpStatusCode.PreconditionFailed)
                {
                    // The request failed because the resource was updated by some external process.
                    throw new ResourceConflictException();
                }
                else if (ex.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    // The request failed because the server was too busy.
                    throw new ServerTooBusyException();
                }

                throw new TransactionFailedException(ex.OperationOutcome, ex);
            }

            responseBundle = null;

            if (responseBundle == null)
            {
                throw new InvalidFhirResponseException(DicomCastCoreResource.MissingResponseBundle);
            }

            if (responseBundle.Entry?.Count != bundle.Entry.Count)
            {
                throw new InvalidFhirResponseException(DicomCastCoreResource.MismatchBundleEntryCount);
            }

            for (int index = 0; index < responseBundle.Entry.Count; index++)
            {
                Bundle.EntryComponent entry = responseBundle.Entry[index];

                HttpStatusCode statusCode = ValidateBundleEntryAndGetStatusCode(entry, index);

                // Cache the parsed status code.
                entry.Response.AddAnnotation(statusCode);
            }

            return responseBundle;
        }

        private static HttpStatusCode ValidateBundleEntryAndGetStatusCode(Bundle.EntryComponent entry, int entryIndex)
        {
            if (entry == null)
            {
                throw new InvalidFhirResponseException(
                    string.Format(CultureInfo.InvariantCulture, DicomCastCoreResource.MissingBundleEntry, entryIndex));
            }

            if (entry.Response == null)
            {
                throw new InvalidFhirResponseException(
                    string.Format(CultureInfo.InvariantCulture, DicomCastCoreResource.MissingBundleEntryResponse, entryIndex));
            }

            if (entry.Response.Status == null)
            {
                throw new InvalidFhirResponseException(
                    string.Format(CultureInfo.InvariantCulture, DicomCastCoreResource.MissingBundleEntryResponseStatus, entryIndex));
            }

            ReadOnlySpan<char> statusSpan = entry.Response.Status.AsSpan();

            // Based on the spec (http://hl7.org/fhir/R4/bundle-definitions.html#Bundle.entry.response.status),
            // the status should be starting with 3 digit HTTP code and may contain the HTTP description associated
            // with the status code.
            if (statusSpan.Length < 3 ||
                (statusSpan.Length > 3 && statusSpan[3] != ' ') ||
                !Enum.TryParse(statusSpan.Slice(0, 3).ToString(), out HttpStatusCode parsedStatusCode))
            {
                throw new InvalidFhirResponseException(
                    string.Format(CultureInfo.InvariantCulture, DicomCastCoreResource.InvalidBundleEntryResponseStatus, entry.Response.Status, entryIndex));
            }

            if ((int)parsedStatusCode < 200 || (int)parsedStatusCode >= 300)
            {
                throw new InvalidFhirResponseException(
                    string.Format(CultureInfo.InvariantCulture, DicomCastCoreResource.MismatchTransactionStatusCode, entry.Response.Status, entryIndex));
            }

            return parsedStatusCode;
        }
    }
}
