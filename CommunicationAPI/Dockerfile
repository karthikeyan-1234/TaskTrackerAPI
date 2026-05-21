FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the csproj file using its relative path from the root context
COPY ["CommunicationAPI/CommunicationAPI.csproj", "CommunicationAPI/"]
RUN dotnet restore "CommunicationAPI/CommunicationAPI.csproj"

# Copy the remaining source code
COPY . .
WORKDIR "/src/CommunicationAPI"
RUN dotnet publish "CommunicationAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CommunicationAPI.dll"]