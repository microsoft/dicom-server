# Debugging for Azure Functions project is not supported out-of-the-box for Docker Compose tooling.
# See microsoft/DockerTools#124 for details.
# To attach a debugger manually, follow the steps here:
# https://docs.microsoft.com/en-us/visualstudio/debugger/attach-to-running-processes-with-the-visual-studio-debugger?view=vs-2019#BKMK_Linux_Docker_Attach

# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/dotnet:3.0-appservice
FROM mcr.microsoft.com/azure-functions/dotnet:3.4.1@sha256:43a278cdf6f4132717645a0f8eb04bc6f0153642c95f5fc5938835fd034eee73 AS az-func-runtime
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    AzureWebJobsScriptRoot=/home/site/wwwroot

# Copy the DICOM Server repository and build the Azure Functions project
# Note: While the latest .NET SDK is used for building, Azure Functions build target requires .NET Core 3.1 to be installed
FROM mcr.microsoft.com/dotnet/sdk:5.0.402-alpine3.13@sha256:f08db3184fc8fbb12dd4a9d12eb2e3c0a36ca60a4dd13759edde22605fbd00bd AS build
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
