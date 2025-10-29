# -----------------------------
# Etapa 1: Build (compilación)
# -----------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar los archivos del proyecto y restaurar dependencias
COPY ["CrudCloudDb.api.csproj", "./"]
RUN dotnet restore "CrudCloudDb.api.csproj"

# Copiar el resto del código y compilar
COPY . .
RUN dotnet build "CrudCloudDb.api.csproj" -c Release -o /app/build

# -----------------------------
# Etapa 2: Publish (publicación)
# -----------------------------
FROM build AS publish
RUN dotnet publish "CrudCloudDb.api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# -----------------------------
# Etapa 3: Runtime (ejecución)
# -----------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copiar archivos publicados desde la etapa anterior
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "CrudCloudDb.api.dll"]