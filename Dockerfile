FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["BookClub/BookClub.csproj", "BookClub/"]
RUN dotnet restore "BookClub/BookClub.csproj"

COPY ["BookClub/", "BookClub/"]
WORKDIR /src/BookClub
RUN dotnet publish "BookClub.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet BookClub.dll"]
