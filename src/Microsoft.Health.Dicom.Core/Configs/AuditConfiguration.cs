// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Core.Configs
{
    public class AuditConfiguration
    {
        private string _customAuditHeaderPrefix = "X-MS-AZUREDICOM-AUDIT-";

        public string CustomAuditHeaderPrefix
        {
            get
            {
                return _customAuditHeaderPrefix;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new InvalidDefinitionException(DicomCoreResource.CustomHeaderPrefixCannotBeEmpty);
                }

                _customAuditHeaderPrefix = value;
            }
        }
    }
}
