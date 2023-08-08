FROM mcr.microsoft.com/dotnet/sdk:7.0.306-alpine3.17-amd64@sha256:d891127d6f13b97092cd41ec37324dd46977b161c9a16d0ffa3da6282ef79fb1 AS installer-env

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
FROM mcr.microsoft.com/azure-functions/dotnet:4@sha256:351011b522690b6477b78e5e29c0044c1ab4ef536c0499cd377c85851f85ba54
ENV AzureWebJobsScriptRoot=/home/site/wwwroot

COPY --from=installer-env ["/home/site/wwwroot", "/home/site/wwwroot"]
