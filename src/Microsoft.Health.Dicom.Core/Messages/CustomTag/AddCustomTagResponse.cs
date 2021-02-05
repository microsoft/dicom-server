// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Messages.CustomTag
{
    public class AddCustomTagResponse
    {
        public AddCustomTagResponse(string job)
        {
            Job = job;
        }

        /// <summary>
        /// The Url to view job details.
        /// </summary>
        public string Job { get; }
    }
}
