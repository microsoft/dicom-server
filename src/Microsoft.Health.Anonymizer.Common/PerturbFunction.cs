// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using MathNet.Numerics.Distributions;
using Microsoft.Health.Anonymizer.Common.Exceptions;
using Microsoft.Health.Anonymizer.Common.Models;
using Microsoft.Health.Anonymizer.Common.Settings;

namespace Microsoft.Health.Anonymizer.Common
{
    public class PerturbFunction
    {
        private readonly PerturbSetting _perturbSetting;

        public PerturbFunction(PerturbSetting perturbSetting)
        {
            EnsureArg.IsNotNull(perturbSetting, nameof(perturbSetting));

            _perturbSetting = perturbSetting ?? new PerturbSetting();
            _perturbSetting.Validate();
        }

        public AgeObject Perturb(AgeObject value)
        {
            EnsureArg.IsNotNull(value, nameof(value));

            return new AgeObject(Perturb(value.Value), value.AgeType);
        }

        public decimal Perturb(decimal value)
        {
            return (decimal)AddNoise((double)value);
        }

        public string Perturb(string value)
        {
            EnsureArg.IsNotNull(value, nameof(value));

            if (decimal.TryParse(value, out decimal originValue))
            {
                return Perturb(originValue).ToString();
            }
            else
            {
                throw new AnonymizerException(AnonymizerErrorCode.PerturbFailed, "The input value is not a numeric value that can not be perturbed.");
            }
        }

        public double Perturb(double value)
        {
            return AddNoise(value);
        }

        public float Perturb(float value)
        {
            return (float)AddNoise(value);
        }

        public int Perturb(int value)
        {
            return (int)AddNoise(value);
        }

        public short Perturb(short value)
        {
            return (short)AddNoise(value);
        }

        public long Perturb(long value)
        {
            return (long)AddNoise(value);
        }

        public uint Perturb(uint value)
        {
            return (uint)Math.Max(AddNoise(value), 0);
        }

        public ushort Perturb(ushort value)
        {
            return (ushort)Math.Max(AddNoise(value), 0);
        }

        public ulong Perturb(ulong value)
        {
            return (ulong)Math.Max(AddNoise(value), 0);
        }

        private double AddNoise(double value)
        {
            var span = _perturbSetting.Span;
            if (_perturbSetting.RangeType == PerturbRangeType.Proportional)
            {
                span = Math.Abs(value * _perturbSetting.Span);
            }

            double noise = _perturbSetting.NoiseFunction == null ? ContinuousUniform.Sample(-1 * span / 2, span / 2) : _perturbSetting.NoiseFunction(-1 * span / 2, span / 2);
            return Math.Round(value + noise, _perturbSetting.RoundTo);
        }
    }
}
