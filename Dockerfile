FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
# Copy qemu-arm-static to enable cross platform compiling:
COPY qemu-arm-static /usr/bin/
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
RUN dotnet build PiTree.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish PiTree.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "PiTree.dll"]
