FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

ARG PROJECT_NAME

FROM build AS publish
COPY ["${PROJECT_NAME}/${PROJECT_NAME}.csproj", "./"]
RUN dotnet restore "${PROJECT_NAME}.csproj"
COPY ["${PROJECT_NAME}/", "./"]
RUN dotnet publish "${PROJECT_NAME}.csproj" -c Release -o /app/publish

FROM base AS final
ARG PROJECT_NAME
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT sh -c "dotnet ${PROJECT_NAME}.dll"
