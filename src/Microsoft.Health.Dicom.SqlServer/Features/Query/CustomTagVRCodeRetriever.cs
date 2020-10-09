// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.Dicom.SqlServer.Features.Query
{
    public class CustomTagVRCodeRetriever : IVRCodeRetriever
    {
        private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;

        public CustomTagVRCodeRetriever(SqlConnectionWrapperFactory sqlConnectionWrapperFactory)
        {
            _sqlConnectionWrapperFactory = sqlConnectionWrapperFactory;
        }

        public async Task<DicomVR> RetrieveAsync(DicomAttributeId attributeId, CancellationToken cancellationToken)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapper())
            {
                using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand())
                {
                    VLatest.GetCustomTagVRCode.PopulateCommand(sqlCommandWrapper, TagPath: attributeId.GetFullPath());
                    using (var reader = await sqlCommandWrapper.ExecuteReaderAsync(cancellationToken))
                    {
                        if (await reader.ReadAsync(cancellationToken))
                        {
                            (string tagPath, string tagVR) = reader.ReadRow(VLatest.CustomTag.TagPath, VLatest.CustomTag.TagVR);
                            return DicomVR.Parse(tagVR);
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }
    }
}
