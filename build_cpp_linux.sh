#!/bin/bash

whisper_path="$1"
unity_project="$PWD"
build_path="$1/build"

clean_build(){
  rm -rf "$build_path"
  mkdir "$build_path"
  cd "$build_path"
}

build() {
  clean_build
  echo "Starting building..."

  cmake -DCMAKE_BUILD_TYPE=Release -DGGML_VULKAN=ON \
  -DWHISPER_BUILD_TESTS=OFF -DWHISPER_BUILD_EXAMPLES=OFF ../
  make

  echo "Build complete!"

  artifact_path="$build_path/src/libwhisper.so"
  target_path="$unity_project/Packages/com.whisper.unity/Plugins/Linux/libwhisper.so"
  cp "$artifact_path" "$target_path"

  artifact_path=$build_path/ggml/src
  target_path=$unity_project/Packages/com.whisper.unity/Plugins/Linux/
  cp "$artifact_path"/*.so "$target_path"
  cp "$artifact_path"/*/*.so "$target_path"

  echo "Build files copied to $target_path"
}

build
