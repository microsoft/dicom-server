# Deploy DICOM server to Azure portal

In this quickstart, you'll learn how to deploy Medical Imaging Server for DICOM using the Azure portal.

If you do not have an Azure subscription, create a [free account](https://azure.microsoft.com/free) before you begin.

Once you have your subscription, click the link below:

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fdcmcistorage.blob.core.windows.net%2Fcibuild%2Fdefault-azuredeploy.json" target="_blank">
    <img src="https://azuredeploy.net/deploybutton.png"/>
</a>

## Enter account details

1. Select your Azure subscription.
1. Select an existing resource group or create a new one.
1. Select the region to deploy your Medical Imaging Server.
1. Select a Service Name for your deployment. Note that the Service Name will be included in the URL you will use to access the application.

![required-deployment-config](../images/required-deployment.png)

## Configure deployment settings

Configure the remaining deployment settings for your Medical Imaging Server:

| Parameter | Description | Required |
|-|-|-|
| App Service Plan Resource Group | Name of the resource group containing App Service Plan. If empty, your deployment resource group is used. | No |
| App Service Plan Name | Name of App Service Plan (existing or new). If empty, a name will be generated. | No |
| App Service Plan Sku | Choose an App Service Plan SKU, or pricing tier. S1 is the default tier enabled. | No |
| Storage Account Sku | Choose a SKU for your storage account. By default, Standard Locally Redundant Storage is selected. | No |
| Deploy Application Insights | Deploy Application Insights for the DICOM server. Disabled for Microsoft Azure Government (MAG). | No |
| Application Insights Location | Select a location for Application Insights. If empty, the region closet to your deployment location is used. | No |
| Additional DICOM Server Config Properties | Additional configuration properties for the DICOM server. These properties can be modified after deployment. In the form {"path1":"value1","path2":"value2"} | No |
| Sql Admin Password | Set a password for the sql admin. | **Yes** |
| Sql Location | Set an override location for the default sql server database location. | No |
| Deploy OHIF Viewer | Deploy OHIF viewer that is configured for the DICOM server. | No |
| Security Authentication Authority | OAuth Authority. This can be modified after deployment. | No |
| Security Authentication Audience | Audience (aud) to validate in JWT. This can be modified after deployment. | No |
| Solution Type | The type of the solution | No |
| Deploy Package | Webdeploy package specified by deployPackageUrl. | No |
| Deploy Package Url | Webdeploy package to use as deployment code. If blank, the latest CI code package will be deployed. | No |

## Next steps

Once deployment is complete you can go to the newly created App Service to see the details.

The URL to access your Medical Imaging Server will be: ```https://<SERVICE NAME>.azurewebsites.net```

 - [Use DICOM Web Standard APIs with C#](../Tutorials/use-dicom-web-standard-apis-with-c#.md)
 - [Use DICOM Web Standard APIs with Curl](../Tutorials/use-dicom-web-standard-apis-with-curl.md)
 - [Use DICOM Web Standard APIs with Postman](../Tutorials/use-dicom-web-standard-apis-with-postman.md)
 - [Upload DICOM files via Electron Tool](../Tutorials/upload-files-via-electron-tool.md)
 - [Enable Azure AD Authentication](../How-to-guides/enable-authentication-with-tokens.md)
