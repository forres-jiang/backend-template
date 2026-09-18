FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src

COPY ["global.json", "."]
COPY ["Directory.Packages.props", "."]

COPY ["My.XXX.APIs/01My.XXX.APIs.csproj", "My.XXX.APIs/"]
COPY ["My.XXX.Service/02My.XXX.Service.csproj", "My.XXX.Service/"]
COPY ["My.XXX.Data/03My.XXX.Data.csproj", "My.XXX.Data/"]
COPY ["My.XXX.Infra/04My.XXX.Infra.csproj", "My.XXX.Infra/"]

RUN dotnet restore "My.XXX.APIs/01My.XXX.APIs.csproj"

COPY . .

WORKDIR "/src/My.XXX.APIs"

RUN dotnet build "01My.XXX.APIs.csproj" -c Release -o /app/build

FROM build AS publish

RUN dotnet publish "01My.XXX.APIs.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base

RUN apk add --no-cache icu-libs icu-data-full tzdata

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

WORKDIR /app

COPY --from=publish /app/publish .

RUN chown -R app:app /app \
	&& chmod -R 755 /app

EXPOSE 8080

USER app

ENTRYPOINT ["dotnet", "My.XXX.APIs.dll"]
