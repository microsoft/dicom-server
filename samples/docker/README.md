# Running DICOM Server with Docker

*IMPORTANT:* This sample has been created to enable Dev/Test scenarios and is not suitable for production scenarios. Passwords are contained in deployment files, the SQL server connection is not encrypted, authentication on the DICOM Server has been disabled, and data is not persisted between container restarts.

The following instructions detail how to build and run the DICOM Server in Docker on Linux.

## Build and run with SQL Server and Azurite using Docker Compose

Another way to get the DICOM Server up and running on Docker is to build and run the DICOM Server with a [SQL server container](https://docs.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker?view=sql-server-ver15&pivots=cs1-bash) and an [Azurite container](https://github.com/Azure/Azurite) using docker compose. Run the following command, replacing `<SA_PASSWORD>` with your chosen password (be sure to follow the [SQL server password complexity requirements](https://docs.microsoft.com/en-us/sql/relational-databases/security/password-policy?view=sql-server-ver15#password-complexity)), from the root of the `microsoft/dicom-server` repository:

```bash
env SAPASSWORD='<SA_PASSWORD>' docker-compose -f samples/docker/docker-compose.yaml -p dicom-server up -d
```

Given the DICOM API is likely to start before the SQL server is ready, you may need to restart the API container once the SQL server is healthy. This can be done using `docker restart <container-name>`, i.e. docker restart `docker restart docker_dicom-api_1`.

Once deployed the DICOM Server should be available at `http://localhost:8080/`. Additionally the SQL server is able to be browsed using a tcp connection to localhost:1433 and the storage containers are able to be examined via [Azure Storage Explorer](https://azure.microsoft.com/en-us/features/storage-explorer/).

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
