// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Health.Dicom.Core.Configs
{
    public class AuthenticationConfiguration
    {
        public string Audience { get; set; }

        public IEnumerable<string> Audiences { get; set; }

        public string Authority { get; set; }

        public string[] GetValidAudiences()
        {
            if (Audiences != null)
            {
                return Audiences.ToArray();
            }

            if (!string.IsNullOrWhiteSpace(Audience))
            {
                return new string[] { Audience };
            }

            return null;
        }
    }
}
