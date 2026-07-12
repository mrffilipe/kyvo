FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Kyvo.slnx ./
COPY Kyvo.Domain/Kyvo.Domain.csproj Kyvo.Domain/
COPY Kyvo.Application/Kyvo.Application.csproj Kyvo.Application/
COPY Kyvo.Infrastructure/Kyvo.Infrastructure.csproj Kyvo.Infrastructure/
COPY Kyvo.API/Kyvo.API.csproj Kyvo.API/
RUN dotnet restore Kyvo.API/Kyvo.API.csproj
COPY . .
RUN dotnet publish Kyvo.API/Kyvo.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Kyvo.API.dll"]
