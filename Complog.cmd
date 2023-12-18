src\HtmlGenerator\bin\Debug\net472\HtmlGenerator.exe ..\complog\build.complog /out:testSite
pushd testSite
dotnet exec ..\src\SourceIndexServer\bin\Debug\net6.0\Microsoft.SourceBrowser.SourceIndexServer.dll
popd
