// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Models.Operations
{
    /// <summary>
    /// Represents the common metadata for a long-running DICOM operation.
    /// </summary>
    public class CoreOperationStatus
    {
        /// <summary>
        /// Gets or sets the operation ID.
        /// </summary>
        /// <value>The unique ID that denotes a particular operation.</value>
        public Guid OperationId { get; set; }

        /// <summary>
        /// Gets or sets the category of the operation.
        /// </summary>
        /// <value>
        /// The <see cref="OperationType"/> if recognized; otherwise <see cref="OperationType.Unknown"/>.
        /// </value>
        public OperationType Type { get; set; }

        /// <summary>
        /// Gets or sets the date and time the operation was started.
        /// </summary>
        /// <value>The <see cref="DateTime"/> when the operation was started.</value>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// Gets or sets the last date and time the operation's execution status was updated.
        /// </summary>
        /// <value>The last <see cref="DateTime"/> when the operation status was updated.</value>
        public DateTime LastUpdatedTime { get; set; }

        /// <summary>
        /// Gets or sets the execution status of the operation.
        /// </summary>
        /// <value>
        /// The <see cref="OperationRuntimeStatus"/> if recognized; otherwise <see cref="OperationType.Unknown"/>.
        /// </value>
        public OperationRuntimeStatus Status { get; set; }

        /// <summary>
        /// Gets the percentage of work that has been completed by the operation.
        /// </summary>
        /// <value>An integer ranging from 0 to 100.</value>
        public int PercentComplete { get; set; }
    }
}
