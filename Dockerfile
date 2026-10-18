# Imagem de cada serviço .NET. Uso: docker build --build-arg PROJETO=Pedidos.Api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJETO
WORKDIR /src
COPY backend/ .
RUN dotnet publish src/${PROJETO}/${PROJETO}.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
ARG PROJETO
ENV DLL=${PROJETO}.dll
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["sh", "-c", "dotnet $DLL"]
