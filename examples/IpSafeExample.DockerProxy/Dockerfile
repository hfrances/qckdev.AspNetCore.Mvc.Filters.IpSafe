# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore "examples/IpSafeExample.DockerProxy/IpSafeExample.DockerProxy.csproj" -p:TargetFramework=net8.0
RUN dotnet publish "examples/IpSafeExample.DockerProxy/IpSafeExample.DockerProxy.csproj" -f net8.0 -c Release -o /app/publish /p:UseAppHost=false /p:TargetFramework=net8.0 /p:TargetFrameworks=net8.0

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "IpSafeExample.DockerProxy.dll"]
