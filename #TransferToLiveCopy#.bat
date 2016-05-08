@Echo off
cls
set debug=false
set scripturl=%~dp0
set scripturl=%scripturl:~0,-1%

:setquestorDirectory
if "%debug%"=="true" pause && echo ------------------------------------------ && echo ------------------------------------------
if exist ".\..\..\Questor\" set questorDirectory=.\..\..\Questor\
if not exist "%questorDirectory%" goto :error

:CopyQuestor
@Echo.
@Echo Starting to copy libsfiles from [.\libs\*.*] to [.\output\]
@Echo on
copy /y ".\libs\*.*" ".\output\"

@Echo.
@Echo Starting to copy debug files from [.\output\*.pdb] to [%questorDirectory%]
@Echo on
copy /y ".\output\*.pdb" "%questorDirectory%"
@Echo off
@Echo.
set RandomNumber=%Random%
@Echo Rename prior Quester.exe to Questor.exe.RandomNumber.questorold
ren "%questorDirectory%\Questor.exe" "Questor.exe.%RandomNumber%.questorold"
@Echo off
@Echo Rename prior Utility.dll to Utility.dll.RandomNumber.questorold
ren "%questorDirectory%\Utility.dll" "Utility.dll.%RandomNumber%.questorold"
@Echo off
@Echo.
@Echo.@Echo Trying to delete all .questorold files
Del /Q "%questorDirectory%\*.questorold"
@Echo off
@Echo.
@Echo Starting to copy DLL files from [.\output\*.dll] to [%questorDirectory%]
@Echo on
copy /y ".\output\*.dll" "%questorDirectory%"
@Echo off
@Echo.
@Echo Starting to copy EXE files from [.\output\*.exe] to [%questorDirectory%]
@Echo on
copy /y ".\output\*.exe" "%questorDirectory%"
@Echo.
@Echo off

if "%debug%"=="true" pause && echo ------------------------------------------ && echo ------------------------------------------
::
if "%debug%"=="true" Echo on

:CopyXMLConfigFiles
@Echo.
@Echo *** always copy the template settings.xml file to [%questorDirectory%]
@Echo on
copy /y ".\output\settings.xml" "%questorDirectory%"
@Echo off
@Echo.
@Echo *** always copy factions.xml file to [%questorDirectory%]
copy /y ".\output\factions.xml" "%questorDirectory%"
@Echo.
@Echo *** only copy ShipTargetValues.xml if one does not already exist (it contains targeting data)
if not exist "%questorDirectory%\ShipTargetValues.xml" copy /y ".\output\ShipTargetValues.xml" "%questorDirectory%"
@Echo.
@Echo *** only copy invtypes.xml if one does not already exist (it contains pricing data)
if not exist "%questorDirectory%\invtypes.xml" copy /y ".\output\invtypes.xml" "%questorDirectory%"
@Echo.
@Echo *** only copy InvIgnore.xml if one does not already exist (it contains invtypes that will not be sold by valuedump)
if not exist "%questorDirectory%\InvIgnore.xml" copy /y ".\output\InvIgnore.xml" "%questorDirectory%"
@Echo.
@Echo. 
@Echo *** only copy Skill_Prerequisites.xml if one does not already exist (it contains skill prerequisites which allows questor to determine which skills can be injected)
if not exist "%questorDirectory%\Skill_Prerequisites.xml" copy /y ".\output\Skill_Prerequisites.xml" "%questorDirectory%"
@Echo off

goto :done

:error
echo ------------------------------------------ && echo ------------------------------------------
echo Error
echo ------------------------------------------ && echo ------------------------------------------
:done
echo [done copying questor related files to: %questorDirectory%]
