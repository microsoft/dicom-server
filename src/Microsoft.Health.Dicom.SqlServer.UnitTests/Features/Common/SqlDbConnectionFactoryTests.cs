// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Dicom.SqlServer.Features.Common;
using Microsoft.Health.SqlServer;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.SqlServer.UnitTests.Features.Common
{
    public class SqlDbConnectionFactoryTests
    {
        [Fact]
        public void GivenNullArgument_WhenConstructing_ThenThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SqlDbConnectionFactory(null));
        }

        [Fact]
        public async Task GivenAnyInvocation_WhenGettingConnection_ThenReturnUnderlyingResult()
        {
            ISqlConnectionFactory sqlConnectionFactory = Substitute.For<ISqlConnectionFactory>();
            var factory = new SqlDbConnectionFactory(sqlConnectionFactory);

            var expected = new SqlConnection();
            using var tokenSource = new CancellationTokenSource();
            sqlConnectionFactory.GetSqlConnectionAsync(null, tokenSource.Token).Returns(expected);

            Assert.Same(expected, await factory.GetConnectionAsync(tokenSource.Token));
        }
    }
}
