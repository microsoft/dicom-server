# Deploy the Medical Imaging Server for DICOM locally using Docker

This quickstart details how to build and run the Medical Imaging Server for DICOM in Docker on Linux.

> [!IMPORTANT]
> This sample has been created to enable Development/Test scenarios and is not suitable for production scenarios. Passwords are contained in deployment files, the SQL server connection is not encrypted, authentication on the Medical Imaging Server for DICOM has been disabled, and data is not persisted between container restarts.

## Prerequisites

To deploy the Medical Imaging Server for DICOM using Docker Compose, you have to have a local version of the OSS repository. To clone the repository via the command line, run the following command:

```bash
git clone https://github.com/microsoft/dicom-server.git

```

## Build and run with SQL Server and Azurite using Docker Compose

Use Docker Compose to run the Medical Imaging Server for DICOM on Docker with a [SQL Server container](https://docs.microsoft.com/sql/linux/quickstart-install-connect-docker?view=sql-server-ver15&pivots=cs1-bash) and an [Azurite container](https://github.com/Azure/Azurite).

Run the following command from the root of the `microsoft/dicom-server` repository, replacing `<SA_PASSWORD>` with your chosen password (be sure to follow the [SQL Server password complexity requirements](https://docs.microsoft.com/sql/relational-databases/security/password-policy?view=sql-server-ver15#password-complexity)):

```bash
env SAPASSWORD='<SA_PASSWORD>' docker-compose -f samples/docker/docker-compose.yaml -p dicom-server up -d
```

Given the DICOM API is likely to start before the SQL server is ready, you may need to restart the API container once the SQL server is healthy. This can be done using `docker restart <container-name>`, i.e. docker restart `docker restart docker_dicom-api_1`.

Once deployed the Medical Imaging Server for DICOM should be available at `http://localhost:8080/`. Additionally the SQL Server is able to be browsed using a TCP connection to localhost:1433 and the storage containers can be examined via [Azure Storage Explorer](https://azure.microsoft.com/features/storage-explorer/).

## Run in Docker with a custom configuration

To build the `dicom-api` image run the following command from the root of the `microsoft/dicom-server`repository:

```bash
docker build -f build/docker/Dockerfile -t dicom-api .
```

The container can then be run, specifying configuration details such as:

```bash
docker run -d \
    -e DicomServer__Security__Enabled="false"
    -e SqlServer__ConnectionString="Server=tcp:<sql-server-fqdn>,1433;Initial Catalog=Dicom;Persist Security Info=False;User ID=sa;Password=<sql-sa-password>;MultipleActiveResultSets=False;Connection Timeout=30;" \
    -e SqlServer__AllowDatabaseCreation="true" \
    -e SqlServer__Initialize="true" \
    -e DataStore="SqlServer" \
    -e BlobStore__ConnectionString="<blob-connection-string" \
    -p 8080:8080
    dicom-api dicom-api
```

## Next steps

Once deployment is complete you can access your Medical Imaging Server at: ```https://localhost:8080```

* [Use Medical Imaging Server for DICOM APIs](../tutorials/use-the-medical-imaging-server-apis.md)
* [Upload DICOM files via the Electron Tool](../../tools/dicom-web-electron)
* [Enable Azure AD Authentication](../how-to-guides/enable-authentication-with-tokens.md)
* [Enable Identity Server Authentication](../development/identity-server-authentication.md)
