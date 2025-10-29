# -----------------------------
# Etapa 1: Build (compilación)
# -----------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Paso 3: Copia SÓLO el archivo .csproj (que está en la raíz del contexto)
# Docker buscará: ./CrudCloud.api.csproj
COPY ["CrudCloud.api.csproj", "./"] 
RUN dotnet restore "CrudCloud.api.csproj"

# Copiar el resto de los archivos (Controladores, Program.cs, etc.)
COPY . .
RUN dotnet build "CrudCloud.api.csproj" -c Release -o /app/build

# -----------------------------
# Etapa 2: Publish (publicación)
# -----------------------------
FROM build AS publish
RUN dotnet publish "CrudCloud.api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# -----------------------------
# Etapa 3: Runtime (ejecución)
# -----------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copiar archivos publicados desde la etapa anterior
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "CrudCloud.api.dll"]