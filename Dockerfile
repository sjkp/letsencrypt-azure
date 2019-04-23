FROM microsoft/dotnet:2.1-sdk-alpine AS build
# Set the working directory witin the container
WORKDIR /src

# Copy all of the source files
COPY . .
#COPY LetsEncrypt.Azure.DotNetCore.sln /src/LetsEncrypt.Azure.DotNetCore.sln
RUN cd src && ls

# Restore all packages
RUN dotnet restore ./LetsEncrypt.Azure.DotNetCore.sln

# Build the source code
RUN dotnet build -c release ./LetsEncrypt.Azure.DotNetCore.sln

RUN dotnet publish -c release ./src/LetsEncrypt.Azure.Runner/LetsEncrypt.Azure.Runner.csproj


# Build runtime image
FROM microsoft/dotnet:2.1-aspnetcore-runtime-alpine AS app
WORKDIR /app
COPY --from=build /src/src/LetsEncrypt.Azure.Runner/bin/release/netcoreapp2.1/publish .

ENTRYPOINT ["dotnet", "LetsEncrypt.Azure.Runner.dll"]