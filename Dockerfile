# -----------------------------
# Etapa 1: Build (compilación)
# -----------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar el archivo del proyecto usando la ruta relativa de la subcarpeta
COPY ["CrudCloud.api/CrudCloud.api.csproj", "./"] 
RUN dotnet restore "CrudCloud.api.csproj"

# Copiar el resto del código (incluyendo la subcarpeta CrudCloud.api) y compilar
COPY . .
# Usamos la ruta completa del archivo de proyecto dentro del WORKDIR (/src/CrudCloud.api/CrudCloud.api.csproj)
RUN dotnet build "CrudCloudCloud.api/CrudCloud.api.csproj" -c Release -o /app/build

# -----------------------------
# Etapa 2: Publish (publicación)
# -----------------------------
FROM build AS publish
# Usamos la ruta completa del archivo de proyecto para publicar
RUN dotnet publish "CrudCloud.api/CrudCloud.api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# -----------------------------
# Etapa 3: Runtime (ejecución)
# -----------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copiar archivos publicados desde la etapa anterior
COPY --from=publish /app/publish .

# El nombre del DLL (debería ser CrudCloud.api.dll basado en el csproj)
ENTRYPOINT ["dotnet", "CrudCloud.api.dll"]