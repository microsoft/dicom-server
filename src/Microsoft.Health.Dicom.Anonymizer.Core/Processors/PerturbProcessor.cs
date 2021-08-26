// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Anonymizer.Common;
using Microsoft.Health.Anonymizer.Common.Settings;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    /// <summary>
    /// With perturb rule, you can replace specific values by adding noise.
    /// Perturb function can be used for numeric values (ushort, short, uint, int, ulong, long, decimal, double, float).
    /// </summary>
    public class PerturbProcessor : IAnonymizerProcessor
    {
        private readonly PerturbFunction _perturbFunction;
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<PerturbProcessor>();

        private static readonly Dictionary<DicomVR, Type> _numericElementTypeMapping = new Dictionary<DicomVR, Type>()
        {
            { DicomVR.DS, typeof(DicomDecimalString) },
            { DicomVR.FL, typeof(DicomFloatingPointSingle) },
            { DicomVR.OF, typeof(DicomOtherFloat) },
            { DicomVR.FD, typeof(DicomFloatingPointDouble) },
            { DicomVR.OD, typeof(DicomOtherDouble) },
            { DicomVR.IS, typeof(DicomIntegerString) },
            { DicomVR.SL, typeof(DicomSignedLong) },
            { DicomVR.SS, typeof(DicomSignedShort) },
            { DicomVR.US, typeof(DicomUnsignedShort) },
            { DicomVR.OW, typeof(DicomOtherWord) },
            { DicomVR.UL, typeof(DicomUnsignedLong) },
            { DicomVR.OL, typeof(DicomOtherLong) },
            { DicomVR.UV, typeof(DicomUnsignedVeryLong) },
            { DicomVR.OV, typeof(DicomOtherVeryLong) },
            { DicomVR.SV, typeof(DicomSignedVeryLong) },
        };

        private static readonly Dictionary<DicomVR, Type> _numericValueTypeMapping = new Dictionary<DicomVR, Type>()
        {
            { DicomVR.DS, typeof(decimal[]) },
            { DicomVR.FL, typeof(float[]) },
            { DicomVR.OF, typeof(float[]) },
            { DicomVR.FD, typeof(double[]) },
            { DicomVR.OD, typeof(double[]) },
            { DicomVR.IS, typeof(int[]) },
            { DicomVR.SL, typeof(int[]) },
            { DicomVR.SS, typeof(short[]) },
            { DicomVR.US, typeof(ushort[]) },
            { DicomVR.OW, typeof(ushort[]) },
            { DicomVR.UL, typeof(uint[]) },
            { DicomVR.OL, typeof(uint[]) },
            { DicomVR.UV, typeof(ulong[]) },
            { DicomVR.OV, typeof(ulong[]) },
            { DicomVR.SV, typeof(long[]) },
        };

        public PerturbProcessor(JObject settingObject)
        {
            EnsureArg.IsNotNull(settingObject, nameof(settingObject));

            var settingFactory = new AnonymizerSettingsFactory();
            var perturbSetting = settingFactory.CreateAnonymizerSetting<PerturbSetting>(settingObject);
            _perturbFunction = new PerturbFunction(perturbSetting);
        }

        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(item, nameof(item));

            if (item.ValueRepresentation == DicomVR.AS)
            {
                var values = ((DicomAgeString)item).Get<string[]>().Select(DicomUtility.ParseAge).Select(x => _perturbFunction.Perturb(x));
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Select(DicomUtility.GenerateAgeString).Where(x => x != null).ToArray());
            }
            else
            {
                if (!_numericValueTypeMapping.ContainsKey(item.ValueRepresentation))
                {
                    throw new AnonymizerOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, $"Perturb is not supported for {item.ValueRepresentation}.");
                }

                var elementType = _numericElementTypeMapping[item.ValueRepresentation];
                var valueType = _numericValueTypeMapping[item.ValueRepresentation];

                // Get numeric value using reflection.
                var valueObj = elementType.GetMethod("Get").MakeGenericMethod(valueType).Invoke(item, new object[] { -1 });
                PerturbNumericValue(dicomDataset, item, valueObj as Array);
            }

            _logger.LogDebug($"The value of DICOM item '{item}' is perturbed.");
        }

        private void PerturbNumericValue(DicomDataset dicomDataset, DicomItem item, Array values)
        {
            if (values == null || values.Length == 0)
            {
                return;
            }

            var valueType = values.GetValue(0).GetType();
            if (valueType == typeof(decimal))
            {
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Cast<decimal>().Select(_perturbFunction.Perturb).ToArray());
            }
            else if (valueType == typeof(double))
            {
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Cast<double>().Select(_perturbFunction.Perturb).ToArray());
            }
            else if (valueType == typeof(float))
            {
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Cast<float>().Select(_perturbFunction.Perturb).ToArray());
            }
            else if (valueType == typeof(int))
            {
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Cast<int>().Select(_perturbFunction.Perturb).ToArray());
            }
            else if (valueType == typeof(uint))
            {
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Cast<uint>().Select(_perturbFunction.Perturb).ToArray());
            }
            else if (valueType == typeof(short))
            {
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Cast<short>().Select(_perturbFunction.Perturb).ToArray());
            }
            else if (valueType == typeof(ushort))
            {
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Cast<ushort>().Select(_perturbFunction.Perturb).ToArray());
            }
            else if (valueType == typeof(long))
            {
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Cast<long>().Select(_perturbFunction.Perturb).ToArray());
            }
            else if (valueType == typeof(ulong))
            {
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Cast<ulong>().Select(_perturbFunction.Perturb).ToArray());
            }
            else
            {
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Cast<string>().Select(_perturbFunction.Perturb).ToArray());
            }
        }

        public bool IsSupported(DicomItem item)
        {
            EnsureArg.IsNotNull(item, nameof(item));

            return DicomDataModel.PerturbSupportedVR.Contains(item.ValueRepresentation) && !(item is DicomFragmentSequence);
        }
    }
}
