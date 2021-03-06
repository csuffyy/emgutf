REM @echo off
pushd %~p0
cd ..\..

SET TF_TYPE=FULL
IF "%1%"=="lite" SET TF_TYPE=LITE

IF "%2%"=="64" ECHO "BUILDING 64bit solution" 
IF "%2%"=="ARM" ECHO "BUILDING ARM solution"
IF "%2%"=="32" ECHO "BUILDING 32bit solution"

SET OS_MODE=
IF "%2%"=="64" SET OS_MODE= Win64
IF "%2%"=="ARM" SET OS_MODE= ARM

SET PROGRAMFILES_DIR_X86=%programfiles(x86)%
if NOT EXIST "%PROGRAMFILES_DIR_X86%" SET PROGRAMFILES_DIR_X86=%programfiles%
SET PROGRAMFILES_DIR=%programfiles%

REM Find CMake  
SET CMAKE="cmake.exe"
IF EXIST "%PROGRAMFILES_DIR_X86%\CMake 2.8\bin\cmake.exe" SET CMAKE="%PROGRAMFILES_DIR_X86%\CMake 2.8\bin\cmake.exe"
IF EXIST "%PROGRAMFILES_DIR_X86%\CMake\bin\cmake.exe" SET CMAKE="%PROGRAMFILES_DIR_X86%\CMake\bin\cmake.exe"
IF EXIST "%PROGRAMFILES_DIR%\CMake\bin\cmake.exe" SET CMAKE="%PROGRAMFILES_DIR%\CMake\bin\cmake.exe"
IF EXIST "%PROGRAMW6432%\CMake\bin\cmake.exe" SET CMAKE="%PROGRAMW6432%\CMake\bin\cmake.exe"

REM Find Visual Studio or Msbuild
FOR /F "tokens=* USEBACKQ" %%F IN (`miscellaneous\vswhere.exe -version [15.0^,16.0^) -property installationPath`) DO SET VS2017_DIR=%%F
SET VS2017="%VS2017_DIR%\Common7\IDE\devenv.com" 

FOR /F "tokens=* USEBACKQ" %%F IN (`miscellaneous\vswhere.exe -version [16.0^,17.0^) -property installationPath`) DO SET VS2019_DIR=%%F
SET VS2019="%VS2019_DIR%\Common7\IDE\devenv.com"

IF EXIST "%MSBUILD35%" SET DEVENV="%MSBUILD35%"
IF EXIST "%MSBUILD40%" SET DEVENV="%MSBUILD40%"

IF EXIST %VS2017% SET DEVENV=%VS2017%
IF EXIST %VS2019% SET DEVENV=%VS2019%


:SET_BUILD_TYPE
IF %DEVENV%=="%MSBUILD35%" SET BUILD_TYPE=/property:Configuration=Release
IF %DEVENV%=="%MSBUILD40%" SET BUILD_TYPE=/property:Configuration=Release
IF %DEVENV%==%VS2017% SET BUILD_TYPE=/Build Release
IF %DEVENV%==%VS2019% SET BUILD_TYPE=/Build Release

IF %DEVENV%=="%MSBUILD35%" SET CMAKE_CONF="Visual Studio 12 2005%OS_MODE%"
IF %DEVENV%=="%MSBUILD40%" SET CMAKE_CONF="Visual Studio 12 2005%OS_MODE%"
IF %DEVENV%==%VS2017% SET CMAKE_CONF="Visual Studio 15%OS_MODE%"
IF %DEVENV%==%VS2019% IF "%2%"=="64" SET CMAKE_CONF="Visual Studio 16" -A x64
IF %DEVENV%==%VS2019% IF "%2%"=="32" SET CMAKE_CONF="Visual Studio 16" -A Win32

REM build EMGU TF

IF "%3%"=="doc" ^
SET CMAKE_CONF_FLAGS=%CMAKE_CONF_FLAGS% -DEMGU_TF_DOCUMENTATION_BUILD:BOOL=TRUE 

IF NOT EXIST b mkdir b
cd b

%CMAKE% .. ^
-G %CMAKE_CONF% ^
%CMAKE_CONF_FLAGS% 

SET CMAKE_BUILD_TARGET=ALL_BUILD
IF NOT "%5%"=="package" GOTO CHECK_BUILD_TYPE
SET CMAKE_BUILD_TARGET=%CMAKE_BUILD_TARGET% PACKAGE

:CHECK_BUILD_TYPE
IF "%TF_TYPE%"=="LITE" goto BUILD_TF_LITE

:BUILD_TF_FULL
IF NOT "%3%"=="doc" GOTO BUILD_TF_FULL_NUGET
SET CMAKE_BUILD_TARGET=%CMAKE_BUILD_TARGET% Emgu.TF.Document.Html

:BUILD_TF_FULL_NUGET
IF NOT "%4%"=="nuget" GOTO BUILD
SET CMAKE_BUILD_TARGET=%CMAKE_BUILD_TARGET% Emgu.TF.Models.nuget Emgu.TF.Protobuf.nuget
GOTO BUILD

:BUILD_TF_LITE
IF NOT "%3%"=="doc" GOTO BUILD_TF_LITE_NUGET
SET CMAKE_BUILD_TARGET=%CMAKE_BUILD_TARGET% Emgu.TF.Lite.Document.Html 

:BUILD_TF_LITE_NUGET
IF NOT "%4%"=="nuget" GOTO BUILD
SET CMAKE_BUILD_TARGET=%CMAKE_BUILD_TARGET% Emgu.TF.Lite.Models.nuget Emgu.TF.Lite.nuget

:BUILD
ECHO BUILDING TARGETS: %CMAKE_BUILD_TARGET%
%CMAKE% --build . --config Release --target %CMAKE_BUILD_TARGET%

:END_OF_SCRIPT
popd