# Deploy the Medical Imaging Server for DICOM locally using Docker

This quickstart guide details how to build and run the Medical Imaging Server for DICOM in Docker. By using Docker Compose, all of the necessary dependencies are started automatically in containers without requiring any installations on your development machine. In particular, the Medical Imaging Server for DICOM in Docker starts a container for [SQL Server](https://docs.microsoft.com/sql/linux/quickstart-install-connect-docker?view=sql-server-ver15&pivots=cs1-bash) and the Azure Storage emulator called [Azurite](https://github.com/Azure/Azurite).

> **IMPORTANT**
>
> This sample has been created to enable Development/Test scenarios and is not suitable for production scenarios. Passwords are contained in deployment files, the SQL server connection is not encrypted, authentication on the Medical Imaging Server for DICOM has been disabled, and data is not persisted between container restarts.

## Visual Studio (DICOM Server Only)

You can easily run and debug the Medical Imaging Server for DICOM right from Visual Studio. Simply open up the solution file *Microsoft.Health.Dicom.sln* in Visual Studio 2019 (or later) and run the "docker-compose" project. This should build each of the images and run the containers locally without any additional action.

Once it's ready, a web page should open automatically for the URL `https://localhost:8080` where you can communicate with the Medical Imaging Server for DICOM.

## Command Line

Run the following command from the root of the `microsoft/dicom-server` repository, replacing `<SA_PASSWORD>` with your chosen password (be sure to follow the [SQL Server password complexity requirements](https://docs.microsoft.com/sql/relational-databases/security/password-policy?view=sql-server-ver15#password-complexity)):

```bash
docker-compose -p healthcare -f docker/docker-compose.yml up --build -d
```

If you wish to specify your own SQL admin password, you can include one as well:

```bash
env SAPASSWORD='<SA_PASSWORD>' docker-compose -p healthcare -f docker/docker-compose.yml up --build -d
```

Once deployed the Medical Imaging Server for DICOM should be available at `http://localhost:8080/`.

### Including DICOMcast

If you also want to include DICOMcast, simply add one more file to the `docker-compose up` command:

```bash
docker-compose -p healthcare -f docker/docker-compose.yml -f docker/docker-compose.cast.yml up --build -d
```

### Run in Docker with a custom configuration

To build the `dicom-server` image run the following command from the root of the `microsoft/dicom-server`repository:

```bash
docker build -f src/microsoft.health.dicom.web/Dockerfile -t dicom-server .
```

When running the container, additional configuration details can also be specified such as:

```bash
docker run -d \
    -e DicomServer__Security__Enabled="false" \
    -e SqlServer__ConnectionString="Server=tcp:<sql-server-fqdn>,1433;Initial Catalog=Dicom;Persist Security Info=False;User ID=sa;Password=<sql-sa-password>;MultipleActiveResultSets=False;Connection Timeout=30;TrustServerCertificate=true" \
    -e SqlServer__AllowDatabaseCreation="true" \
    -e SqlServer__Initialize="true" \
    -e BlobStore__ConnectionString="<blob-connection-string>" \
    -p 8080:8080 \
    dicom-server
```

## Connecting to Dependencies

By default, the storage services like `azurite` and `sql` are not exposed locally, but you may connect to them directly by uncommenting the `ports` element in the `docker-compose.yml` file. Be sure those ports aren't already in-use locally! Without changing the values, the following ports are used:
* SQL Server exposes a TCP connection on port `1433`
  * In a SQL connection string, use `localhost:1433` or even `tcp:(local)`
* Azurite, the Azure Storage Emulator, exposes the blob service on port `10000`, the queue service on port `10001`, and the table service on port `10002`
  * The emulator uses a well-defined [connection string](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator#connect-to-the-emulator-account-using-the-well-known-account-name-and-key)
  * Use [Azure Storage Explorer](https://azure.microsoft.com/features/storage-explorer/) to browse its contents
* [FHIR](https://github.com/microsoft/fhir-server) can be accessible via `http://localhost:8081`

You can also connect to them via their IP address rather rather than via localhost. The following command will help you understand the IPs and ports by which the services are exposed:

```bash
docker inspect -f 'Name: {{.Name}} - IPs: {{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}} - Ports: {{.Config.ExposedPorts}}' $(docker ps -aq)
```

## Next steps

Once deployment is complete you can access your Medical Imaging Server at `https://localhost:8080`. Make sure to specify the version as part of the url when making requests. More information can be found in the [Api Versioning Documentation](../api-versioning.md)

* [Use Medical Imaging Server for DICOM APIs](../tutorials/use-the-medical-imaging-server-apis.md)
* [Upload DICOM files via the Electron Tool](../../tools/dicom-web-electron)
* [Enable Azure AD Authentication](../how-to-guides/enable-authentication-with-tokens.md)
* [Enable Identity Server Authentication](../development/identity-server-authentication.md)
