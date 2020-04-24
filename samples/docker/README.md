# Running DICOM Server with Docker

*IMPORTANT:* This sample has been created to enable Dev/Test scenarios and is not suitable for production scenarios. Passwords are contained in deployment files, the SQL server connection is not encrypted, authentication on the DICOM Server has been disabled, and data is not persisted between container restarts.

The following instructions detail how to build and run the DICOM Server in Docker on Linux.

## Build and run with SQL Server using Docker Compose

Another way to get the DICOM Server up and running on Docker is to build and run the DICOM Server with a SQL server container using docker compose. Run the following command, replacing `<SA_PASSWORD>` with your chosen password (be sure to follow the [SQL server password complexity requirements](https://docs.microsoft.com/en-us/sql/relational-databases/security/password-policy?view=sql-server-ver15#password-complexity)), from the root of the `microsoft/dicom-server` repository:

```bash
env SAPASSWORD='<SA_PASSWORD>' docker-compose -f samples/docker/docker-compose.yaml up -d
```

Given the DICOM API is likely to start before the SQL server is ready, you may need to restart the API container once the SQL server is healthy. This can be done using `docker restart <container-name>`, i.e. docker restart `docker restart docker_dicom-api_1`.

Once deployed the DICOM Server should be available at `http://localhost:8080/`.

## Run in Docker with a custom configuration

To build the `dicom-api` image run the following command from the root of the `microsoft/dicom-server`repository:

```bash
docker build -f samples/docker/Dockerfile -t azure-dicom-api .
```

The container can then be run, specifying configuration details such as:

```bash
docker run -d \
    -e DicomServer__Security__Enabled="false"
    -e SqlServer__ConnectionString="Server=tcp:<sql-server-fqdn>,1433;Initial Catalog=Dicom;Persist Security Info=False;User ID=sa;Password=<sql-sa-password>;MultipleActiveResultSets=False;Connection Timeout=30;" \
    -e SqlServer__AllowDatabaseCreation="true" \
    -e SqlServer__Initialize="true" \
    -e DataStore="SqlServer" \
    -p 8080:8080
    azure-dicom-api azure-dicom-api
```
