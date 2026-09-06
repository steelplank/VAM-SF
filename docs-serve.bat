@echo off
REM ============================================================
REM  VAM:SF docs - one-click MkDocs preview (isolated venv).
REM  Double-click this, or run it from a terminal. First run
REM  creates .venv and installs MkDocs; later runs just serve.
REM  Opens the site at http://127.0.0.1:8000  (Ctrl+C to stop)
REM ============================================================
setlocal
cd /d "%~dp0"

REM --- find any Python just to BOOTSTRAP the venv (py launcher preferred) ---
set "PY="
where py >nul 2>nul && set "PY=py"
if not defined PY ( where python >nul 2>nul && set "PY=python" )
if not defined PY (
  echo [!] No Python found on PATH. Install Python from https://www.python.org/downloads/
  echo     (tick "Add python.exe to PATH"^), then run this again.
  pause & exit /b 1
)

REM --- create the venv once ---
if not exist ".venv\Scripts\python.exe" (
  echo Creating virtual environment in .venv ...
  %PY% -m venv .venv || ( echo [!] venv creation failed. & pause & exit /b 1 )
)

set "VENVPY=.venv\Scripts\python.exe"

REM --- install / update MkDocs Material inside the venv ---
echo Installing MkDocs Material (first run only, takes a moment)...
"%VENVPY%" -m pip install --upgrade pip >nul 2>nul
"%VENVPY%" -m pip install "mkdocs-material>=9.7,<10" "mkdocs>=1.6,<2" || ( echo [!] install failed. & pause & exit /b 1 )

REM --- serve ---
echo.
echo  MkDocs running at  http://127.0.0.1:8000    (press Ctrl+C to stop)
echo.
"%VENVPY%" -m mkdocs serve
pause
