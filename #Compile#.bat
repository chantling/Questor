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
Echo Done building [ %nameofproject% ] - see above for any errors - 2 of 5 builds
%pause%

if not exist output mkdir output >>nul 2>>nul
:: Echo deleting old build from the output directory
del .\output\*.exe /Q >>nul 2>>nul
del .\output\*.dll /Q >>nul 2>>nul
del .\output\*.pdb /Q >>nul 2>>nul
del .\output\*.bak /Q >>nul 2>>nul
:: the files that match the file pattern below are created by dropbox occassionally
del ".\bin\release\* conflicted copy *.*" /Q >>nul 2>>nul

::
:: DO NOT delete the XMLs as this is the ONLY directory they exist in now. 
::
::del .\output\*.xml /Q >>nul 2>>nul

::
:: Eventually all EXEs and DLLs will be in the following common directory...
::
copy .\bin\%releasetype%\*.exe .\output\ >>nul 2>>nul
copy .\bin\%releasetype%\*.dll .\output\ >>nul 2>>nul
copy .\DirectEVE\*.dll .\output\ >>nul 2>>nul
if "%releasetype%"=="Debug" copy .\bin\%releasetype%\*.pdb .\output\ >>nul 2>>nul

::Echo Copying mostly static files...
::copy .\questor\invtypes.xml .\output\
::copy .\questor\ShipTargetValues.xml .\output\
::copy .\questor\factions.xml .\output\
::copy .\questor\settings.xml .\output\settings-template-rename-to-charactername.xml
Echo.
Echo use #TransferToLiveCopy#.bat to move the new build into place for testing 
Echo.
%pause%
