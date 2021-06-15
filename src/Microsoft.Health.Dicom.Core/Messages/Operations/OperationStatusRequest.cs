// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.Operations
{
    /// <summary>
    /// Represents a request for the status of long-running DICOM operations.
    /// </summary>
    public class OperationStatusRequest : IRequest<OperationStatusResponse>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OperationStatusRequest"/> class.
        /// </summary>
        /// <param name="operationId">The unique ID for a particular DICOM operation.</param>
        /// <exception cref="ArgumentException"><paramref name="operationId"/> consists of white space characters.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="operationId"/> is <see langword="null"/>.</exception>
        public OperationStatusRequest(string operationId)
        {
            EnsureArg.IsNotNullOrWhiteSpace(operationId, nameof(operationId));
            OperationId = operationId;
        }

        /// <summary>
        /// Gets the operation ID.
        /// </summary>
        /// <value>The unique ID that denotes a particular operation.</value>
        public string OperationId { get; }
    }
}

