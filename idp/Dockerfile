FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Kyvo.IDP.slnx ./
COPY Kyvo.IDP.Domain/Kyvo.IDP.Domain.csproj Kyvo.IDP.Domain/
COPY Kyvo.IDP.Application/Kyvo.IDP.Application.csproj Kyvo.IDP.Application/
COPY Kyvo.IDP.Infrastructure/Kyvo.IDP.Infrastructure.csproj Kyvo.IDP.Infrastructure/
COPY Kyvo.IDP.API/Kyvo.IDP.API.csproj Kyvo.IDP.API/
RUN dotnet restore Kyvo.IDP.API/Kyvo.IDP.API.csproj
COPY . .
RUN dotnet publish Kyvo.IDP.API/Kyvo.IDP.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Kyvo.IDP.API.dll"]
