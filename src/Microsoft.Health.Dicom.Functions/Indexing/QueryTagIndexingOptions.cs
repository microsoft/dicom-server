// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Health.Dicom.Functions.Durable;

namespace Microsoft.Health.Dicom.Functions.Indexing
{
    /// <summary>
    /// Represents the options for a "re-index" function.
    /// </summary>
    public class QueryTagIndexingOptions
    {
        internal const string SectionName = "Indexing";

        /// <summary>
        /// Gets or sets the number of DICOM instances processed by a single activity.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets the number of threads available for each batch.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int BatchThreadCount { get; set; } = 5;

        /// <summary>
        /// Gets or sets the maximum number of concurrent batches processed at a given time.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int MaxParallelBatches { get; set; } = 10;

        /// <summary>
        /// Gets the maximum number of DICOM instances that are processed concurrently
        /// across all activities for a single orchestration instance.
        /// </summary>
        public int MaxParallelCount => BatchSize * MaxParallelBatches;

        /// <summary>
        /// Gets or sets the <see cref="RetryOptions"/> for re-indexing activities.
        /// </summary>
        public RetryOptions ActivityRetryOptions { get; set; }

        // TODO: Change this hackery. The problem is that the binder used to convert 1 or more properties
        //       found in an IConfiguration object into a user-defined type can only process types with a
        //       default ctor. Unfortunately, RetryOptions does not define a default ctor, despite
        //       all of its properties being mutable. This should probably be fixed by the durable extension framework.
        [Required]
        [SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "This property is set via reflection.")]
        private RetryOptionsTemplate RetryOptions
        {
            get => ActivityRetryOptions == null
                ? null
                : new RetryOptionsTemplate
                {
                    BackoffCoefficient = ActivityRetryOptions.BackoffCoefficient,
                    FirstRetryInterval = ActivityRetryOptions.FirstRetryInterval,
                    MaxNumberOfAttempts = ActivityRetryOptions.MaxNumberOfAttempts,
                    MaxRetryInterval = ActivityRetryOptions.MaxRetryInterval,
                    RetryTimeout = ActivityRetryOptions.RetryTimeout,
                };
            set => ActivityRetryOptions = value == null
                ? null
                : new RetryOptions(value.FirstRetryInterval, value.MaxNumberOfAttempts)
                {
                    BackoffCoefficient = value.BackoffCoefficient,
                    MaxRetryInterval = value.MaxRetryInterval,
                    RetryTimeout = value.RetryTimeout,
                };
        }
    }
}
