#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/azure-functions/dotnet:2.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-stretch AS build
WORKDIR /src
COPY ["src/Orneholm.RadioText.AzureFunctions/Orneholm.RadioText.AzureFunctions.csproj", "src/Orneholm.RadioText.AzureFunctions/"]
COPY ["src/Orneholm.RadioText.Core/Orneholm.RadioText.Core.csproj", "src/Orneholm.RadioText.Core/"]
COPY ["src/Orneholm.RadioText.Azure/Orneholm.RadioText.Azure.csproj", "src/Orneholm.RadioText.Azure/"]
RUN dotnet restore "src/Orneholm.RadioText.AzureFunctions/Orneholm.RadioText.AzureFunctions.csproj"
COPY . .
WORKDIR "/src/src/Orneholm.RadioText.AzureFunctions"
RUN dotnet build "Orneholm.RadioText.AzureFunctions.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Orneholm.RadioText.AzureFunctions.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/app