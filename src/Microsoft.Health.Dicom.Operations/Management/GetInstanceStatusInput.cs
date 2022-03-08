// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Microsoft.Health.Dicom.Operations.Management
{
    /// <summary>
    /// Represents the input to <see cref="IDurableOrchestrationClient.GetStatusAsync(string, bool, bool, bool)"/>.
    /// </summary>
    public class GetInstanceStatusInput : IEquatable<GetInstanceStatusInput>
    {
        /// <summary>
        /// Gets or sets the ID of the orchestration instance to query.
        /// </summary>
        public string InstanceId { get; set; }

        /// <summary>
        /// Gets or sets a flag for including execution history in the response.
        /// </summary>
        public bool ShowHistory { get; set; }

        /// <summary>
        /// Gets or sets a flag for including input and output in the execution history response.
        /// </summary>
        public bool ShowHistoryOutput { get; set; }

        /// <summary>
        /// Gets or sets a flag for including the orchestration input.
        /// </summary>
        public bool ShowInput { get; set; } = true;

        /// <summary>
        /// Determines whether this instance and a specified object, which must also be a
        /// <see cref="GetInstanceStatusInput"/> object, have the same value.
        /// </summary>
        /// <param name="obj">The <see cref="GetInstanceStatusInput"/> object to compare to this instance.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="obj"/> is a <see cref="GetInstanceStatusInput"/> and its value
        /// is the same as this instance; otherwise, <see langword="false"/>. If <paramref name="obj"/> is
        /// <see langword="null"/>, the method returns <see langword="false"/>.
        /// </returns>
        public override bool Equals(object obj)
            => obj is GetInstanceStatusInput input && Equals(input);

        /// <summary>
        /// Determines whether this instance and another specified <see cref="GetInstanceStatusInput"/> object have the same value.
        /// </summary>
        /// <param name="other">The <see cref="GetInstanceStatusInput"/> object to compare to this instance.</param>
        /// <returns>
        /// <see langword="true"/> if the value of the <paramref name="other"/> parameter is the same as the value
        /// of this instance; otherwise, <see langword="false"/>. If <paramref name="other"/> is
        /// <see langword="null"/>,the method returns <see langword="false"/>.
        /// </returns>
        public bool Equals(GetInstanceStatusInput other)
            => other != null
            && InstanceId == other.InstanceId
            && ShowHistory == other.ShowHistory
            && ShowHistoryOutput == other.ShowHistoryOutput
            && ShowInput == other.ShowInput;

        /// <summary>
        /// Returns the hash code for this <see cref="GetInstanceStatusInput"/> object.
        /// </summary>
        /// <returns>Returns the hash code for this string using the specified rules.</returns>
        public override int GetHashCode()
            => HashCode.Combine(InstanceId, ShowHistory, ShowHistoryOutput, ShowInput);
    }
}
