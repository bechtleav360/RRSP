#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

## RUN apt-get update
RUN apt-get update && apt-get install -y curl && \
    curl -SLO https://deb.nodesource.com/nsolid_setup_deb.sh && \
    chmod 500 nsolid_setup_deb.sh && \
    ./nsolid_setup_deb.sh 22 && \
    apt-get install -y nodejs && \
    npm install -g yarn && \
    rm -rf /var/lib/apt/lists/*

COPY ["Framework.tar", "/"]
RUN tar -xvf /Framework.tar

COPY ["Meros.tar", "/"]
RUN tar -xvf /Meros.tar

COPY ["RRSP/RRSP.csproj", "RRSP/"]
COPY ["RRSP/package.json", "RRSP/"]
COPY ["RRSP.Server/RRSP.Server.csproj", "RRSP.Server/"]
COPY ["RRSP.Server/package.json", "RRSP.Server/"]
COPY ["package.json", ""]
COPY ["yarn.lock", ""]
COPY [".npmrc", ""]

RUN ls

WORKDIR "/src/RRSP.Server"
RUN yarn install

WORKDIR /src
RUN dotnet restore "RRSP.Server/RRSP.Server.csproj"

COPY . .

ENV TZ=Europe/Berlin
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

WORKDIR "/src/RRSP.Server"
RUN yarn run build; 

FROM build AS publish
RUN dotnet publish "RRSP.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
RUN apt-get update && apt-get install -y libfontconfig1
RUN apt-get update && \
    apt-get install -y libgdiplus libfontconfig1 libgssapi-krb5-2 && \
    rm -rf /var/lib/apt/lists/*
COPY --from=publish /app/publish .
ENV TZ=Europe/Berlin
ENV FilesRoot=/app/files
ENV IsPostgres=true
ENTRYPOINT ["dotnet", "RRSP.Server.dll"]
