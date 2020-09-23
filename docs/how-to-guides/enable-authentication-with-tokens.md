# Azure Active Directory Authentication

This How-to Guide shows you how to configure the authentication settings for the Medical Imaging Server for DICOM through Azure. To complete this configuration, you will:

1. **Create a resource application in Azure AD**: This resource application will be a representation of the Medical Imaging Server for DICOM that can be used to authenticate and obtain tokens. In order for an application to interact with Azure AD, it needs to be registered.
1. **Provide app registration details to your Medical Imaging Server for DICOM**: Once the resource application is registered, you will set the authentication of your Medical Imaging Server for DICOM App Service.
1. **Create a service client application in Azure AD**: Client application registrations are Azure AD representations of applications that can be used to authenticate and obtain tokens. A service client is intended to be used by an application to obtain an access token without interactive authentication of a user. It will have certain application permissions and use an application secret (password) when obtaining access tokens.
1. **Retrieve Access Token via Postman or Azure CLI**: With your service client application enabled, you can obtain an access token to authenticate your application.

## Prerequisites

1. Deploy a [Medical Imaging Server for DICOM to Azure](../quickstarts/deploy-via-azure.md).
1. This tutorial requires an Azure AD tenant. If you have not created a tenant, see [Create a new tenant in Azure Active Directory](https://docs.microsoft.com/azure/active-directory/fundamentals/active-directory-access-create-new-tenant).

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

### Create a Resource Application in Azure AD for your Medical Imaging Server for DICOM

1. Sign into the [Azure Portal](https://ms.portal.azure.com/). Search for **App Services** and select your Medical Imaging Server for DICOM App Service. Copy the **URL** of your Dicom App Service.
1. Select **Azure Active Directory** > **App Registrations** > **New registration**:
    1. Enter a **Name** for your app registration.
    2. In **Redirect URI**, select **Web** and enter the **URL** of your Medical Imaging Server for DICOM App Service.
    3. Select **Register**.
1. Select **Expose an API** > **Set**. You can specify a URI as the **URL** of your app service or use the generated App ID URI. Select **Save**.
1. Select **Add a Scope**:
    1. In **Scope name**, enter *user.impersonation*.
    1. In the text boxes, add an admin consent display name and admin consent description you want users to see on the consent page. For example, *access my app*.

### Set the Authentication of your App Service

1. Navigate to your Medical Imaging Server for DICOM App Service that you deployed to Azure.
1. Select **Configuration** to update the **Audience** and **Authority**:
    1. Set the **Application ID URI** enabled above as the **Audience**.
    1. **Authority** is whichever tenant your application exists in, for example: ```https://login.microsoftonline.com/<tenant-name>.onmicrosoft.com```.

### Create a Service Client Application

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

1. First, update the application you create above to have access to the Azure CLI:
    1. Select **Expose an API** > **Add a Client Application**.
    1. For **Client ID**, provide the client ID of Azure CLI: **04b07795-8ddb-461a-bbee-02f9e1bf7b46**. *Note this is available at the [Azure CLI Github Repository](https://github.com/Azure/azure-cli/blob/24e0b9ef8716e16b9e38c9bb123a734a6cf550eb/src/azure-cli-core/azure/cli/core/_profile.py#L65)*.
    1. Select your **Application ID URI** under **Authorized Scopes**.
    1. Select **Add Application**.
1. [Install Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli?view=azure-cli-latest).
1. Login to Azure: ```az account```
1. Request access token using the **Application ID URI** set above: ```az account get-access-token --resource=<APP-ID-URI>```

### Get Access Token Using Postman

1. [Install Postman](https://www.postman.com/downloads/) or use the [Postman Web App](https://web.postman.co/).
1. Create a new **Post** Request with the following form-data:
    1. URL: ```<Authority>/<tenant-ID>/oauth2/token``` where **Authority** is the tenant your application exists in, configured above, and **Tenant ID** is from your Azure App Registration.
    1. *client_id*: the **Client ID** for your Service Client.
    1. *grant_type*: "client_credentials"
    1. *client_secret*: the **Client secret** for your Service Client.
    1. *resource*: the **Application ID URI** for your Resource Application.
1. Select **Send** to retrieve the access token.

## Summary

In this How-to Guide, you learned how to configure the authentication settings for the Medical Imaging Server for DICOM through Azure. To learn how to manage authentication in development and test scenarios, see [Using Identity Server for Development](../development/identity-server-authentication.md).
