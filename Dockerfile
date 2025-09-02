# Use the official .NET runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Publish the app
FROM build AS publish
COPY ["SemanticKernelApi/SemanticKernelApi.csproj", "./"]
RUN dotnet restore "SemanticKernelApi.csproj"
COPY ["SemanticKernelApi/", "./"]
RUN dotnet publish "SemanticKernelApi.csproj" -c Release -o /app/publish

# Build runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create a non-privileged user that the app will run under
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "SemanticKernelApi.dll"]