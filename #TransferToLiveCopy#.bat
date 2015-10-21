@Echo off
cls
set debug=false
set scripturl=%~dp0
set scripturl=%scripturl:~0,-1%

:setinnerspacedotnetdirectory
if "%debug%"=="true" pause && echo ------------------------------------------ && echo ------------------------------------------
if exist ".\..\..\Questor\" set innerspacedotnetdirectory=.\..\..\Questor\
if not exist "%Innerspacedotnetdirectory%" goto :error

:CopyQuestor
@Echo.
@Echo Starting to copy EasyHook files from [.\DirectEVE\easyhook*.dll] to [.\output\]
@Echo on
copy /y ".\DirectEVE\easyhook*.dll" ".\output\"

@Echo.
@Echo Starting to copy debug files from [.\output\*.pdb] to [%innerspacedotnetdirectory%]
@Echo on
copy /y ".\output\*.pdb" "%innerspacedotnetdirectory%"
@Echo off
@Echo.
set RandomNumber=%Random%
@Echo Rename prior Quester.exe to Questor.exe.RandomNumber.questorold
ren "%innerspacedotnetdirectory%\Questor.exe" "Questor.exe.%RandomNumber%.questorold"
@Echo off
@Echo Rename prior Utility.dll to Utility.dll.RandomNumber.questorold
ren "%innerspacedotnetdirectory%\Utility.dll" "Utility.dll.%RandomNumber%.questorold"
@Echo off
@Echo.
@Echo.@Echo Trying to delete all .questorold files
Del /Q "%innerspacedotnetdirectory%\*.questorold"
@Echo off
@Echo.
@Echo Starting to copy DLL files from [.\output\*.dll] to [%innerspacedotnetdirectory%]
@Echo on
copy /y ".\output\*.dll" "%innerspacedotnetdirectory%"
@Echo off
@Echo.
@Echo Starting to copy EXE files from [.\output\*.exe] to [%innerspacedotnetdirectory%]
@Echo on
copy /y ".\output\*.exe" "%innerspacedotnetdirectory%"
@Echo.
@Echo off

if "%debug%"=="true" pause && echo ------------------------------------------ && echo ------------------------------------------
::
if "%debug%"=="true" Echo on

:CopyXMLConfigFiles
@Echo.
@Echo *** always copy the template settings.xml file to [%innerspacedotnetdirectory%]
@Echo on
copy /y ".\output\settings.xml" "%innerspacedotnetdirectory%"
@Echo off
@Echo.
@Echo *** always copy factions.xml file to [%innerspacedotnetdirectory%]
copy /y ".\output\factions.xml" "%innerspacedotnetdirectory%"
@Echo.
@Echo *** only copy ShipTargetValues.xml if one does not already exist (it contains targeting data)
if not exist "%innerspacedotnetdirectory%\ShipTargetValues.xml" copy /y ".\output\ShipTargetValues.xml" "%innerspacedotnetdirectory%"
@Echo.
@Echo *** only copy invtypes.xml if one does not already exist (it contains pricing data)
if not exist "%innerspacedotnetdirectory%\invtypes.xml" copy /y ".\output\invtypes.xml" "%innerspacedotnetdirectory%"
@Echo.
@Echo *** only copy InvIgnore.xml if one does not already exist (it contains invtypes that will not be sold by valuedump)
if not exist "%innerspacedotnetdirectory%\InvIgnore.xml" copy /y ".\output\InvIgnore.xml" "%innerspacedotnetdirectory%"
@Echo.
@Echo. 
@Echo *** only copy Skill_Prerequisites.xml if one does not already exist (it contains skill prerequisites which allows questor to determine which skills can be injected)
if not exist "%innerspacedotnetdirectory%\Skill_Prerequisites.xml" copy /y ".\output\Skill_Prerequisites.xml" "%innerspacedotnetdirectory%"
@Echo off

goto :done

:error
echo ------------------------------------------ && echo ------------------------------------------
echo Error
echo ------------------------------------------ && echo ------------------------------------------
:done
echo [done copying questor related files to: %innerspacedotnetdirectory%]
