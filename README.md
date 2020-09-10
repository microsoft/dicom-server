# Medical Imaging Server for DICOM

 [![Build Status](https://microsofthealthoss.visualstudio.com/DicomServer/_apis/build/status/CI-Build-OSS?branchName=master)](https://microsofthealthoss.visualstudio.com/DicomServer/_build/latest?definitionId=34&branchName=master)

A .NET Core implementation of the DICOM Web standard. Details of the DICOM web standard implemented can be found [here](docs/resources/conformance-statement.md).

## Deploy the Medical Imaging Server for DICOM

The source code is available to be deployed in any manner you would like. The Medical Imaging Server for DICOM can be run on-prem or in the cloud. To assist with easy deployment we have included two options below, one through Azure and one which will deploy locally.

### Deploy to Azure

To deploy the Medical Imaging Server for DICOM to Azure, follow the instructions [here](docs/quickstarts/deploy-via-azure.md).

If you have an Azure subscription, click the link below:

- Medical Imaging Server for DICOM <br/>
    <a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fdcmcistorage.blob.core.windows.net%2Fcibuild%2Fdefault-azuredeploy.json" target="_blank"><img src="https://azuredeploy.net/deploybutton.png"/></a> 

- DICOM Cast <br/>
    <a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fdcmcistorage.blob.core.windows.net%2Fcibuild%2Fdicom-cast%2Fdefault-azuredeploy.json" target="_blank"><img src="https://azuredeploy.net/deploybutton.png"/>
    </a> 

### Deploy locally

Follow the steps [here](docs/development.md) to deploy a local copy of the Medical Imaging Server for DICOM

## Development

- [Setup](docs/development/development.md)
- [Code Organization](docs/development/code-organization.md)
- [Naming Guidelines](docs/development/naming-guidelines.md)
- [Exception handling](docs/development/exception-handling.md)
- [Tests](docs/development/tests.md])
- [Identity Server Authentication](docs/development/identity-server-authentication.md)

## Quickstarts

- [Deploy Dicom via Azure](docs/quickstarts/deploy-via-azure.md)
- [Deploy Dicom via Docker](docs/quickstarts/deploy-via-docker.md)
- [Set up Dicom-cast](docs/quickstarts/dicom-cast.md)

## Tutorials

- [Use Dicom web standards APIs with C#](docs/tutorials/use-dicom-web-standard-apis-with-c%23.md)
- [Use Dicom web standards APIs with Postman](docs/tutorials/use-dicom-web-standard-apis-with-postman.md)
- [Use Dicom web standards APIs with Curl](docs/tutorials/use-dicom-web-standard-apis-with-curl.md)
- [Upload files with Dicom Web Electron](docs/tutorials/upload-files-via-electron-tool.md)

## How-to guides

- [Configure Dicom server settings](docs/how-to-guides/configure-dicom-server-settings.md)
- [Enable Authentication and retrieve an OAuth token](docs/how-to-guides/enable-authentication-with-tokens.md)
- [Enable notifications on Dicom with Change Feed](docs/how-to-guides/enable-notifications-with-change-feed.md)
- [Sync Dicom metadata to FHIR](docs/how-to-guides/sync-dicom-metadata-to-fhir.md)

## Concepts

- [Dicom](docs/concepts/dicom.md)
- [Change Feed](docs/concepts/change-feed.md)
- [Dicom-cast](docs/concepts/dicom-cast.md)

## Resources

- [FAQ](docs/resources/faq.md)
- [Health Check API](docs/resources/health-check-api.md)
- [Conformance Statement](docs/resources/conformance-statement.md)

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
