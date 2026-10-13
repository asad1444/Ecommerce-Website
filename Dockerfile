FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY loginlogout.csproj ./
RUN dotnet restore loginlogout.csproj
COPY . ./
RUN dotnet publish loginlogout.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "loginlogout.dll"]
