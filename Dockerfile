# Etapa 1: build e publish (imagem com o SDK completo)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY BibliotecaApi/BibliotecaApi.csproj BibliotecaApi/
RUN dotnet restore BibliotecaApi/BibliotecaApi.csproj

COPY BibliotecaApi/ BibliotecaApi/
RUN dotnet publish BibliotecaApi/BibliotecaApi.csproj -c Release -o /app/publish --no-restore

# Etapa 2: imagem final, só com o runtime ASP.NET (menor, sem SDK)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Banco SQLite fica numa pasta própria para poder virar volume
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/biblioteca.db"
RUN mkdir -p /app/data

# 8080 = REST (HTTP/1.1) | 8081 = gRPC (HTTP/2)
EXPOSE 8080 8081

ENTRYPOINT ["dotnet", "BibliotecaApi.dll"]
