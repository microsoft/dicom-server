// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Models.Operations;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// Represent an extended query tag entry has retrieved from the store that has been
    /// joined with its corresponding optional operation.
    /// </summary>
    public class ExtendedQueryTagStoreJoinEntry : ExtendedQueryTagStoreEntry
    {
        public ExtendedQueryTagStoreJoinEntry(ExtendedQueryTagStoreEntry storeEntry, Guid? operationId = null)
            : base(
                EnsureArg.IsNotNull(storeEntry, nameof(storeEntry)).Key,
                storeEntry.Path,
                storeEntry.VR,
                storeEntry.PrivateCreator,
                storeEntry.Level,
                storeEntry.Status,
                storeEntry.QueryStatus,
                storeEntry.ErrorCount)
        {
            OperationId = operationId;
        }

        public ExtendedQueryTagStoreJoinEntry(
            int key,
            string path,
            string vr,
            string privateCreator,
            QueryTagLevel level,
            ExtendedQueryTagStatus status,
            QueryStatus queryStatus,
            int errorCount,
            Guid? operationId = null)
            : base(key, path, vr, privateCreator, level, status, queryStatus, errorCount)
        {
            OperationId = operationId;
        }

        /// <summary>
        /// The optional ID for the long-running operation acted upon the tag.
        /// </summary>
        public Guid? OperationId { get; }

        /// <summary>
        /// Convert to  <see cref="GetExtendedQueryTagEntry"/>.
        /// </summary>
        /// <param name="resolver">An optional <see cref="IUrlResolver"/> for resolving resource paths.</param>
        /// <returns>The extended query tag entry.</returns>
        public GetExtendedQueryTagEntry ToGetExtendedQueryTagEntry(IUrlResolver resolver = null)
        {
            return new GetExtendedQueryTagEntry
            {
                Path = Path,
                VR = VR,
                PrivateCreator = PrivateCreator,
                Level = Level,
                Status = Status,
                Errors = ErrorCount > 0 && resolver != null
                    ? new ExtendedQueryTagErrorReference(ErrorCount, resolver.ResolveQueryTagErrorsUri(Path))
                    : null,
                Operation = OperationId.HasValue && resolver != null
                    ? new OperationReference(OperationId.GetValueOrDefault(), resolver.ResolveOperationStatusUri(OperationId.GetValueOrDefault()))
                    : null,
                QueryStatus = QueryStatus
            };
        }
    }
}
