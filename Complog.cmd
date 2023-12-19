rd /Q /S src\SourceIndexServer\bin\Debug\net6.0\index\complog
src\HtmlGenerator\bin\Debug\net472\HtmlGenerator.exe ..\complog\build.complog /out:testSite
move testSite\index src\SourceIndexServer\bin\Debug\net6.0\index\complog
REM pushd testSite
REM dotnet exec ..\src\SourceIndexServer\bin\Debug\net6.0\Microsoft.SourceBrowser.SourceIndexServer.dll
REM popd
