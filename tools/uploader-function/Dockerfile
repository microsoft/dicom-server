FROM mcr.microsoft.com/dotnet/sdk:6.0.301-alpine3.14@sha256:f7bcb4614e83e3da501bbd9733f74219d871c2e6d73765feaed0d3197c28f4c6 AS installer-env

COPY Directory.Build.props Directory.Packages.props global.json nuget.config .editorconfig ./
COPY ./src/Microsoft.Health.Dicom.Client /src/Microsoft.Health.Dicom.Client
COPY ./tools/uploader-function /tools/uploader-function
RUN cd /tools/uploader-function && \
    mkdir -p /home/site/wwwroot && \
    dotnet publish *.csproj --output /home/site/wwwroot

# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/dotnet:4-appservice
FROM mcr.microsoft.com/azure-functions/dotnet:4.9.0-slim@sha256:6bc5aa7c84d584f335c6ba2cdc106f875f1b408e65cd3ecf66e1825cb6d81b87 
ENV AzureWebJobsScriptRoot=/home/site/wwwroot

COPY --from=installer-env ["/home/site/wwwroot", "/home/site/wwwroot"]
