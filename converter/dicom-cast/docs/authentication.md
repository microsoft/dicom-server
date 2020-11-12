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

Below is an example of the settings need to be added to the KeyVault for OAuth2ClientCredential authentication:

* Add secrets related to Authentication in KeyVault for Medical Imaging Server for DICOM.
  + Example: If Medical Imaging Server for Azure was configured with `OAuth2ClientCredential`, below is the list of secrets that need to added to the KeyVault.
    - DicomWeb--Authentication--Enabled : True
    - DicomWeb--Authentication--AuthenticationType : OAuth2ClientCredential
    - DicomWeb--Authentication--OAuth2ClientCredential--TokenUri : ```<AAD tenant token uri>```
    - DicomWeb--Authentication--OAuth2ClientCredential--Resource : ```Application ID URI of the resource app```
    - DicomWeb--Authentication--OAuth2ClientCredential--Scope : ```Application ID URI of the resource app```
    - DicomWeb--Authentication--OAuth2ClientCredential--ClientId : ```Client Id of the client app```
    - DicomWeb--Authentication--OAuth2ClientCredential--ClientSecret : ```Client app secret```
* Add similar secrets to KeyVault for FHIR&trade; server.
* Stop and Start the Container, to pickup the new configurations.
