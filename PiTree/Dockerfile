FROM microsoft/dotnet:2.1-sdk AS build-env
COPY qemu-arm-static /usr/bin/
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY PiTree.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
# add IL Linker package
#RUN dotnet add package ILLink.Tasks -v 0.1.5-preview-1841731 -s https://dotnet.myget.org/F/dotnet-core/api/v3/index.json
RUN dotnet publish -c Release -o out /p:ShowLinkerSizeComparison=true

# Build runtime image
FROM microsoft/dotnet:2.1-runtime AS runtime
WORKDIR /app
COPY --from=build-env /app/out ./

ENTRYPOINT ["dotnet", "PiTree.dll"]