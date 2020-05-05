// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Routing;

namespace Microsoft.Health.Dicom.Tests.Common
{
    public class MockUrlResolver : IUrlResolver
    {
        public Uri ResolveRetrieveInstanceUri(InstanceIdentifier instance)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));

            return new Uri(
                $"/{instance.StudyInstanceUid}/{instance.SeriesInstanceUid}/{instance.SopInstanceUid}",
                UriKind.Relative);
        }

        public Uri ResolveRetrieveStudyUri(string studyInstanceUid)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));

            return new Uri(studyInstanceUid, UriKind.Relative);
        }
    }
}
