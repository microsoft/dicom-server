FROM mcr.microsoft.com/dotnet/sdk:6.0.301-alpine3.14@sha256:fc3b535877460e7097af7dbbf64ce0815148206fa086340cd4c4db9ec064115e AS installer-env

COPY Directory.Build.props Directory.Packages.props global.json nuget.config .editorconfig ./
COPY ./src/Microsoft.Health.Dicom.Client /src/Microsoft.Health.Dicom.Client
COPY ./tools/uploader-function/src/DicomUploaderFunction /tools/uploader-function/src/DicomUploaderFunction
RUN cd /tools/uploader-function/src/DicomUploaderFunction && \
    mkdir -p /home/site/wwwroot && \
    dotnet publish *.csproj --output /home/site/wwwroot

# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/dotnet:4-appservice
FROM mcr.microsoft.com/azure-functions/dotnet:4.6.1-slim@sha256:11bc18a02197d460dfa96fe2aabde5a20606c85892bb670224e92c20e045ec0b 
ENV AzureWebJobsScriptRoot=/home/site/wwwroot

COPY --from=installer-env ["/home/site/wwwroot", "/home/site/wwwroot"]
