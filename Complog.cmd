
rd /Q /S src\SourceIndexServer\bin\Debug\net8.0\index\complog
dotnet build src\HtmlGenerator\HtmlGenerator.csproj
src\HtmlGenerator\bin\Debug\net472\HtmlGenerator.exe ..\complog\build.complog /out:testSite /force
mkdir src\SourceIndexServer\bin\Debug\net8.0\index
move testSite\index src\SourceIndexServer\bin\Debug\net8.0\index\complog
REM pushd testSite
REM dotnet exec ..\src\SourceIndexServer\bin\Debug\net6.0\Microsoft.SourceBrowser.SourceIndexServer.dll
REM popd
