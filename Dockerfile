FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5004

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Payments.API/Payments.API.csproj", "Payments.API/"]
# Only add GitHub source if credentials are provided
RUN if [ -n "$GITHUB_USER" ] && [ -n "$GITHUB_TOKEN" ]; then \
      dotnet nuget add source "https://nuget.pkg.github.com/YOUR_ORG/index.json" \
        --name github \
        --username "$GITHUB_USER" \
        --password "$GITHUB_TOKEN" \
        --store-password-in-clear-text; \
    else \
      echo "GitHub credentials not provided, skipping private source"; \
    fi
RUN dotnet restore "Payments.API/Payments.API.csproj"
COPY src/Payments.API/. Payments.API/
WORKDIR "/src/Payments.API"
RUN dotnet build "Payments.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Payments.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5004
ENTRYPOINT ["dotnet", "Payments.API.dll"]
