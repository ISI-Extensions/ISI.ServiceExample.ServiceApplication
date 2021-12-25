#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["ISI.ServiceExample.ServiceApplication/ISI.ServiceExample.ServiceApplication.csproj", "ISI.ServiceExample.ServiceApplication/"]
COPY ["ISI.ServiceExample.Service/ISI.ServiceExample.Service.csproj", "ISI.ServiceExample.Service/"]
COPY ["ISI.ServiceExample/ISI.ServiceExample.csproj", "ISI.ServiceExample/"]
COPY ["ISI.Services.ServiceExample/ISI.Services.ServiceExample.csproj", "ISI.Services.ServiceExample/"]
COPY ["ISI.ServiceExample.Api/ISI.ServiceExample.Api.csproj", "ISI.ServiceExample.Api/"]
COPY ["ISI.ServiceExample.Repository/ISI.ServiceExample.Repository.csproj", "ISI.ServiceExample.Repository/"]
COPY ["ISI.ServiceExample.Repository.SqlServer/ISI.ServiceExample.Repository.SqlServer.csproj", "ISI.ServiceExample.Repository.SqlServer/"]
RUN dotnet restore "ISI.ServiceExample.ServiceApplication/ISI.ServiceExample.ServiceApplication.csproj"
COPY . .
WORKDIR "/src/ISI.ServiceExample.ServiceApplication"
RUN dotnet build "ISI.ServiceExample.ServiceApplication.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ISI.ServiceExample.ServiceApplication.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ISI.ServiceExample.ServiceApplication.dll"]