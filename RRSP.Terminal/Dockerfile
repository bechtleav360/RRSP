FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src


COPY ["Framework.tar","/"]
RUN tar -xvf /Framework.tar

COPY ["Meros.tar", "/"]
RUN tar -xvf /Meros.tar

COPY ["RRSP.Terminal/RRSP.Terminal.csproj", "RRSP.Terminal/"]
COPY ["RRSP/RRSP.csproj", "RRSP/"]

RUN dotnet restore "RRSP.Terminal/RRSP.Terminal.csproj"

COPY . .
ENV TZ=Europe/Berlin
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

FROM build AS publish
RUN dotnet publish "RRSP.Terminal/RRSP.Terminal.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY RRSP.Terminal/Migrations /Migrations
ENV MigrationDir=/Migrations
ENV AssetsDir=.
ENV IsPostgres=true
ENTRYPOINT ["dotnet", "RRSP.Terminal.dll"]
