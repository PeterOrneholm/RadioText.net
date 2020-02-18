#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["src/Orneholm.RadioText.Web/Orneholm.RadioText.Web.csproj", "Orneholm.RadioText.Web/"]
COPY ["src/Orneholm.RadioText.Core/Orneholm.RadioText.Core.csproj", "Orneholm.RadioText.Core/"]
COPY ["src/Orneholm.RadioText.Azure/Orneholm.RadioText.Azure.csproj", "Orneholm.RadioText.Azure/"]
RUN dotnet restore "Orneholm.RadioText.Web/Orneholm.RadioText.Web.csproj"
COPY /src .
WORKDIR "/src/Orneholm.RadioText.Web"
RUN dotnet build "Orneholm.RadioText.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Orneholm.RadioText.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Orneholm.RadioText.Web.dll"]
