@Echo off

:: 
set pause=pause
if "%1"=="/nopause" set pause=Echo.
::set releasetype=Release
set releasetype=Debug
::
:: path to msbuild compiler - do not include trailing slash
::
::set msbuild35=%systemroot%\Microsoft.Net\FrameWork\v3.5\msbuild.exe
set msbuild4=%systemroot%\Microsoft.Net\FrameWork\v4.0.30319\msbuild.exe
::

::
:: clear existing DLLs and EVEs from the previous build(s)
::
del ".\bin\debug\*.*" /Q
del ".\bin\release\*.*" /Q
::
:: Build Project: Questor
::
set nameofproject=Questor
set csproj=.\%nameofproject%\%nameofproject%.csproj
"%msbuild4%" "%csproj%" /p:configuration="%releasetype%" /target:Clean;Build
Echo Done building [ %nameofproject% ] - see above for any errors

if not exist output mkdir output >>nul 2>>nul
:: Echo deleting old build from the output directory
del .\output\*.exe /Q >>nul 2>>nul
del .\output\*.dll /Q >>nul 2>>nul
del .\output\*.pdb /Q >>nul 2>>nul
del .\output\*.bak /Q >>nul 2>>nul

::
:: Eventually all EXEs and DLLs will be in the following common directory...
::
copy .\bin\%releasetype%\*.exe .\output\ >>nul 2>>nul
copy .\bin\%releasetype%\*.dll .\output\ >>nul 2>>nul
copy .\libs\*.* .\output\ >>nul 2>>nul


if "%releasetype%"=="Debug" copy .\bin\%releasetype%\*.pdb .\output\ >>nul 2>>nul

%pause%
