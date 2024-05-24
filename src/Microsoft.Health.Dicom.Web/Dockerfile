# See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Define the "runtime" image which will run the DICOM Server
FROM mcr.microsoft.com/dotnet/aspnet:8.0.5-alpine3.18-amd64@sha256:e962ea2d86bf743dee76556cd56d79a3592770c7e83dda84fe55005f6164a749 AS runtime
RUN set -x && \
    # See https://www.abhith.net/blog/docker-sql-error-on-aspnet-core-alpine/
    apk update && \
    apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_URLS=http://+:8080 \
    # Explicitly set locale to be en_US for DICOM Web
    LANG=en_US.UTF-8 \
    LC_ALL=en_US.UTF-8
EXPOSE 8080
USER $APP_UID

# Copy the DICOM Server project and build it
FROM mcr.microsoft.com/dotnet/sdk:8.0.300-alpine3.18-amd64@sha256:f6e0972b79c63c4307b849e039767c4f3780b87e42c5d78d9abbda86ced97fdb AS build
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
