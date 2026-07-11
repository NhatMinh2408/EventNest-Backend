# Sử dụng hình ảnh .NET 10 chính thức từ Microsoft
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy toàn bộ source code vào container
COPY . ./

# Restore các gói NuGet và build ứng dụng
RUN dotnet restore
RUN dotnet publish -c Release -o out

# Chạy ứng dụng
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "EventNestBE.dll"]