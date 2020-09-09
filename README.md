# Medical Imaging Server for DICOM

A .NET Core implementation of the DICOMweb standard. Details of the DICOMweb standard can be found [here](https://www.dicomstandard.org/dicomweb). Details of our conformance to the standard can be found in our [Conformance Statment](docs/resources/conformance-statement.md).

## Deploy the Medical Imaging Server for DICOM

The Medical Imaging Server for DICOM is designed to run on Azure for production workloads. However, for dev/test environments it can be deployed locally as a set of Docker containers to speed development. 

### Deploy to Azure

To deploy the Medical Imaging Server for DICOM to Azure, follow the instructions [here](docs/quickstarts/deploy-via-azure.md).

If you have an Azure subscription, click the link below:

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fdcmcistorage.blob.core.windows.net%2Fcibuild%2Fdefault-azuredeploy.json" target="_blank">
    <img src="https://azuredeploy.net/deploybutton.png"/>
</a>

### Deploy locally

Follow the steps [here](docs/development.md) to deploy a local copy of the Medical Imaging Server for DICOM. Be aware that this deployment leverages the [Azurite container](https://github.com/Azure/Azurite) which emulates the Azure Storage API, and should not be used in production.

## Quickstarts

- [Deploy DICOM via Azure](docs/quickstarts/deploy-via-azure.md)
- [Deploy DICOM via Docker](docs/quickstarts/deploy-via-docker.md)
- [Set up DICOM Cast](docs/quickstarts/dicom-cast.md)

## Tutorials

- [Use DICOM web standards APIs with C#](docs/tutorials/use-dicom-web-standard-apis-with-c%23.md)
- [Use DICOM web standards APIs with Postman](docs/tutorials/use-dicom-web-standard-apis-with-postman.md)
- [Use DICOM web standards APIs with Curl](docs/tutorials/use-dicom-web-standard-apis-with-curl.md)
- [Upload files with DICOM Web Electron](docs/tutorials/upload-files-via-electron-tool.md)

## How-to guides

- [Configure DICOM server settings](docs/how-to-guides/configure-dicom-server-settings.md)
- [Enable Authentication and retrieve an OAuth token](docs/how-to-guides/enable-authentication-with-tokens.md)
- [Enable notifications on DICOM with Change Feed](docs/how-to-guides/enable-notifications-with-change-feed.md)
- [Sync DICOM metadata to FHIR](docs/how-to-guides/sync-dicom-metadata-to-fhir.md)

## Concepts

- [DICOM](docs/concepts/dicom.md)
- [Change Feed](docs/concepts/change-feed.md)
- [DICOM Cast](docs/concepts/dicom-cast.md)
- [Health Check API](docs/resources/health-check-api.md)

## Resources

- [FAQ](docs/resources/faq.md)
- [Conformance Statement](docs/resources/conformance-statement.md)

## Development

- [Setup](docs/development/development.md)
- [Code Organization](docs/development/code-organization.md)
- [Naming Guidelines](docs/development/naming-guidelines.md)
- [Exception handling](docs/development/exception-handling.md)
- [Tests](docs/development/tests.md])
- [Identity Server Authentication](docs/development/identity-server-authentication.md)

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

There are many other ways to contribute to Medical Imaging Server for DICOM.
* [Submit bugs](https://github.com/Microsoft/dicom-server/issues) and help us verify fixes as they are checked in.
* Review the [source code changes](https://github.com/Microsoft/dicom-server/pulls).
* Engage with Medical Imaging Server for DICOM users and developers on [StackOverflow](https://stackoverflow.com/questions/tagged/medical-imaging-server-for-dicom).
* Join the [#dicomonazure](https://twitter.com/hashtag/dicomonazure?f=tweets&vertical=default) discussion on Twitter.
* [Contribute bug fixes](CONTRIBUTING.md).

See [Contributing to Medical Imaging Server for DICOM](CONTRIBUTING.md) for more information.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
