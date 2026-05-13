FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EasySaveServer/EasySaveServer.csproj EasySaveServer/
COPY EasySave.Models/EasySave.Models.csproj EasySave.Models/
COPY EasySave.Services/EasySave.Services.csproj EasySave.Services/
COPY EasyLog/EasyLog.csproj EasyLog/

RUN dotnet restore EasySaveServer/EasySaveServer.csproj

COPY . .
RUN dotnet publish EasySaveServer/EasySaveServer.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV EASYSAVE_DATA_DIR=/data

COPY --from=build /app/publish .

EXPOSE 8080
VOLUME ["/data"]

ENTRYPOINT ["dotnet", "EasySaveServer.dll"]
