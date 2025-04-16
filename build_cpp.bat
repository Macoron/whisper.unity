@echo off

set unity_path=%CD%
set whisper_path=%1

echo Starting building target...
cd %whisper_path%
rmdir .\build /s /q
cmake -S . -B ./build -A x64 -DGGML_VULKAN=ON -DCMAKE_BUILD_TYPE=Release -DWHISPER_BUILD_TESTS=OFF -DWHISPER_BUILD_EXAMPLES=OFF 

cd ./build
msbuild ALL_BUILD.vcxproj -t:build -p:configuration=Release -p:platform=x64

del %unity_path%\Packages\com.whisper.unity\Plugins\Windows\*.dll
xcopy /y /q .\bin\Release\ggml*.dll %unity_path%\Packages\com.whisper.unity\Plugins\Windows\
xcopy /y /q .\bin\Release\whisper.dll %unity_path%\Packages\com.whisper.unity\Plugins\Windows\libwhisper.dll*