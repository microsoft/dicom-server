// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using EnsureThat;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.Core.Configs;
using Microsoft.Health.Core.Features.Security;

namespace Microsoft.Health.Dicom.Core.Features.Security
{
    public class DicomRoleLoader : RoleLoader<DataActions>
    {
        private Dictionary<string, DataActions> _dataActionsMap = new Dictionary<string, DataActions>();

        public DicomRoleLoader(AuthorizationConfiguration<DataActions> authorizationConfiguration, IHostEnvironment hostEnvironment)
            : base(authorizationConfiguration, hostEnvironment)
        {
            // Loop through all the enums and pre-load them into a dictionary for mapping between string representation and enum
            var enumType = typeof(DataActions);
            foreach (var name in Enum.GetNames(enumType))
            {
                var enumMemberAttribute = ((EnumMemberAttribute[])enumType.GetField(name).GetCustomAttributes(typeof(EnumMemberAttribute), true)).Single();
                _dataActionsMap.TryAdd(enumMemberAttribute.Value, Enum.Parse<DataActions>(name));
            }
        }

        protected override Role<DataActions> RoleContractToRole(RoleContract roleContract)
        {
            EnsureArg.IsNotNull(roleContract, nameof(roleContract));

            DataActions dataActions = roleContract.DataActions.Aggregate(default(DataActions), (acc, a) => acc | ToEnum(a));
            DataActions notDataActions = roleContract.NotDataActions.Aggregate(default(DataActions), (acc, a) => acc | ToEnum(a));

            return new Role<DataActions>(roleContract.Name, dataActions & ~notDataActions, roleContract.Scopes.Single());
        }

        private DataActions ToEnum(string str)
        {
            if (_dataActionsMap.TryGetValue(str, out DataActions foundDataAction))
            {
                return foundDataAction;
            }

            throw new ArgumentOutOfRangeException($"Invalid data action supplied: {str}");
        }
    }
}
