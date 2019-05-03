FROM microsoft/dotnet:2.1-sdk AS build-env
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

RUN apt-get update && apt-get install -y git-core \
    sudo \
    build-essential \
    gcc \
    --no-install-recommends && \
    rm -rf /var/lib/apt/lists/*

RUN git clone git://git.drogon.net/wiringPi
RUN cd wiringPi && ./build

ENTRYPOINT ["dotnet", "PiTree.dll"]
