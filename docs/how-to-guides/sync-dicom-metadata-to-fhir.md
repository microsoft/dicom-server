# Sync Medical Imaging Server for DICOM metadata into Azure API for FHIR Resources

In this How-to Guide, you will learn how to sync Medical Imaging Server for DICOM metadata with FHIR. To do this, you will learn how to enable DICOM Cast by authentication with Managed Identity.

For healthcare organizations seeking to integrate clinical and imaging data through the FHIR® standard, DICOM Cast enables synchronizing of DICOM image changes to FHIR&trade; ImagingStudy resource. This allows you to sync DICOM studies, series and instances into the FHIR&trade; ImagingStudy resource.

Once you have competed the [prerequisites](##Prerequisites) and [enabled authentication](##Configure-Authentication-using-Managed-Identity) between your Medical Imaging Server for DICOM, your FHIR Server and your DICOM Cast deployment, you have enabled DICOM Cast. When you upload files to your Medical Imaging Server for DICOM, the corresponding FHIR resources will be persisted in your FHIR server.

To learn more about DICOM Cast, see the [DICOM Cast Concept](../docs/Concepts/dicom-cast.md).

## Prerequisites

To enable DICOM Cast, you need to complete the following steps:

1. [Deploy a Medical Imaging Server for DICOM](../quickstarts/deploy-via-azure.md)
1. [Deploy a FHIR Server](https://github.com/microsoft/fhir-server)
1. [Deploy DICOM Cast](../quickstarts/deploy-dicom-cast.md)

## Configure Authentication using Managed Identity

Currently there are three types of authentication supported for both Azure API for FHIR and Medical Imaging Server for DICOM: Managed Identity, OAuth2 Client Credential and OAuth2 User Password. The authentication can be configured via the application settings by the appropriate values in the `Authentication` property of the given server. For details on the three types, see [DICOM Cast authentication](/converter/dicom-cast/docs/authentication.md).

This section will provide an end to end guide for configuring authentication with Managed Identity.

### Create a resource application for FHIR and DICOM servers

For both your FHIR and DICOM servers, you will create a resource application in Azure. Follow the instructions below for each server, once for your Medical Imaging Server for DICOM and once for your FHIR Server.

1. Sign into the [Azure Portal](https://ms.portal.azure.com/). Search for **App Services** and select the FHIR or DICOM App Service. Copy the **URL** of the App Service.
1. Select **Azure Active Directory** > **App Registrations** > **New registration**:
    1. Enter a **Name** for your app registration.
    2. In **Redirect URI**, select **Web** and enter the **URL** of your App Service.
    3. Select **Register**.
1. Select **Expose an API** > **Set**. You can specify a URI as the **URL** of your app service or use the generated App ID URI. Select **Save**.
1. Select **Add a Scope**:
    1. In **Scope name**, enter *user.impersonation*.
    1. In the text boxes, add an admin consent display name and admin consent description you want users to see on the consent page. For example, *access my app*.

### Set the Authentication for your FHIR & DICOM App Services

For both your FHIR and DICOM servers, you will set the Audience and Authority for Authentication. Follow the instructions below for each server, once for your Medical Imaging Server for DICOM and once for your FHIR Server.

1. Navigate to the App Service that you deployed to Azure.
1. Select **Configuration** to update the **Audience** and **Authority**:
    1. Set the **Application ID URI** from the App Service as the **Audience**.
    1. **Authority** is whichever tenant your application exists in, for example: ```https://login.microsoftonline.com/<tenant-name>.onmicrosoft.com```.

### Update Key Vault for DICOM Cast

1. Navigate to the DICOM Cast Key Vault that was created when you deployed DICOM Cast.
1. Select **Access Policies** in the menu bar and click **Add Access Policy**.
    1. Under **Configure from template**, select **Secret Management**.
    1. Under **Select principal**, click **None selected**. Search for your Service Principle, click **Select** and then **Add**. 
    1. Select **Save**.
1. Select **Secrets** in the menu bar and click **Generate/Import**. Use the tables below to add secrets for your DICOM and FHIR servers. For each secret, use the **Manual Upload option** and click **Create**:

#### Medical Imaging Server for DICOM Secrets

| Name | Value |
| :------- | :----- |
| DICOM--Endpoint | ```<dicom-server-url>``` |
| DicomWeb--Authentication--Enabled | "true" |
| DicomWeb--Authentication--AuthenticationType | "ManagedIdentity" |
| DicomWeb--Authentication--ManagedIdentityCredential--Resource | ```<dicom-server-url>``` |

#### FHIR Server Secrets

| Name | Value |
| :------- | :----- |
| Fhir--Endpoint | ```<fhir-server-url>``` |
| Fhir--Authentication--Enabled | "true" |
| Fhir--Authentication--AuthenticationType | "ManagedIdentity" |
| Fhir--Authentication--ManagedIdentityCredential--Resource | ```<fhir-server-url>``` |

### Restart Azure Container Instance for DICOM Cast

Now that you have enabled Authentication for DICOM Cast, you have to Stop and Start the Azure Container Instance to pickup the new configurations:

1. Navigate to the Container Instance created when you deployed DICOM Cast.
1. Click **Stop** and then **Start**.

## Summary

In this How-to Guide, you learned how to enable DICOM Cast by authentication with Managed Identity. Now you can upload DICOM files to your Medical Imaging Server for DICOM, and the corresponding FHIR resources will be populated in your FHIR server.

To manage authentication with OAuth2 Client Credentials or OAuth2 User Passwords, see [DICOM Cast authentication](/converter/dicom-cast/docs/authentication.md). 

For an overview of DICOM Cast, see [DICOM Cast Concept](../concepts/dicom-cast.md).

To upload files to your DICOM Server, refer to [Use the Medical Imaging Server APIs](../tutorials/use-the-medical-imaging-server-apis.md).
