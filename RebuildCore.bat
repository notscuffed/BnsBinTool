@ECHO OFF
setlocal EnableDelayedExpansion

SET output=D:\Nuget
SET src=%~dp0\Src
SET release=bin\Release

IF NOT "%NUGET_PACKAGES%"==""  (
    SET nuget_cache=%NUGET_PACKAGES%
) ELSE (
    SET nuget_cache=%userprofile%\.nuget\packages
)

echo Nuget cache location: %nuget_cache%

for %%l in (
    BnsBinTool.Core
) do (
    :: Clean old release
    echo [90m* Cleaning old release for [35m%%l[0m
    del /Q "%src%\%%l\%release%\*">nul 2>nul

    :: Clean nuget cache
    echo [90m* Cleaning nuget cache for [35m%%l[0m
    del /S /Q !nuget_cache!\%%l\>nul 2>nul

    :: Compile libary
    echo [90m* Compiling [35m%%l[0m
    dotnet pack --verbosity=quiet --configuration=Release "%src%\%%l">nul 2>nul
    if !ERRORLEVEL! NEQ 0 ( 
        echo [91mFailed to compile %%l[0m
        pause
        exit
    )
    
    :: Copy nuget package
    echo [90m* Copying [35m%%l[0m
    copy /Y "%src%\%%l\%release%\*.nupkg" "%output%">nul 2>nul
    if !ERRORLEVEL! NEQ 0 ( 
        echo [91mFailed to copy %%l to %output%. Does it exist?[0m
        pause
        exit
    )
)
pause