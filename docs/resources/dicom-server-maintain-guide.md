# Managing deployment of Medical Imaging Server for DICOM in production

Below is a list of things to consider if you want to mantain your own deployment. We recommend you to be moderately familiar with our code. 

| Area | Description |
| --- | --- |
| ARM Deployment template | The [sample deployment](../quickstarts/deploy-via-azure.md) deploys all the dependencies and configurations, but is not production ready. Consider it has a template and evolve it to your organization needs. |
| Web Application | This is our hosting layer. Consider creating your own with the right composition of Authentication and Authorization services. The [Web application](../../src/Microsoft.Health.Dicom.Web/) we have uses development Identity server.  |
| Authentication and Authorization | Sample Azure deployment will deploy with no security, meaning the server is accessible to everyone on the internet. Review our Authentication and Authorization documentation and set it up correctly.|
| Upgrade | This is a active project, we are constantly fixing issues, adding features and re-architecting to make the service better. This means [SQL schema](https://github.com/microsoft/fhir-server/blob/main/docs/SchemaMigrationGuide.md) and binaries needs to be upgraded. Our old SQL schema versions go out support quickly. Weekly upgrade cadence with a close monitoring of commit history is recommended. |
| Monitoring | Logs from our services goes to Application Insights, consider Active monitoring and alerting on top of it. |
| Capacity and Scale | Consider production workloads and performance to plan for vertical or horizontal scale. |
| Network Security | Consider network security like Private endpoints and Virtual network access to the dependent services.|
| Pricing | App service plan and SQL server is deployed with the sample deployment. There are basic charges for these resources even if they are not used. |
| Data redudancy | Consider and SQL Database and Storage Account SKU for the right data redudancy.  |
| Disaster recovery | Consider region unavailability for mission critical deployments. |
