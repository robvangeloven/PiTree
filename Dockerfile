FROM mcr.microsoft.com/dotnet/core/runtime:2.2-stretch-slim-arm32v7 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build
WORKDIR /src
# Copy all source projects
COPY PiTree/PiTree.csproj PiTree/
COPY PiTree.Monitor.AzureDevopsAPI/PiTree.Monitor.AzureDevopsAPI.csproj PiTree.Monitor.AzureDevopsAPI/
COPY PiTree.Interfaces/PiTree.Shared.csproj PiTree.Interfaces/
COPY PiTree.Output.GPIO/PiTree.Output.GPIO.csproj PiTree.Output.GPIO/
COPY PiTree.Monitor.ServiceBus/PiTree.Monitor.ServiceBus.csproj PiTree.Monitor.ServiceBus/
RUN dotnet restore PiTree/PiTree.csproj
# Copy everything else
COPY . .
WORKDIR /src/PiTree
RUN dotnet build -r linux-arm PiTree.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish -r linux-arm PiTree.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "PiTree.dll"]
