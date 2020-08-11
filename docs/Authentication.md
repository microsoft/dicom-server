# Authentication

This article shows you how to configure the authentication settings for the Dicom Server through Azure. To complete this configuration, you will:

1. Create an app registration in Azure AD.
1. Provide app registration details to your Dicom App Service.
1. Retrieve Access Token via Postman or Azure CLI.

This article also explains how to manage authentication in development and test scenarios without AAD integration using an Identity Provider.

## Prerequisites

1. Deploy a [Dicom server in Azure](https://github.com/microsoft/dicom-server/blob/master/README.md#deploy-to-azure).
1. This tutorial requires an Azure AD tenant. If you have not created a tenant, see [Create a new tenant in Azure Active Directory](https://docs.microsoft.com/en-us/azure/active-directory/fundamentals/active-directory-access-create-new-tenant).

## Authentication Settings Overview

The current authentication settings exposed in configuration are the following:

```json

{
  "DicomServer" : {
    "Security": {
      "Enabled":  true,
      "Authentication": {
        "Audience": "",
        "Authority": ""
      }
    }
  }
}
```

| Element                    | Description |
| -------------------------- | --- |
| Enabled                    | Whether or not the server has any security enabled. |
| Authentication:Audience    | Identifies the recipient that the token is intended for. This is set automatically by the `DevelopmentIdentityProvider`. |
| Authentication:Authority   | The issuer of the jwt token. This is set automatically by the `DevelopmentIdentityProvider`. |

## Authentication with Azure AD

### Create a Resource Application in Azure AD for your Dicom App Service

The resource application you create is an Azure AD representation of your Dicom App service that can be used to authenticate and obtain tokens.

1. Sign into the [Azure Portal](https://ms.portal.azure.com/). Search for **App Services** and select your Dicom App Service. Copy the **URL** of your Dicom App Service.
1. Select **Azure Active Directory** > **App Registrations** > **New registration**:
    1. Enter a **Name** for your app registration.
    2. In **Redirect URI**, select **Web** and enter the **URL** of your Dicom App Service.
    3. Select **Register**.
1. Select **Expose an API** > **Set**. You can specify a URI as the **URL** of your app service or use the generated App ID URI. Select **Save**.
1. Select **Add a Scope**:
    1. In **Scope name**, enter *name of scope*.
    1. In the text boxes, add a consent scope name and description you want users to see on the consent page. For example, *access my app*.

### Set the Authentication of your App Service

1. Navigate to your Dicom App Service that you deployed to Azure.
1. Select **Configuration** to update the **Audience** and **Authority**:
    1. Set the **Application ID URI** enabled above as the **Audience**.
    1. **Authority** is whichever tenant your application exists in, for example: ```https://login.microsoftonline.com/<tenant-name>.onmicrosoft.com```.

### Create a Service Client Application

A service client  is used by an application to obtain an access token without interactive authentication of a user. It will have certain application permissions and use an application secret (password) when obtaining access tokens:

1. Select **Azure Active Directory** > **App Registrations** > **New registration**:
    1. Enter a **Name** for your service client. You can provide a **URI** but it typically will not be used.
    1. Select **Register**.
1. Copy the **Application (client) ID** and the **Directory (tenant) ID** for later.
1. Select **API Permissions** to provide your service client permission to your resource application:
    1. Select **Add a permission**.
    1. Under **My APIs**, select the resource application you created above for your Dicom App Service.
    1. Under **Select Permissions**, select the application roles from the ones that you defined on the resource application.
    1. Select **Add permissions**.
1. Select **Certificates & secrets** to generate a secret for obtaining tokens:
    1. Select **New client secret**.
    1. Provide a **Description** and duration of the secret. Select **Add**.
    1. Copy the secret once it has been created. It will only be displayed once in the portal. 

### Get Access Token Using Azure CLI

To obtain an access token using Azure CLI:

1. First, update the application you create above to have access to the Azure CLI:
    1. Select **Expose an API** > **Add a Client Application**.
    1. For **Client ID**, provide the client ID of Azure CLI: **04b07795-8ddb-461a-bbee-02f9e1bf7b46**. *Note this is available at the [Azure CLI Github Repository](https://github.com/Azure/azure-cli/blob/24e0b9ef8716e16b9e38c9bb123a734a6cf550eb/src/azure-cli-core/azure/cli/core/_profile.py#L65)*.
    1. Select your **Application ID URI** under **Authorized Scopes**.
    1. Select **Add Application**.
1. [Install Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest).
1. Login to Azure: ```az account```
1. Request access token using the **Application ID URI** set above: ```az account get-access-token --resource=<APP-ID-URI>```

### Get Access Token Using Postman

To obtain an access token using Postman:

1. [Install Postman](https://www.postman.com/downloads/) or use the [Postman Web App](https://web.postman.co/).
1. Create a new **Post** Request with the following form-data:
    1. URL: ```<Authority>/<tenant-ID>/oauth2/token ``` where **Authority** is the tenant your application exists in, configured above, and **Tenant ID** is from your Azure App Registration.
    1. *client_id*: the **Client ID** for your Service Client.
    1. *grant_type*: "client_credentials"
    1. *client_secret*: the **Client secret** for your Service Client.
    1. *resource*: the **Application ID URI** for your Resource Application.
1. Select **Send** to retrieve the access token.

## Using Identity Server for Development

For the F5 experience and test environments, an in-process identity provider is included that can act as the authentication provider for the DicomWeb API.

### TestAuthEnvironment.json

The [`testauthenvironment.json`](../testauthenvironment.json) file located in the root directory holds the configuration used for the server. **This file is meant only for local and test environments.** The items represented in this file include the roles available for the API as well as users and client applications that have access to the API. During the F5 experience and local testing, the password/secret for both users and client applications is the same as the id of the item.

### Enabling Development Identity Provider for testing

[Launch settings](../src/Microsoft.Health.Dicom.Web/Properties/launchSettings.json) has `DicomWebSecurityEnabled` profile that has pre-set settings used to enable development identity provider.

### Authenticating using built in IdentityServer

To obtain a token issue the following command.

```
POST /connect/token HTTP/1.1
Host: https://localhost:63838
Content-Type: application/x-www-form-urlencoded

client_id=globalAdminServicePrincipal&client_secret=globalAdminServicePrincipal&grant_type=client_credentials&scope=health-api
```

To authenticate with the Dicom API take the `access_token` from the previous command and attach it as an `Authorization` header with the syntax: `Bearer {access_token}`.

Example token response

```json
{
    "access_token": "eyJhbGciOiJSUzI1NiIsImtpZCI6Ijc4YWJlMDM0OGEyNDg4NzU0MmUwOGJjNTg3YWFjY2Q4IiwidHlwIjoiSldUIn0.eyJuYmYiOjE1MjM1NTQ3OTQsImV4cCI6MTUyMzU1ODM5NCwiaXNzIjoiaHR0cDovL2xvY2FsaG9zdDo1MzcyNyIsImF1ZCI6WyJodHRwOi8vbG9jYWxob3N0OjUzNzI3L3Jlc291cmNlcyIsImZoaXItYXBpIl0sImNsaWVudF9pZCI6Imtub3duLWNsaWVudC1pZCIsInNjb3BlIjpbImZoaXItYXBpIl19.pZWIWy3RdDHp5zgcYs8bb9VrxIHXbYu8LolC3YTy6xWsPxMoPUQwbAltYmC6WDXFiDygpsC5ofkGlR4BH0Bt1FMvFWqFYhPcOOKvBqLLc055EHZfTcNcmiUUf4y4KRuQFqWZsH_HrfWwykSGVio2OnYcQvytrbjAi_EzHf2vrHJUHX2JFY4A_F6WpJbQiI1hUVEOd7h1jfmAptWlNGwNRbCF2Wd1Hf_Hodym8mEOKQz21VHdvNJ_B-owPMvLjalV5Nrvpv0yC9Ly5YablrkzB583eHwQNSA7A4ZMm49O8MWv8kUwwF5TF0lJJDyyw3ruqmPWCM-058chenU0rtCsPQ",
    "expires_in": 3600,
    "token_type": "Bearer"
}
```

Example Authorization header
```
Authorization: Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6Ijc4YWJlMDM0OGEyNDg4NzU0MmUwOGJjNTg3YWFjY2Q4IiwidHlwIjoiSldUIn0.eyJuYmYiOjE1MjM1NTQ3OTQsImV4cCI6MTUyMzU1ODM5NCwiaXNzIjoiaHR0cDovL2xvY2FsaG9zdDo1MzcyNyIsImF1ZCI6WyJodHRwOi8vbG9jYWxob3N0OjUzNzI3L3Jlc291cmNlcyIsImZoaXItYXBpIl0sImNsaWVudF9pZCI6Imtub3duLWNsaWVudC1pZCIsInNjb3BlIjpbImZoaXItYXBpIl19.pZWIWy3RdDHp5zgcYs8bb9VrxIHXbYu8LolC3YTy6xWsPxMoPUQwbAltYmC6WDXFiDygpsC5ofkGlR4BH0Bt1FMvFWqFYhPcOOKvBqLLc055EHZfTcNcmiUUf4y4KRuQFqWZsH_HrfWwykSGVio2OnYcQvytrbjAi_EzHf2vrHJUHX2JFY4A_F6WpJbQiI1hUVEOd7h1jfmAptWlNGwNRbCF2Wd1Hf_Hodym8mEOKQz21VHdvNJ_B-owPMvLjalV5Nrvpv0yC9Ly5YablrkzB583eHwQNSA7A4ZMm49O8MWv8kUwwF5TF0lJJDyyw3ruqmPWCM-058chenU0rtCsPQ
```
