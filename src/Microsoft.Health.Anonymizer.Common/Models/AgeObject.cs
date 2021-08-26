// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Anonymizer.Common.Models
{
    public class AgeObject
    {
        public AgeObject(uint value, AgeType ageType)
        {
            Value = value;
            AgeType = ageType;
        }

        public uint Value { get; }

        public AgeType AgeType { get; }

        public uint AgeInYears()
        {
            return AgeType switch
            {
                AgeType.Year => Value,
                AgeType.Month => Value / 12,
                AgeType.Week => Value / 52,
                AgeType.Day => Value / 365,
                _ => Value,
            };
        }
    }
}
