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
using Microsoft.Health.Anonymizer.Common.Utilities;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    /// <summary>
    /// With this method, the input date/dateTime/ value will be shifted within a specific range.
    /// Dateshift function can only be used for date (DA) and datetime (DT) types.
    /// In rule setting, customers can define dateShiftRange, DateShiftKey and dateShiftScope.
    /// </summary>
    public class DateShiftProcessor : IAnonymizerProcessor
    {
        private readonly DateShiftFunction _dateShiftFunction;
        private readonly DateShiftScope _dateShiftScope = DateShiftScope.SopInstance;
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<DateShiftProcessor>();

        public DateShiftProcessor(JObject settingObject)
        {
            EnsureArg.IsNotNull(settingObject, nameof(settingObject));

            var settingFactory = new AnonymizerSettingsFactory();
            var dateShiftSetting = settingFactory.CreateAnonymizerSetting<DateShiftSetting>(settingObject);
            _dateShiftFunction = new DateShiftFunction(dateShiftSetting);
            if (settingObject.TryGetValue("DateShiftScope", StringComparison.OrdinalIgnoreCase, out JToken scope))
            {
                _dateShiftScope = (DateShiftScope)Enum.Parse(typeof(DateShiftScope), scope.ToString(), true);
            }
        }

        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(item, nameof(item));
            EnsureArg.IsNotNull(context, nameof(context));

            _dateShiftFunction.SetDateShiftPrefix(_dateShiftScope switch
            {
                DateShiftScope.StudyInstance => context.StudyInstanceUID ?? string.Empty,
                DateShiftScope.SeriesInstance => context.SeriesInstanceUID ?? string.Empty,
                DateShiftScope.SopInstance => context.SopInstanceUID ?? string.Empty,
                _ => string.Empty,
            });
            if (item.ValueRepresentation == DicomVR.DA)
            {
                var values = DicomUtility.ParseDicomDate((DicomDate)item)
                    .Where(x => !DateTimeUtility.IsAgeOverThreshold(x)) // Age over 89 will be redacted.
                    .Select(_dateShiftFunction.Shift);

                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, values.Select(DicomUtility.GenerateDicomDateString).Where(x => x != null).ToArray());
            }
            else if (item.ValueRepresentation == DicomVR.DT)
            {
                var values = DicomUtility.ParseDicomDateTime((DicomDateTime)item);
                var results = new List<string>();
                foreach (var dateObject in values)
                {
                    if (!DateTimeUtility.IsAgeOverThreshold(dateObject.DateValue))
                    {
                        dateObject.DateValue = _dateShiftFunction.Shift(dateObject.DateValue);
                        results.Add(DicomUtility.GenerateDicomDateTimeString(dateObject));
                    }
                }

                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, results.Where(x => x != null).ToArray());
            }
            else
            {
                throw new AnonymizerOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, $"DateShift is not supported for {item.ValueRepresentation}.");
            }

            _logger.LogDebug($"The value of DICOM item '{item}' is shifted.");
        }

        public bool IsSupported(DicomItem item)
        {
            EnsureArg.IsNotNull(item, nameof(item));

            return DicomDataModel.DateShiftSupportedVR.Contains(item.ValueRepresentation);
        }
    }
}
