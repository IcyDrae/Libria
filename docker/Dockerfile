FROM mcr.microsoft.com/dotnet/sdk:9.0
WORKDIR /app

COPY Libria.csproj ./
RUN dotnet restore Libria.csproj

EXPOSE 8080
ENTRYPOINT ["dotnet", "watch", "run", "--urls", "http://0.0.0.0:8080"]