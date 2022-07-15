FROM mcr.microsoft.com/dotnet/sdk:6.0.301-alpine3.14@sha256:f7bcb4614e83e3da501bbd9733f74219d871c2e6d73765feaed0d3197c28f4c6 AS installer-env

COPY Directory.Build.props Directory.Packages.props global.json nuget.config .editorconfig ./
COPY ./src/Microsoft.Health.Dicom.Client /src/Microsoft.Health.Dicom.Client
COPY ./tools/uploader-function/src/DicomUploaderFunction /tools/uploader-function/src/DicomUploaderFunction
RUN cd /tools/uploader-function/src/DicomUploaderFunction && \
    mkdir -p /home/site/wwwroot && \
    dotnet publish *.csproj --output /home/site/wwwroot

# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/dotnet:4-appservice
FROM mcr.microsoft.com/azure-functions/dotnet:4.8.0-slim@sha256:9f070359ce5bc7d61241cd5e2d5f983c153726245d1a07bc8742ebe6a9003419 
ENV AzureWebJobsScriptRoot=/home/site/wwwroot

COPY --from=installer-env ["/home/site/wwwroot", "/home/site/wwwroot"]
