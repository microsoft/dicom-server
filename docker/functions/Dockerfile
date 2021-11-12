# Debugging for Azure Functions project is not supported out-of-the-box for Docker Compose tooling.
# See microsoft/DockerTools#124 for details.
# To attach a debugger manually, follow the steps here:
# https://docs.microsoft.com/en-us/visualstudio/debugger/attach-to-running-processes-with-the-visual-studio-debugger?view=vs-2019#BKMK_Linux_Docker_Attach

# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/dotnet:3.0-appservice
FROM mcr.microsoft.com/azure-functions/dotnet:4.0.1.16816@sha256:2c18c4ca6ad982257e9023161329c38be6d9d494bc48a6aae15ef43cc8f7ea5d AS az-func-runtime
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    AzureWebJobsScriptRoot=/home/site/wwwroot

# Copy the DICOM Server repository and build the Azure Functions project
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine3.13@sha256:14cc4b76c0c06ec6fc58270662105c790da59e01600d60f741eea7920c023c96 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /dicom-server
COPY . .
WORKDIR /dicom-server/src/Microsoft.Health.Dicom.Functions
RUN dotnet build "Microsoft.Health.Dicom.Functions.csproj" -c $BUILD_CONFIGURATION

# Publish the Azure Functions from the build
FROM build as publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Microsoft.Health.Dicom.Functions.csproj" -c $BUILD_CONFIGURATION --no-build --framework net6.0 -o /home/site/wwwroot

# Copy the published application
FROM az-func-runtime AS dicom-az-func
WORKDIR /home/site/wwwroot
COPY --from=publish /home/site/wwwroot .
