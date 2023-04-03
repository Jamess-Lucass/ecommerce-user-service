# Builder
FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS builder

WORKDIR /src
COPY ./src .

RUN dotnet publish user-service.csproj -c Release -o /app

# Final
FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine AS final

# Needed if using sql server
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

WORKDIR /app
COPY --from=builder /app .
ENTRYPOINT [ "dotnet", "user-service.dll" ]