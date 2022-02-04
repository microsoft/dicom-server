// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.DicomCast.Core.UnitTests;
using Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction;
using Microsoft.Health.DicomCast.TableStorage.Configs;
using Microsoft.Health.DicomCast.TableStorage.Features.Storage;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.DicomCast.TableStorage.UnitTests.Features.Storage
{
    public class TableExceptionStoreTests
    {
        private readonly TableExceptionStore _tableExceptionStore;

        public TableExceptionStoreTests()
        {
            Dictionary<string, string> _tableNames = new Dictionary<string, string>();
            _tableNames.Add("FhirFailToStoreExceptionTable", "FhirFailToStoreExceptionTable");

            TableServiceClientProvider tableServiceClientProvider = new TableServiceClientProvider
                (Substitute.For<TableServiceClient>(), Substitute.For<ITableServiceClientInitializer>(), Substitute.For<IOptions<TableDataStoreConfiguration>>(), NullLogger<TableServiceClientProvider>.Instance);

            tableServiceClientProvider.TableList.Returns(_tableNames);

            _tableExceptionStore = new TableExceptionStore(tableServiceClientProvider, NullLogger<TableExceptionStore>.Instance);
        }

        [Fact]
        public async Task GivenTableExceptionSToreWithNoDicomCastName_WhenExceptionsAreThrown_AreStoredInTablesSuccessfully()
        {

            await _tableExceptionStore.WriteExceptionAsync(ChangeFeedGenerator.Generate(1, metadata: FhirTransactionContextBuilder.CreateDicomDataset()), new Exception("new Exception"), Core.Features.ExceptionStorage.ErrorType.FhirError, CancellationToken.None);
        }

    }
}
