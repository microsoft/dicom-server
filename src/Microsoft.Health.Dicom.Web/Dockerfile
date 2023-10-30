# See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Define the "runtime" image which will run the DICOM Server
FROM mcr.microsoft.com/dotnet/aspnet:7.0.13-alpine3.18-amd64@sha256:6e2d88b32914dbff11d8e2a7bc877e9684d880c67b29d47b3b8feafba3ba37a9 AS runtime
RUN set -x && \
    # See https://www.abhith.net/blog/docker-sql-error-on-aspnet-core-alpine/
    apk update && \
    apk add --no-cache icu-libs && \
    addgroup nonroot && \
    adduser -S -D -H -s /sbin/nologin -G nonroot -g nonroot nonroot
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_URLS=http://+:8080 \
    # Explicitly set locale to be en_US for DICOM Web
    LANG=en_US.UTF-8 \
    LC_ALL=en_US.UTF-8
EXPOSE 8080
USER nonroot

# Copy the DICOM Server project and build it
FROM mcr.microsoft.com/dotnet/sdk:7.0.403-alpine3.18-amd64@sha256:f141a56d45e6c325e724e68d3fbc4c76c62c2279281f538b17cf9b1d42fc9fa1 AS build
ARG BUILD_CONFIGURATION=Release
ARG CONTINUOUS_INTEGRATION_BUILD=false

# TODO: Remove once Swashbuckle has been updated for .NET 7
RUN set -x && \
    apk update && \
    apk add --no-cache bash && \
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin -Channel 6.0 -InstallDir /usr/share/dotnet

WORKDIR /dicom-server
COPY . .
WORKDIR /dicom-server/src/Microsoft.Health.Dicom.Web
RUN dotnet build "Microsoft.Health.Dicom.Web.csproj" -c $BUILD_CONFIGURATION -p:ContinuousIntegrationBuild=$CONTINUOUS_INTEGRATION_BUILD -warnaserror

# Publish the DICOM Server from the build
FROM build as publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Microsoft.Health.Dicom.Web.csproj" -c $BUILD_CONFIGURATION --no-build -o /app/publish

# Copy the published application
FROM runtime AS dicom-server
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Microsoft.Health.Dicom.Web.dll"]
