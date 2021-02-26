# Start from the sdk image
FROM mcr.microsoft.com/dotnet/sdk:5.0.103-alpine3.12@sha256:3e31dfc4639dfc80fe38f40496eff7541f574edd1248b53eca7334f884cd5d4d AS build

# Set the working directory to where the source is located
WORKDIR /src

# Copy everything
COPY ./ ./

# Execute a dotnet build and publish the web project
RUN dotnet build --configuration Release
RUN dotnet publish "./src/Microsoft.Health.Dicom.Web/Microsoft.Health.Dicom.Web.csproj" -c Release -o "/build" --no-build

# Build the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:5.0.3-alpine3.12@sha256:5de46fc04b1dc08994ea810d427536cbb2491604768cf665d2001634181937ab AS runtime

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
