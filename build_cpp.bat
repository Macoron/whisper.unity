set MY_PATH=%CD%

cd %1
rmdir .\build /s /q
cmake -S . -B ./build -A x64 -DCMAKE_BUILD_TYPE=Release

cd ./build
msbuild ALL_BUILD.vcxproj -t:build -p:configuration=Release -p:platform=x64
xcopy /y /q .\bin\Release\whisper.dll %MY_PATH%\Packages\com.whisper.unity\Plugins\Windows\libwhisper.dll*