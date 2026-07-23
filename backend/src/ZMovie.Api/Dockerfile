FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG BUILD_VERSION=local
WORKDIR /src
COPY ["Directory.Packages.props", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["src/ZMovie.Api/ZMovie.Api.csproj", "src/ZMovie.Api/"]
COPY ["src/ZMovie.Application/ZMovie.Application.csproj", "src/ZMovie.Application/"]
COPY ["src/ZMovie.Domain/ZMovie.Domain.csproj", "src/ZMovie.Domain/"]
COPY ["src/ZMovie.Infrastructure/ZMovie.Infrastructure.csproj", "src/ZMovie.Infrastructure/"]
COPY ["src/Aspire/ServiceDefaults/ZMovie.ServiceDefaults.csproj", "src/Aspire/ServiceDefaults/"]
RUN dotnet restore "src/ZMovie.Api/ZMovie.Api.csproj"
COPY . .
WORKDIR "/src/src/ZMovie.Api"
RUN dotnet build "./ZMovie.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build /p:SourceRevisionId=$BUILD_VERSION

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG BUILD_VERSION=local
RUN dotnet publish "./ZMovie.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:SourceRevisionId=$BUILD_VERSION

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ZMovie.Api.dll"]
