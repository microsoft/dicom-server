# Start from the sdk image
FROM mcr.microsoft.com/dotnet/sdk:5.0.203-alpine3.12@sha256:4fe96d2e0ad763284c5a222efb5e7098c84c1bce8fcb18c2f896c547963de1a5 AS build

# Set the working directory to where the source is located
WORKDIR /src

# Copy everything
COPY ./ ./

# Execute a dotnet build and publish the web project
RUN dotnet build --configuration Release
RUN dotnet publish "./src/Microsoft.Health.Dicom.Web/Microsoft.Health.Dicom.Web.csproj" -c Release -o "/build" --no-build

# Build the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:5.0.5-alpine3.12@sha256:6809cda3436013c4e9d69df72c7b84f0943e97269ac0164c987895c4780b583d AS runtime

RUN set -x && \
    # See https://www.abhith.net/blog/docker-sql-error-on-aspnet-core-alpine/
    apk add --no-cache icu-libs && \
    addgroup nonroot && \
    adduser -S -D -H -s /sbin/nologin -G nonroot -g nonroot nonroot

# Set environment variables
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_URLS=http://+:8080

# Set the workiting directory and copy the build
WORKDIR /app
COPY --from=build /build .

# Expose and start the web app on port 8080
USER nonroot
EXPOSE 8080
ENTRYPOINT ["dotnet", "Microsoft.Health.Dicom.Web.dll"]
