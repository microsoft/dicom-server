// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public class ETagGenerator : IETagGenerator
    {
        private readonly IInstanceStore _instanceStore;

        public ETagGenerator(IInstanceStore instanceStore)
        {
            EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
            _instanceStore = instanceStore;
        }

        public string GetETag(ResourceType resourceType, IEnumerable<VersionedInstanceIdentifier> retrieveInstances)
        {
            EnsureArg.IsTrue(
                resourceType == ResourceType.Study ||
                resourceType == ResourceType.Series ||
                resourceType == ResourceType.Instance,
                nameof(resourceType));
            EnsureArg.IsNotNull(retrieveInstances);

            string eTag = string.Empty;
            long maxWatermark = retrieveInstances.Max(ri => ri.Version);

            switch (resourceType)
            {
                case ResourceType.Study:
                case ResourceType.Series:
                    int countInstances = retrieveInstances.Count();
                    eTag = $"{maxWatermark}-{countInstances}";
                    break;
                case ResourceType.Instance:
                    eTag = maxWatermark.ToString();
                    break;
                default:
                    break;
            }

            return eTag;
        }
    }
}
