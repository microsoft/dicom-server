// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;
using Microsoft.SqlServer.Management.Smo;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    /// <summary>
    /// Validate if new stored procedures compatible with old ones
    /// </summary>
    internal class StoredProcedureCompatibleValidator
    {
        /// <summary>
        /// Validate if newProcedures are compatible with old ones.
        /// </summary>
        /// <param name="version">The current version number.</param>
        /// <param name="newProcedures">The new procedures.</param>
        /// <param name="oldProcedures">The old procedures.</param>
        public static void Validate(int version, IReadOnlyCollection<StoredProcedure> newProcedures, IReadOnlyCollection<StoredProcedure> oldProcedures)
        {
            var pairs = GetComparisonProcedures(version, newProcedures, oldProcedures);

            foreach (var pair in pairs)
            {
                var oldProcedure = pair.Item1;
                var newProcedure = pair.Item2;

                List<StoredProcedureParameter> oldParams = oldProcedure.Parameters.Cast<StoredProcedureParameter>().ToList();
                List<StoredProcedureParameter> newParams = newProcedure.Parameters.Cast<StoredProcedureParameter>().ToList();

                // any old parameter should be able to find a match
                foreach (var oldParam in oldParams)
                {
                    int iNewParam = newParams.FindIndex(x => x.Name == oldParam.Name);
                    Assert.NotEqual(-1, iNewParam);
                    Assert.Equal(oldParam.DataType, newParams[iNewParam].DataType);

                    // remove from new list since having a match
                    newParams.RemoveAt(iNewParam);
                }

                // additional parameters must have default value
                foreach (var item in newParams)
                {
                    Assert.NotEqual(string.Empty, item.DefaultValue);
                }
            }
        }

        private static List<Tuple<StoredProcedure, StoredProcedure>> GetComparisonProcedures(int version, IReadOnlyCollection<StoredProcedure> newProcedures, IReadOnlyCollection<StoredProcedure> oldProcedures)
        {
            List<Tuple<StoredProcedure, StoredProcedure>> pairs = new List<Tuple<StoredProcedure, StoredProcedure>>();
            foreach (var oldProcedure in oldProcedures)
            {
                var newOne = newProcedures.FirstOrDefault(x => x.Name == oldProcedure.Name);

                // Check to see if the removal of the procedure was expected
                if (RemovedProcedures.TryGetValue(oldProcedure.Name, out int removedVersion) && version == removedVersion)
                {
                    Assert.Null(newOne);
                }
                else
                {
                    Assert.NotNull(newOne);
                    pairs.Add(new Tuple<StoredProcedure, StoredProcedure>(oldProcedure, newOne));
                }
            }
            return pairs;
        }

        private static readonly ImmutableDictionary<string, int> RemovedProcedures =
            new KeyValuePair<string, int>[]
            {
                KeyValuePair.Create(nameof(V5.BeginAddInstance), 6),
                KeyValuePair.Create(nameof(V5.EndAddInstance), 6),
            }.ToImmutableDictionary();
    }
}
