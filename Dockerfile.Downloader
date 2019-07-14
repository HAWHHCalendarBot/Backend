FROM mcr.microsoft.com/dotnet/core/sdk:2.1
ENV project Downloader

COPY CalendarBackendLib /app/CalendarBackendLib

WORKDIR /app/${project}

COPY ${project}/${project}.csproj ./
RUN dotnet restore

COPY ${project}/ ./
RUN dotnet publish -c Release -o /app/out


FROM mcr.microsoft.com/dotnet/core/runtime:2.1-alpine
WORKDIR /app
VOLUME /app/eventfiles

COPY --from=0 /app/out ./

ENTRYPOINT ["dotnet", "Downloader.dll"]
