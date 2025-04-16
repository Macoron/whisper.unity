#!/bin/bash

whisper_path="$1"
targets=${2:-all}
android_sdk_path="$3"
unity_project="$PWD"
build_path="$1/build"

clean_build(){
  rm -rf "$build_path"
  mkdir "$build_path"
  cd "$build_path"
}

build_mac() {
  clean_build
  echo "Starting building for Mac (Metal)..."

  cmake -DCMAKE_OSX_ARCHITECTURES="x86_64;arm64" -DGGML_METAL=ON -DCMAKE_BUILD_TYPE=Release  \
   -DWHISPER_BUILD_TESTS=OFF -DWHISPER_BUILD_EXAMPLES=OFF -DGGML_METAL_EMBED_LIBRARY=ON ../
  make

  echo "Build for Mac (Metal) complete!"

  rm $unity_project/Packages/com.whisper.unity/Plugins/MacOS/*.dylib

  artifact_path="$build_path/src/libwhisper.dylib"
  target_path="$unity_project/Packages/com.whisper.unity/Plugins/MacOS/libwhisper.dylib"
  cp "$artifact_path" "$target_path"

  artifact_path=$build_path/ggml/src
  target_path=$unity_project/Packages/com.whisper.unity/Plugins/MacOS/
  cp "$artifact_path"/*.dylib "$target_path"
  cp "$artifact_path"/*/*.dylib "$target_path"

  # Required by Unity to properly find the dependencies
  for file in "$target_path"*.dylib; do
    install_name_tool -add_rpath @loader_path $file
  done

  echo "Build files copied to $target_path"
}

build_ios() {
  clean_build
  echo "Starting building for ios..."

  cmake -DBUILD_SHARED_LIBS=OFF -DCMAKE_SYSTEM_NAME=iOS -DCMAKE_BUILD_TYPE=Release  \
  -DCMAKE_OSX_ARCHITECTURES="x86_64;arm64" -DGGML_METAL=ON \
  -DCMAKE_SYSTEM_PROCESSOR=arm64 -DCMAKE_IOS_INSTALL_COMBINED=YES \
  -DWHISPER_BUILD_TESTS=OFF -DWHISPER_BUILD_EXAMPLES=OFF ../
  make

  echo "Build for ios complete!"

  rm $unity_project/Packages/com.whisper.unity/Plugins/iOS/*.a

  artifact_path="$build_path/src/libwhisper.a"
  target_path="$unity_project/Packages/com.whisper.unity/Plugins/iOS/libwhisper.a"
  cp "$artifact_path" "$target_path"

  artifact_path=$build_path/ggml/src
  target_path=$unity_project/Packages/com.whisper.unity/Plugins/iOS/
  cp "$artifact_path"/*.a "$target_path"
  cp "$artifact_path"/*/*.a "$target_path"

  echo "Build files copied to $target_path"
}

build_android() {
  clean_build
  echo "Starting building for Android..."

  cmake -DCMAKE_TOOLCHAIN_FILE="$android_sdk_path" -DANDROID_ABI=arm64-v8a -DGGML_OPENMP=OFF -DBUILD_SHARED_LIBS=OFF \
  -DWHISPER_BUILD_TESTS=OFF -DWHISPER_BUILD_EXAMPLES=OFF -DCMAKE_BUILD_TYPE=Release ../
  make

  echo "Build for Android complete!"

  rm $unity_project/Packages/com.whisper.unity/Plugins/Android/*.a

  artifact_path="$build_path/src/libwhisper.a"
  target_path="$unity_project/Packages/com.whisper.unity/Plugins/Android/libwhisper.a"
  cp "$artifact_path" "$target_path"

  artifact_path=$build_path/ggml/src
  target_path=$unity_project/Packages/com.whisper.unity/Plugins/Android/
  cp "$artifact_path"/*.a "$target_path"
  cp "$artifact_path"/*/*.a "$target_path"

  echo "Build files copied to $target_path"
}

if [ "$targets" = "all" ]; then
  build_mac
  build_ios
  build_android
elif [ "$targets" = "mac" ]; then
  build_mac
elif [ "$targets" = "ios" ]; then
  build_ios
elif [ "$targets" = "android" ]; then
  build_android
else
  echo "Unknown targets: $targets"
fi
