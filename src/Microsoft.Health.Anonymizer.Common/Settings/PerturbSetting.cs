// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using MathNet.Numerics.Distributions;
using Microsoft.Health.Anonymizer.Common.Exceptions;

namespace Microsoft.Health.Anonymizer.Common.Settings
{
    public class PerturbSetting
    {
        private const int MaxRoundToValue = 28;

        public double Span { get; set; } = 1;

        public PerturbRangeType RangeType { get; set; } = PerturbRangeType.Proportional;

        public int RoundTo { get; set; } = 2;

        public Func<double, double, double> NoiseFunction { get; set; } = ContinuousUniform.Sample;

        public void Validate()
        {
            if (Span < 0)
            {
                throw new AnonymizerException(
                    AnonymizerErrorCode.InvalidAnonymizerSettings,
                    "Perturb setting is invalid: Span value must be greater than 0.");
            }

            if (RoundTo > MaxRoundToValue || RoundTo < 0)
            {
                throw new AnonymizerException(
                    AnonymizerErrorCode.InvalidAnonymizerSettings,
                    "Perturb setting is invalid: RoundTo value must be in range [0, 28].");
            }
        }
    }
}
