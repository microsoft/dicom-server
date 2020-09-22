# Authentication

The DICOM cast project supports connecting to both DICOM and FHIR servers that require authentication. Currently there are three types of authentication supported for both servers. The authentication can be configured via the application settings by the appropriate values in the `Authentication` property of the given server.

## Managed Identity

This option uses the identity of the deployed DICOM cast instance to communicate with the server.

```json
{
  "DicomWeb": {
    "Endpoint": "https://dicom-server.example.com",
    "Authentication": {
      "Enabled": true,
      "AuthenticationType": "ManagedIdentity",
      "ManagedIdentityCredential": {
        "Resource": "https://dicom-server.example.com/"
      }
    }
  }
}
```

## OAuth2 Client Credential

This option uses a `client_credentials` OAuth2 grant to obtain an identity to communicate with the server.

```json
{
  "DicomWeb": {
    "Endpoint": "https://dicom-server.example.com",
    "Authentication": {
      "Enabled": true,
      "AuthenticationType": "OAuth2ClientCredential",
      "OAuth2ClientCredential": {
        "TokenUri": "https://idp.example.com/connect/token",
        "Resource": "https://dicom-server.example.com",
        "Scope": "https://dicom-server.example.com",
        "ClientId": "bdba742b-8138-4b7c-a6d8-03cbb7a8c053",
        "ClientSecret": "d8147077-d907-4551-8f40-90c6e86f3f0e"
      }
    }
  }
}
```

## OAuth2 User Password

This option uses a `password` OAuth2 grant to obtain an identity to communicate with the server.

```json
{
  "DicomWeb": {
    "Endpoint": "https://dicom-server.example.com",
    "Authentication": {
      "Enabled": true,
      "AuthenticationType": "OAuth2UserPasswordCredential",
      "OAuth2ClientCredential": {
        "TokenUri": "https://idp.example.com/connect/token",
        "Resource": "https://dicom-server.example.com",
        "Scope": "https://dicom-server.example.com",
        "ClientId": "bdba742b-8138-4b7c-a6d8-03cbb7a8c053",
        "ClientSecret": "d8147077-d907-4551-8f40-90c6e86f3f0e",
        "Username": "user@example.com",
        "Password": "randomstring"
      }
    }
  }
}
```

## Secrets Management

There are currently two ways provided to store secrets within the application.

### User-Secrets

User secrets are enabled when the `EnvironmentName` is `Development`. You can read more about the use of user secrets in [Safe storage of app secrets in development in ASP.NET Core](https://docs.microsoft.com/aspnet/core/security/app-secrets?view=aspnetcore-3.1).

### KeyVault

Using KeyVault to store secrets can be enabled by entering a value into the `KeyVault:Endpoint` configuration. On application start this will use the [current identity of the application](https://docs.microsoft.com/en-us/aspnet/core/security/key-vault-configuration?view=aspnetcore-3.1#use-managed-identities-for-azure-resources) to read the key vault and add a configuration provider.
