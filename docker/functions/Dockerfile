# Debugging for Azure Functions project is not supported out-of-the-box for Docker Compose tooling. See microsoft/DockerTools#124 for details.
# To attach a debugger manually, follow the steps here:
# https://docs.microsoft.com/en-us/visualstudio/debugger/attach-to-running-processes-with-the-visual-studio-debugger?view=vs-2019#BKMK_Linux_Docker_Attach

# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/dotnet:3.0-appservice
FROM mcr.microsoft.com/azure-functions/dotnet:3.0@sha256:51e397ceb4863898580b360ca3d4ed6e29d36095983fd38e6cb526f8eb69144e AS az-func-runtime
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

# Copy the DICOM Server repository and build the Azure Functions project
FROM mcr.microsoft.com/dotnet/sdk:3.1.409-alpine3.12@sha256:3647a1c8c0b1c5533a3af9861e79299e07be33340f19b310ab9b74e2555e1b59 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /dicom-server
COPY . .
WORKDIR /dicom-server/src/Microsoft.Health.Dicom.Functions
RUN dotnet build "Microsoft.Health.Dicom.Functions.csproj" -c $BUILD_CONFIGURATION

# Publish the Azure Functions from the build
FROM build as publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Microsoft.Health.Dicom.Functions.csproj" -c $BUILD_CONFIGURATION --no-build -o /home/site/wwwroot

# Copy the published application
FROM az-func-runtime AS dicom-az-func
WORKDIR /home/site/wwwroot
COPY --from=publish /home/site/wwwroot .
