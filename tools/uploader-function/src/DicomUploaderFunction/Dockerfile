FROM mcr.microsoft.com/dotnet/sdk:6.0.401-alpine3.16@sha256:ed94db9b569b9315978fd54a7cb5f95f23229746493d91dc5c5d122d0fcfe368 AS installer-env

COPY Directory.Build.props Directory.Packages.props global.json nuget.config .editorconfig .globalconfig ./
COPY ./src/Microsoft.Health.Dicom.Client /src/Microsoft.Health.Dicom.Client
COPY ./forks /forks
COPY ./tools/uploader-function/src/DicomUploaderFunction /tools/uploader-function/src/DicomUploaderFunction
RUN cd /tools/uploader-function/src/DicomUploaderFunction && \
    mkdir -p /home/site/wwwroot && \
    dotnet publish *.csproj --output /home/site/wwwroot

# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/dotnet:4-appservice
FROM mcr.microsoft.com/azure-functions/dotnet:4.11.2-slim@sha256:903a180b9f85df88221c6995e55385e1c71eda1e233ec280fe7c061df6fa46fb
ENV AzureWebJobsScriptRoot=/home/site/wwwroot

COPY --from=installer-env ["/home/site/wwwroot", "/home/site/wwwroot"]
