SET TARGET_BASE=src\SourceIndexServer\bin\Debug\net8.0\.data
SET TARGET=%TARGET_BASE%\index\initial
rd /Q /S %TARGET_BASE%
dotnet build src\HtmlGenerator\HtmlGenerator.csproj
dotnet exec src\HtmlGenerator\bin\Debug\net8.0\HtmlGenerator.dll ..\complog\build.complog /out:%TARGET% /projectName:complog
copy source.json %TARGET_BASE%
REM pushd testSite
REM dotnet exec ..\src\SourceIndexServer\bin\Debug\net6.0\Microsoft.SourceBrowser.SourceIndexServer.dll
REM popd