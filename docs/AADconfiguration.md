# Authentication

This article shows you how to configure your Dicom App Service to use Azure Active Directory (Azure AD) as an authentication provider. To complete this configuration, you will:

1. Create an app registration in Azure AD.
1. Provide app registration details to your Dicom App Service.

## Prerequisites

1. Deploy a [Dicom server in Azure](https://github.com/microsoft/dicom-server/blob/master/README.md#deploy-to-azure).
1. This tutorial requires an Azure AD tenant. If you have not created a tenant, see [Create a new tenant in Azure Active Directory](https://docs.microsoft.com/en-us/azure/active-directory/fundamentals/active-directory-access-create-new-tenant).

## Create an app registration in Azure AD

1. Sign into the [Azure Portal](https://ms.portal.azure.com/). Search for **App Services** and select your Dicom App Service. Copy the **URL** of your Dicom App Service.
1. Select **Azure Active Directory** > **App Registrations** > **New registration**:
    1. Enter a **Name** for your app registration.
    2. In **Redirect URI**, select **Web** and type `<app-url>/.auth/login/aad/callback` where `<app-url>` is the **URL** of your Dicom App Service. For example, `https://my-dicom-app.azurewebsites.net/.auth/login/aad/callback`.
    3. Select **Create**.
1. Copy the **Application (client) ID** and the **Directory (tenant) ID** for later.
1. Select **Authentication**. Under **Implicit Grant**, select enable **ID Tokens** to allow OpenID Connect user sign-ins from your App Service.
1. Select **Expose an API** > **Set**. For single-tenant application, paste in the **URL** of your Dicom App Service and select **Save**. For multi-tenant applications, paste in the URL which is based on one of the tenant verified domains and then select **Save**.
1. Select **Add a Scope**:
    1. In **Scope name**, enter *name of scope*.
    1. In the text boxes, add a consent scope name and description you want users to see on the consent page. For example, *access my app*.
1. (Optional) If you want to create a client secret, select **Certificates & Secrets** > **New client secret** > **Add**. Copy the client secret value shown in the page.

## Enable Azure AD in your Dicom App Service

1. In the Azure portal, search for and select **App Services**. Select your Dicom App Service.
1. In the left pane under **Settings**, select **Authentication/Authorization** > **On**.
1. By default, App Service authentication allows unauthenticated access to your app. To enforce user authentication, set **Action to take when request is not authenticated** to **Log in with Azure Active Directory**.
1. Under **Authentication Providers**, select **Azure Active Directory**.
1. In **Management mode**, select **Advanced** and configure your Dicom App Service authentication:
    1. **Client ID**: Use the **Application (client) ID** of the app registration from above.
    1. **Issuer Url**: Use `<authentication-endpoint>/<tenant-id>/v2.0`. Replace `<authentication-endpoint>`with the [authentication endpoint for your cloud environment](https://docs.microsoft.com/en-us/azure/active-directory/develop/authentication-national-cloud#azure-ad-authentication-endpoints). Replace `<tenant-id>` with the **Directory (tenant) ID** from your app registration above.
    1. **Client Secret** (optional): Use the client secret you generated in the app registration.
1. Select **Ok** and **Save**.

You are now ready to use Azure Active Directory for authentication in your Dicom App Service.

Any users within the Azure AD tenant in which you created your App will have access to your Dicom App Service. When you provide the URL of your Dicom App service to members of your tenant, they will be prompted to log in with their AAD credentials.

To manage the users in your tenant, see [Add or delete users using Azure Active Directory](https://docs.microsoft.com/en-us/azure/active-directory/fundamentals/add-users-azure-active-directory).