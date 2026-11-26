@echo off
echo Cerrando procesos de SITEL.Server bloqueados...
taskkill /F /IM SITEL.Server.exe /T 2>nul
taskkill /F /PID 24032 /T 2>nul
taskkill /F /IM dotnet.exe 2>nul
echo.
echo ========================================================
echo  Procesos cerrados con exito!
echo  Ahora puedes volver a Visual Studio y presionar:
echo  Compilar -> Recompilar solucion
echo ========================================================
pause
