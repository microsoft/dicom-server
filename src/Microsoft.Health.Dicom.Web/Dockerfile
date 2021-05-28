#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["src/Microsoft.Health.Dicom.Web/Microsoft.Health.Dicom.Web.csproj", "src/Microsoft.Health.Dicom.Web/"]
COPY ["src/Microsoft.Health.Dicom.Metadata/Microsoft.Health.Dicom.Metadata.csproj", "src/Microsoft.Health.Dicom.Metadata/"]
COPY ["src/Microsoft.Health.Dicom.Core/Microsoft.Health.Dicom.Core.csproj", "src/Microsoft.Health.Dicom.Core/"]
COPY ["src/Microsoft.Health.Dicom.Blob/Microsoft.Health.Dicom.Blob.csproj", "src/Microsoft.Health.Dicom.Blob/"]
COPY ["src/Microsoft.Health.Dicom.SqlServer/Microsoft.Health.Dicom.SqlServer.csproj", "src/Microsoft.Health.Dicom.SqlServer/"]
COPY ["src/Microsoft.Health.Dicom.Api/Microsoft.Health.Dicom.Api.csproj", "src/Microsoft.Health.Dicom.Api/"]
RUN dotnet restore "src/Microsoft.Health.Dicom.Web/Microsoft.Health.Dicom.Web.csproj"
COPY . .
WORKDIR "/src/src/Microsoft.Health.Dicom.Web"
RUN dotnet build "Microsoft.Health.Dicom.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Microsoft.Health.Dicom.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Microsoft.Health.Dicom.Web.dll"]
