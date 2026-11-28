# ASP.NET API image for ECS Fargate (SignalR hub /hubs/chat). The same code runs on Lambda for REST.
# Build (from this folder):  docker buildx build --platform linux/arm64 -t student-market-api .
# The SDK stage runs on the build machine's own architecture and cross-publishes for the target,
# so building an ARM64 image on an x64 PC needs no emulation.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
WORKDIR /src

# Restore first (cached while only source files change).
COPY StudentFleaMarket.Domain/StudentFleaMarket.Domain.csproj StudentFleaMarket.Domain/
COPY StudentFleaMarket.Application/StudentFleaMarket.Application.csproj StudentFleaMarket.Application/
COPY StudentFleaMarket.Infrastructure/StudentFleaMarket.Infrastructure.csproj StudentFleaMarket.Infrastructure/
COPY StudentFleaMarket.API/StudentFleaMarket.API.csproj StudentFleaMarket.API/
RUN dotnet restore StudentFleaMarket.API/StudentFleaMarket.API.csproj -a $TARGETARCH

COPY StudentFleaMarket.Domain/ StudentFleaMarket.Domain/
COPY StudentFleaMarket.Application/ StudentFleaMarket.Application/
COPY StudentFleaMarket.Infrastructure/ StudentFleaMarket.Infrastructure/
COPY StudentFleaMarket.API/ StudentFleaMarket.API/
RUN dotnet publish StudentFleaMarket.API/StudentFleaMarket.API.csproj -c Release -a $TARGETARCH \
      --no-restore --self-contained false -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_gcServer=0
EXPOSE 8080
# Non-root user shipped with the .NET 8 images.
USER app
ENTRYPOINT ["dotnet", "StudentFleaMarket.API.dll"]
