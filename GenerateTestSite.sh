rm -rf src/SourceIndexServer/bin/Debug/net8.0/index/complog
dotnet build src/HtmlGenerator/HtmlGenerator.csproj
dotnet exec src/HtmlGenerator/bin/Debug/net8.0/HtmlGenerator.dll ../complog/build.complog /out:testSite /force
mkdir -p src/SourceIndexServer/bin/Debug/net8.0/index
mv testSite/index src/SourceIndexServer/bin/Debug/net8.0/index/complog