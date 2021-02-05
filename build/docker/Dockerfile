# Start from the sdk image
FROM mcr.microsoft.com/dotnet/sdk:5.0.102-1-alpine3.12@sha256:fb1a43b50c7047e5f28e309268a8f5425abc9cb852124f6828dcb0e4f859a4a1 AS build

# Set the working directory to where the source is located
WORKDIR /src

# Copy everything
COPY ./ ./

# Execute a dotnet build and publish the web project
RUN dotnet build --configuration Release
RUN dotnet publish "./src/Microsoft.Health.Dicom.Web/Microsoft.Health.Dicom.Web.csproj" -c Release -o "/build" --no-build

# Build the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:5.0.2-alpine3.12@sha256:efd03222416e60b50eb86183a6018c698eebcf496dfa427711ebd7d5c50b427f AS runtime

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
