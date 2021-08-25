// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Text;
using Dicom;
using Dicom.IO.Buffer;
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
    /// This function hash the value and outputs a Hex encoded representation.
    /// The length of output string depends on the hash function (e.g. sha256 will output 64 bytes length), you should pay attention to the length limitation of output DICOM file.
    /// In cryptoHash setting, you can set cryptoHash key and cryptoHash function (only support sha256 for now) for cryptoHash.
    /// </summary>
    public class CryptoHashProcessor : IAnonymizerProcessor
    {
        private readonly CryptoHashFunction _cryptoHashFunction;
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<CryptoHashProcessor>();

        public CryptoHashProcessor(JObject settingObject)
        {
            EnsureArg.IsNotNull(settingObject, nameof(settingObject));

            var settingFactory = new AnonymizerSettingsFactory();
            var cryptoHashSetting = settingFactory.CreateAnonymizerSetting<CryptoHashSetting>(settingObject);
            _cryptoHashFunction = new CryptoHashFunction(cryptoHashSetting);
        }

        public void Process(DicomDataset dicomDataset, DicomItem item, ProcessContext context = null)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(item, nameof(item));

            if (item is DicomStringElement)
            {
                var hashedValues = ((DicomStringElement)item).Get<string[]>().Select(GetCryptoHashString);
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, hashedValues.ToArray());
            }
            else if (item is DicomOtherByte)
            {
                var valueBytes = ((DicomOtherByte)item).Get<byte[]>();
                var hashedBytes = _cryptoHashFunction.Hash(valueBytes);
                dicomDataset.AddOrUpdate(item.ValueRepresentation, item.Tag, hashedBytes);
            }
            else if (item is DicomFragmentSequence)
            {
                var element = item.ValueRepresentation == DicomVR.OW
                    ? (DicomFragmentSequence)new DicomOtherWordFragment(item.Tag)
                    : new DicomOtherByteFragment(item.Tag);

                foreach (var fragment in (DicomFragmentSequence)item)
                {
                    element.Fragments.Add(new MemoryByteBuffer(_cryptoHashFunction.Hash(fragment.Data)));
                }

                dicomDataset.AddOrUpdate(element);
            }
            else
            {
                throw new AnonymizerOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, $"CryptoHash is not supported for {item.ValueRepresentation}.");
            }

            _logger.LogDebug($"The value of DICOM item '{item}' is cryptoHashed.");
        }

        public bool IsSupported(DicomItem item)
        {
            EnsureArg.IsNotNull(item, nameof(item));

            return DicomDataModel.CryptoHashSupportedVR.Contains(item.ValueRepresentation) || item is DicomFragmentSequence;
        }

        public string GetCryptoHashString(string input)
        {
            EnsureArg.IsNotNull(input, nameof(input));

            var resultBytes = _cryptoHashFunction.Hash(Encoding.UTF8.GetBytes(input));
            return string.Concat(resultBytes.Select(b => b.ToString("x2")));
        }
    }
}
