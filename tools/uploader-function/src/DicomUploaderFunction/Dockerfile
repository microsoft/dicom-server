FROM mcr.microsoft.com/dotnet/aspnet:7.0.0-alpine3.16@sha256:68c4e7e7a8720eff089658930af0a3b57ebf4d1bc2e5340e908f837733a30ef9 AS installer-env

COPY Directory.Build.props Directory.Packages.props global.json nuget.config .editorconfig .globalconfig ./
COPY ./src/Microsoft.Health.Dicom.Client /src/Microsoft.Health.Dicom.Client
COPY ./forks /forks
COPY ./tools/uploader-function/src/DicomUploaderFunction /tools/uploader-function/src/DicomUploaderFunction
RUN cd /tools/uploader-function/src/DicomUploaderFunction && \
    mkdir -p /home/site/wwwroot && \
    dotnet publish *.csproj --output /home/site/wwwroot

# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/dotnet:4-appservice
FROM mcr.microsoft.com/azure-functions/dotnet:4-slim@sha256:9afb7c76d9e01574e06f0490035d2537a0bcbaa5346509a9cdc8d3551379a5bd
ENV AzureWebJobsScriptRoot=/home/site/wwwroot

COPY --from=installer-env ["/home/site/wwwroot", "/home/site/wwwroot"]
