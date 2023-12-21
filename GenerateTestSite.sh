rm -rf src/SourceIndexServer/bin/Debug/net8.0/index
dotnet build src/HtmlGenerator/HtmlGenerator.csproj
dotnet exec src/HtmlGenerator/bin/Debug/net8.0/HtmlGenerator.dll ../complog/build.complog /out:src/SourceIndexServer/bin/Debug/net8.0/index/complog /force