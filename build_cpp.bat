@echo off

set unity_path=%CD%
set target=%1
set whisper_path=%2

IF "%target%"=="cpu" goto cpu
IF "%target%"=="cuda" goto cuda
IF "%target%"=="all" goto cpu

echo Unknown target %target%, should "cpu", "cuda" or "all"
goto commonexit

:cpu
	echo Starting building cpu target...
	cd %whisper_path%
	rmdir .\build /s /q
	cmake -S . -B ./build -A x64 -DCMAKE_BUILD_TYPE=Release -DWHISPER_BUILD_TESTS=OFF -DWHISPER_BUILD_EXAMPLES=OFF 

	cd ./build
	msbuild ALL_BUILD.vcxproj -t:build -p:configuration=Release -p:platform=x64
	xcopy /y /q .\bin\Release\whisper.dll %unity_path%\Packages\com.whisper.unity\Plugins\Windows\libwhisper.dll*

	IF NOT "%target%"=="all" goto commonexit
:cuda
	echo Starting building CUDA target...
	cd %whisper_path%
	rmdir .\build /s /q
	cmake -S . -B ./build -A x64 -DWHISPER_CUBLAS=ON -DCMAKE_BUILD_TYPE=Release -DWHISPER_BUILD_TESTS=OFF -DWHISPER_BUILD_EXAMPLES=OFF 

	cd ./build
	msbuild ALL_BUILD.vcxproj -t:build -p:configuration=Release -p:platform=x64
	xcopy /y /q .\bin\Release\whisper.dll %unity_path%\Packages\com.whisper.unity\Plugins\Windows\libwhisper_cuda.dll*

:commonexit