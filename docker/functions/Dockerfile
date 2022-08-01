# Debugging for Azure Functions project is not supported out-of-the-box for Docker Compose tooling.
# See microsoft/DockerTools#124 for details.
# To attach a debugger manually, follow the steps here:
# https://docs.microsoft.com/en-us/visualstudio/debugger/attach-to-running-processes-with-the-visual-studio-debugger?view=vs-2019#BKMK_Linux_Docker_Attach

# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/dotnet:4.0-appservice
FROM mcr.microsoft.com/azure-functions/dotnet:4.9.1-slim@sha256:1642d5fa1472e012dfdf09150f7c0ac68c559751242c9c1e7096b635cc9b8021 AS az-func-runtime
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    AzureWebJobsScriptRoot=/home/site/wwwroot \
    LANG=en_US.UTF-8 \
    LC_ALL=en_US.UTF-8
RUN groupadd nonroot && useradd -r -M -s /sbin/nologin -g nonroot -c nonroot nonroot
USER nonroot

# Copy the DICOM Server repository and build the Azure Functions project
FROM mcr.microsoft.com/dotnet/sdk:6.0.302-alpine3.16@sha256:e0575d5756d32a4679f0cdec32a6fac22cbd43352ffe8e52c1620a34d9386a9d AS build
ARG BUILD_CONFIGURATION=Release
ARG CONTINUOUS_INTEGRATION_BUILD=false
WORKDIR /dicom-server
COPY . .
WORKDIR /dicom-server/src/Microsoft.Health.Dicom.Functions.App
RUN dotnet build "Microsoft.Health.Dicom.Functions.App.csproj" -c $BUILD_CONFIGURATION -p:ContinuousIntegrationBuild=$CONTINUOUS_INTEGRATION_BUILD -warnaserror

# Publish the Azure Functions from the build
FROM build as publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Microsoft.Health.Dicom.Functions.App.csproj" -c $BUILD_CONFIGURATION --no-build -o /home/site/wwwroot

# Copy the published application
FROM az-func-runtime AS dicom-az-func
WORKDIR /home/site/wwwroot
COPY --chown=nonroot:nonroot --from=publish /home/site/wwwroot .
