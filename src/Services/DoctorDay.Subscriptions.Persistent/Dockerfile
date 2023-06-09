#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/Services/DoctorDay.Subscriptions.Persistent/DoctorDay.Subscriptions.Persistent.csproj", "src/Services/DoctorDay.Subscriptions.Persistent/"]
COPY ["src/BuildingBlocks/JsonConverters/BuildingBlocks.JsonConverters.csproj", "src/BuildingBlocks/JsonConverters/"]
COPY ["src/Services/DoctorDay.Application/DoctorDay.Application.csproj", "src/Services/DoctorDay.Application/"]
COPY ["src/BuildingBlocks/Archivers/BuildingBlocks.Archivers.csproj", "src/BuildingBlocks/Archivers/"]
COPY ["src/Services/DoctorDay.Domain/DoctorDay.Domain.csproj", "src/Services/DoctorDay.Domain/"]
RUN dotnet restore "src/Services/DoctorDay.Subscriptions.Persistent/DoctorDay.Subscriptions.Persistent.csproj"
COPY . .
WORKDIR "/src/src/Services/DoctorDay.Subscriptions.Persistent"
RUN dotnet build "DoctorDay.Subscriptions.Persistent.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DoctorDay.Subscriptions.Persistent.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DoctorDay.Subscriptions.Persistent.dll"]