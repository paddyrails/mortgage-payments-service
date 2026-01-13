FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5004

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Payments.API/Payments.API.csproj", "Payments.API/"]
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
