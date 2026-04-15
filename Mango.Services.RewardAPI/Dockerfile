FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 5023
EXPOSE 7011

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Mango.Services.RewardAPI/Mango.Services.RewardAPI.csproj", "Mango.Services.RewardAPI/"]
COPY ["Mango.MessageBus/Mango.MessageBus.csproj", "Mango.MessageBus/"]
RUN dotnet restore "Mango.Services.RewardAPI/Mango.Services.RewardAPI.csproj"
COPY . .
WORKDIR "/src/Mango.Services.RewardAPI"
RUN dotnet build "Mango.Services.RewardAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Mango.Services.RewardAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Mango.Services.RewardAPI.dll"]
