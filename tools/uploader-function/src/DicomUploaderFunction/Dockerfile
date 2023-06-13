FROM mcr.microsoft.com/dotnet/sdk:7.0.302-alpine3.17-amd64@sha256:ce1b323214a67089bbddcf29d9946e19096aa8e4671fd43c3c4290ae972626cd AS installer-env

RUN set -x && \
    apk update && \
    apk add --no-cache bash && \
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin -Channel 6.0 -InstallDir /usr/share/dotnet
COPY Directory.Build.props Directory.Packages.props global.json nuget.config .editorconfig .globalconfig ./
COPY ./src/Microsoft.Health.Dicom.Client /src/Microsoft.Health.Dicom.Client
COPY ./forks /forks
COPY ./tools/uploader-function/src/DicomUploaderFunction /tools/uploader-function/src/DicomUploaderFunction
RUN cd /tools/uploader-function/src/DicomUploaderFunction && \
    mkdir -p /home/site/wwwroot && \
    dotnet publish *.csproj --output /home/site/wwwroot

# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/dotnet:4-appservice
FROM mcr.microsoft.com/azure-functions/dotnet:4@sha256:9a34d75d2d7f82fa0bb4429d183801b4e3ffd4a9b969cf627548787db9f09754
ENV AzureWebJobsScriptRoot=/home/site/wwwroot

COPY --from=installer-env ["/home/site/wwwroot", "/home/site/wwwroot"]
