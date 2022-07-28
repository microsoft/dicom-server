// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IdentityModel.Tokens.Jwt;

namespace Microsoft.Health.Dicom.Api.Features.Security;
public class JwtSecurityTokenParser : IJwtSecurityTokenParser
{
    public JwtSecurityTokenParser()
    {
    }

    public string GetIssuer(string token)
    {
        var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
        var decodedToken = jwtSecurityTokenHandler.ReadJwtToken(token);

        return decodedToken?.Issuer;
    }
}
