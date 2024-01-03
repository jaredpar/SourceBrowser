FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /App

# Copy everything
COPY . ./

# Restore as distinct layers
RUN dotnet restore

# Build and publish the html generator
RUN dotnet publish -c Release -o out/generator src/HtmlGenerator/HtmlGenerator.csproj

RUN dotnet publish -c Release -o out/web src/SourceIndexServer/SourceIndexServer.csproj

# Build runtime image
# FROM mcr.microsoft.com/dotnet/aspnet:8.0
# Right now still need to use an SDK image because the HtmlGenerator unconditionally calls 
# MSBuildLocator that requires an SDK.
FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /App
COPY --from=build-env /App/out .

# Build up the initial index
RUN mkdir -p /App/web/.data/sources/console
RUN mkdir -p /App/web/.data/index
COPY ./testSite/msbuild.complog /App/web/.data/sources/console
COPY sources.json /App/web/.data
RUN dotnet exec generator/HtmlGenerator.dll /App/web/.data/sources/console/msbuild.complog /out:/App/web/.data/index/initial /force

# Start the browser
WORKDIR /App/web
ENV HtmlGeneratorFilePath=/App/generator/HtmlGenerator.dll
ENTRYPOINT ["dotnet", "exec", "Microsoft.SourceBrowser.SourceIndexServer.dll"]