# Debugging for Azure Functions project is not supported out-of-the-box for Docker Compose tooling.
# See microsoft/DockerTools#124 for details.
# To attach a debugger manually, follow the steps here:
# https://docs.microsoft.com/en-us/visualstudio/debugger/attach-to-running-processes-with-the-visual-studio-debugger?view=vs-2019#BKMK_Linux_Docker_Attach

# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/dotnet:3.0-appservice
FROM mcr.microsoft.com/azure-functions/dotnet:4.4.1-slim@sha256:7c11c680183e908689388172dedd8be8ab1bf9420d0b81761b271780b080b57c AS az-func-runtime
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    AzureWebJobsScriptRoot=/home/site/wwwroot
RUN groupadd nonroot && useradd -r -M -s /sbin/nologin -g nonroot -c nonroot nonroot
RUN chown -R nonroot:nonroot /azure-functions-host
USER nonroot

# Copy the DICOM Server repository and build the Azure Functions project
FROM mcr.microsoft.com/dotnet/sdk:6.0.202-alpine3.14@sha256:3322ccf973ecdff1b23792186d7360c698d06cb909aa773c0929b2c6b1881125 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /dicom-server
COPY . .
WORKDIR /dicom-server/src/Microsoft.Health.Dicom.Functions.App
RUN dotnet build "Microsoft.Health.Dicom.Functions.App.csproj" -c $BUILD_CONFIGURATION

# Publish the Azure Functions from the build
FROM build as publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Microsoft.Health.Dicom.Functions.App.csproj" -c $BUILD_CONFIGURATION --no-build -o /home/site/wwwroot

# Copy the published application
FROM az-func-runtime AS dicom-az-func
WORKDIR /home/site/wwwroot
COPY --from=publish /home/site/wwwroot .
