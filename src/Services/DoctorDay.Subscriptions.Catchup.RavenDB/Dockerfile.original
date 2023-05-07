#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/Services/DoctorDay.Subscriptions.Catchup.RavenDB/DoctorDay.Subscriptions.Catchup.RavenDB.csproj", "src/Services/DoctorDay.Subscriptions.Catchup.RavenDB/"]
RUN dotnet restore "src/Services/DoctorDay.Subscriptions.Catchup.RavenDB/DoctorDay.Subscriptions.Catchup.RavenDB.csproj"
COPY . .
WORKDIR "/src/src/Services/DoctorDay.Subscriptions.Catchup.RavenDB"
RUN dotnet build "DoctorDay.Subscriptions.Catchup.RavenDB.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DoctorDay.Subscriptions.Catchup.RavenDB.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DoctorDay.Subscriptions.Catchup.RavenDB.dll"]