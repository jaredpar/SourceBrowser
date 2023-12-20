SET TARGET=src\SourceIndexServer\bin\Debug\net8.0\index\complog
rd /Q /S %TARGET%
dotnet build src\HtmlGenerator\HtmlGenerator.csproj
dotnet exec src\HtmlGenerator\bin\Debug\net8.0\HtmlGenerator.dll ..\complog\build.complog /out:%TARGET% /force
REM pushd testSite
REM dotnet exec ..\src\SourceIndexServer\bin\Debug\net6.0\Microsoft.SourceBrowser.SourceIndexServer.dll
REM popd