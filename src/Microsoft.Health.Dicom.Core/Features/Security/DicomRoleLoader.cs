// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
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
        public DicomRoleLoader(AuthorizationConfiguration<DataActions> authorizationConfiguration, IHostEnvironment hostEnvironment)
            : base(authorizationConfiguration, hostEnvironment)
        {
        }

        protected override Role<DataActions> RoleContractToRole(RoleContract roleContract)
        {
            EnsureArg.IsNotNull(roleContract, nameof(roleContract));

            DataActions dataActions = roleContract.DataActions.Aggregate(default(DataActions), (acc, a) => acc | ToEnum<DataActions>(a));
            DataActions notDataActions = roleContract.NotDataActions.Aggregate(default(DataActions), (acc, a) => acc | ToEnum<DataActions>(a));

            return new Role<DataActions>(roleContract.Name, dataActions & ~notDataActions, roleContract.Scopes.Single());
        }

        // TODO: See if there's a better way to do this than the StackOverflow answer
        public static T ToEnum<T>(string str)
        {
            var enumType = typeof(T);
            foreach (var name in Enum.GetNames(enumType))
            {
                var enumMemberAttribute = ((EnumMemberAttribute[])enumType.GetField(name).GetCustomAttributes(typeof(EnumMemberAttribute), true)).Single();
                if (enumMemberAttribute.Value == str)
                {
                    return (T)Enum.Parse(enumType, name);
                }
            }

            // throw exception or whatever handling you want or
            return default(T);
        }
    }
}
