FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY packages/HopFence.AspNetCore/ packages/HopFence.AspNetCore/
COPY src/HopFence.Core/ src/HopFence.Core/
COPY examples/aspnetcore-nginx/ examples/aspnetcore-nginx/

WORKDIR /app/examples/aspnetcore-nginx
RUN dotnet publish -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "Example.AspNetCoreNginx.dll"]
