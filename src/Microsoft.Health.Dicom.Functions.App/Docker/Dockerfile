FROM mcr.microsoft.com/azure-functions/dotnet:4@sha256:f77fd3eabb810365c8811448c34131496da78e6403a4a0cf581ff5cea9352f51 AS az-func-runtime
ENV ASPNETCORE_URLS=http://+:8080 \
    AzureWebJobsScriptRoot=/home/site/wwwroot \
    LANG=en_US.UTF-8 \
    LC_ALL=en_US.UTF-8
RUN groupadd nonroot && \
    useradd -r -M -s /sbin/nologin -g nonroot -c nonroot nonroot && \
    chown -R nonroot:nonroot /azure-functions-host
USER nonroot
EXPOSE 8080

# Copy the DICOM Server repository and build the Azure Functions project
FROM mcr.microsoft.com/dotnet/sdk:7.0.201-alpine3.16-amd64@sha256:e3d3fab124dd773ddf5945cfcd24bb116d68598d33c597a0c192fe16defc7536 AS build
ARG BUILD_CONFIGURATION=Release
ARG CONTINUOUS_INTEGRATION_BUILD=false

# Azure Functions v4 targets .NET 6
RUN set -x && \
    apk update && \
    apk add --no-cache bash && \
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin -Channel 6.0 -InstallDir /usr/share/dotnet

WORKDIR /dicom-server
COPY . .
WORKDIR /dicom-server/src/Microsoft.Health.Dicom.Functions.App
RUN dotnet build "Microsoft.Health.Dicom.Functions.App.csproj" -c $BUILD_CONFIGURATION -p:ContinuousIntegrationBuild=$CONTINUOUS_INTEGRATION_BUILD -warnaserror

# Publish the Azure Functions from the build
FROM build as publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Microsoft.Health.Dicom.Functions.App.csproj" -c $BUILD_CONFIGURATION --no-build -o /home/site/wwwroot

# Copy the published application
FROM az-func-runtime AS dicom-az-func
WORKDIR /home/site/wwwroot
COPY --from=publish /home/site/wwwroot .