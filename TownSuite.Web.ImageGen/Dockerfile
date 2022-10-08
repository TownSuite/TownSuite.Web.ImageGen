FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TownSuite.Web.ImageGen/TownSuite.Web.ImageGen.csproj", "TownSuite.Web.ImageGen/"]
RUN dotnet restore "TownSuite.Web.ImageGen/TownSuite.Web.ImageGen.csproj"
COPY . .
WORKDIR "/src/TownSuite.Web.ImageGen"
RUN dotnet build "TownSuite.Web.ImageGen.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TownSuite.Web.ImageGen.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TownSuite.Web.ImageGen.dll"]
