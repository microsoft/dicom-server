// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.SqlServer.Features.Schema
{
    /// <summary>
    /// Represents an <see cref="ISchemaVersionResolver"/> that relies on a background service to resolve the version.
    /// </summary>
    public class BackgroundSchemaVersionResolver : ISchemaVersionResolver
    {
        private readonly SchemaInformation _schemaInformation;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundSchemaVersionResolver"/> class.
        /// </summary>
        /// <param name="schemaInformation">The information updated in the background.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="schemaInformation"/> is <see langword="null"/>.
        /// </exception>
        public BackgroundSchemaVersionResolver(SchemaInformation schemaInformation)
        {
            EnsureArg.IsNotNull(schemaInformation, nameof(schemaInformation));
            _schemaInformation = schemaInformation;
        }

        /// <inheritdoc/>
        public Task<SchemaVersion> GetCurrentVersionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult((SchemaVersion)_schemaInformation.Current.GetValueOrDefault());
        }
    }
}
