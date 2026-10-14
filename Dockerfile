FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Project file copy karke dependencies restore karein
COPY loginlogout.csproj ./
RUN dotnet restore loginlogout.csproj

# Baqi saara source code copy karein
COPY . ./

# Yahan se --no-restore hata diya hai taake koi caching conflict na ho
RUN dotnet publish loginlogout.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "loginlogout.dll"]