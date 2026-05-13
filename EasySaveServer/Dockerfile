FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EasySaveServer/EasySaveServer.csproj EasySaveServer/
COPY EasySave.Models/EasySave.Models.csproj EasySave.Models/
COPY EasySave.Services/EasySave.Services.csproj EasySave.Services/
COPY EasyLog/EasyLog.csproj EasyLog/

RUN dotnet restore EasySaveServer/EasySaveServer.csproj

COPY . .
RUN dotnet publish EasySaveServer/EasySaveServer.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

ENV EASYSAVE_DATA_DIR=/data
ENV EASYSAVE_SOCKET_PORT=9090

COPY --from=build /app/publish .

EXPOSE 9090
VOLUME ["/data"]

ENTRYPOINT ["dotnet", "EasySaveServer.dll"]
