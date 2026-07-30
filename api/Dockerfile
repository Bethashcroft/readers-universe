FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ReadersRealm.Api.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
ENV DOTNET_hostBuilder__reloadConfigOnChange=false
ENV DOTNET_gcServer=0
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "ReadersRealm.Api.dll"]
