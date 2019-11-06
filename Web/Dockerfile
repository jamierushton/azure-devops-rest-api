FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["Perspicuity.AzureDevOps.WorkItems/Perspicuity.AzureDevOps.WorkItems.csproj", "Perspicuity.AzureDevOps.WorkItems/"]
RUN dotnet restore "Perspicuity.AzureDevOps.WorkItems/Perspicuity.AzureDevOps.WorkItems.csproj"
COPY . .
WORKDIR "/src/Perspicuity.AzureDevOps.WorkItems"
RUN dotnet build "Perspicuity.AzureDevOps.WorkItems.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Perspicuity.AzureDevOps.WorkItems.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Perspicuity.AzureDevOps.WorkItems.dll"]