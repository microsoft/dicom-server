// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <param name="newProcedures">The new procedures.</param>
        /// <param name="oldProcedures">The old procedures.</param>
        public static void Validate(IReadOnlyCollection<StoredProcedure> newProcedures, IReadOnlyCollection<StoredProcedure> oldProcedures)
        {
            var pairs = GetComparisonProcedures(newProcedures, oldProcedures);

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

        private static List<Tuple<StoredProcedure, StoredProcedure>> GetComparisonProcedures(IReadOnlyCollection<StoredProcedure> newProcedures, IReadOnlyCollection<StoredProcedure> oldProcedures)
        {
            List<Tuple<StoredProcedure, StoredProcedure>> pairs = new List<Tuple<StoredProcedure, StoredProcedure>>();
            foreach (var oldProcedure in oldProcedures)
            {
                // every procedure in old database must have a match in new
                var newOne = newProcedures.FirstOrDefault(x => x.Name == oldProcedure.Name);
                Assert.NotNull(newOne);
                pairs.Add(new Tuple<StoredProcedure, StoredProcedure>(oldProcedure, newOne));
            }
            return pairs;
        }
    }
}
