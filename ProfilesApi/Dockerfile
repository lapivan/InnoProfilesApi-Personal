FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["ProfilesApi.API/ProfilesApi.API.csproj", "ProfilesApi.API/"]
COPY ["FakeFolder/ProfilesApi.Application.csproj", "ProfilesApi.Application/"]
COPY ["ProfilesApi.Domain/ProfilesApi.Domain.csproj", "ProfilesApi.Domain/"]
COPY ["ProfilesApi.Infrastructure/ProfilesApi.Infrastructure.csproj", "ProfilesApi.Infrastructure/"]

RUN dotnet restore "ProfilesApi.API/ProfilesApi.API.csproj"

COPY . .

WORKDIR "/src/ProfilesApi.API"
RUN dotnet publish "ProfilesApi.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ProfilesApi.API.dll"]