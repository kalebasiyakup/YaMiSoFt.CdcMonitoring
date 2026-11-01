@echo off
setlocal
cd /d "%~dp0"
echo === Catalog check ===
py -3 scripts\generate-index.py . --check
set "INDEX_EXIT=%errorlevel%"
echo.
echo === Lint ===
py -3 scripts\vault-lint.py . --notes "wiki/*" --notes "INDEX.md" --notes "LOG.md" %*
set "LINT_EXIT=%errorlevel%"
echo.
if not "%INDEX_EXIT%"=="0" echo WARNING: catalog is out of date. Run: py -3 scripts\generate-index.py .
pause
if not "%INDEX_EXIT%"=="0" exit /b %INDEX_EXIT%
exit /b %LINT_EXIT%
