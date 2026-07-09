# Stage 1: Runtime base
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Stage 2: Build project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy toàn bộ code vào
COPY . .

# 🎯 ĐÃ SỬA: Chỉ định đích danh đường dẫn tới file Backend .csproj 
# Để ngăn nó tự động tìm và chạy nhầm sang file Frontend (.esproj)
RUN dotnet restore "EventNestBE/EventNestBE.csproj"
RUN dotnet build "EventNestBE/EventNestBE.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 3: Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "EventNestBE/EventNestBE.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: Final Image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EventNestBE.dll"]