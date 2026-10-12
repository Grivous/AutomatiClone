@echo off
rem Lance le prototype Automaticlone (compile le C# puis ouvre le jeu).
rem   Double-clic        -> jouer
rem   lancer.bat editeur -> ouvrir le projet dans l'editeur Godot
cd /d "%~dp0"
set GODOT=%USERPROFILE%\tools\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe

dotnet build godot\Automaticlone.csproj -v q -nologo
if errorlevel 1 (
    echo La compilation a echoue.
    pause
    exit /b 1
)

if "%1"=="editeur" (
    start "" "%GODOT%" --path godot --editor
) else (
    start "" "%GODOT%" --path godot
)
