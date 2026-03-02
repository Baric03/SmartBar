FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/BarService/BarService.csproj", "src/BarService/"]
RUN dotnet restore "src/BarService/BarService.csproj"
COPY . .
WORKDIR "/src/src/BarService"
RUN dotnet build "BarService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BarService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update && apt-get install -y curl
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BarService.dll"]
