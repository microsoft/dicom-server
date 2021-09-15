# Debugging for Azure Functions project is not supported out-of-the-box for Docker Compose tooling.
# See microsoft/DockerTools#124 for details.
# To attach a debugger manually, follow the steps here:
# https://docs.microsoft.com/en-us/visualstudio/debugger/attach-to-running-processes-with-the-visual-studio-debugger?view=vs-2019#BKMK_Linux_Docker_Attach

# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/dotnet:3.0-appservice
FROM mcr.microsoft.com/azure-functions/dotnet:3.0@sha256:d8ad1c65053c914f0de17f95c40f8e77fcbbc54b7472c7a591b4bd4bf08bf034 AS az-func-runtime
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureWebJobsSecretStorageType=Files \
    FUNCTIONS_SECRETS_PATH=/etc/secrets
ADD docker/functions/host_secrets.json /etc/secrets/host.json

# Copy the DICOM Server repository and build the Azure Functions project
# Note: While the latest .NET SDK is used for building, Azure Functions build target requires .NET Core 3.1 to be installed
FROM mcr.microsoft.com/dotnet/sdk:5.0.400-alpine3.13@sha256:0e0c3c5f8448815c03ddd889218391611e644acb1af5eedd60a4b7d9eefe0b0f AS build
ARG BUILD_CONFIGURATION=Release
RUN set -x && \
    apk add --no-cache bash && \
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin -Channel 3.1 -InstallDir /usr/share/dotnet
WORKDIR /dicom-server
COPY . .
WORKDIR /dicom-server/src/Microsoft.Health.Dicom.Functions
RUN dotnet build "Microsoft.Health.Dicom.Functions.csproj" -c $BUILD_CONFIGURATION

# Publish the Azure Functions from the build
FROM build as publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Microsoft.Health.Dicom.Functions.csproj" -c $BUILD_CONFIGURATION --no-build --framework netcoreapp3.1 -o /home/site/wwwroot

# Copy the published application
FROM az-func-runtime AS dicom-az-func
WORKDIR /home/site/wwwroot
COPY --from=publish /home/site/wwwroot .
