
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY DeveloperNotes/DeveloperNotes.csproj DeveloperNotes/
RUN dotnet restore DeveloperNotes/DeveloperNotes.csproj

COPY . .
WORKDIR /src/DeveloperNotes

RUN dotnet publish DeveloperNotes.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "DeveloperNotes.dll"]
