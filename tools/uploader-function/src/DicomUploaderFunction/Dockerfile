FROM mcr.microsoft.com/dotnet/sdk:8.0.201-alpine3.18-amd64@sha256:ba7c7ce462015322e6f34e30e60055000e4e860fe805eb14623557fbe90ec60c AS installer-env

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
FROM mcr.microsoft.com/azure-functions/dotnet:4@sha256:125fc7d5f7b1aff5f2f5e5a1cde3318e89c745fb40e5c25df4670aa297cec756
ENV AzureWebJobsScriptRoot=/home/site/wwwroot

COPY --from=installer-env ["/home/site/wwwroot", "/home/site/wwwroot"]
