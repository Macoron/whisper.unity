#!/bin/bash

whisper_path="$1"
targets=${2:-all}
unity_project="$PWD"
build_path="$1/build"

clean_build(){
  rm -rf "$build_path"
  mkdir "$build_path"
  cd "$build_path"
}

build_linux() {
  clean_build
  echo "Starting building for Linux..."

  cmake -DCMAKE_BUILD_TYPE=Release \
  -DWHISPER_BUILD_TESTS=OFF -DWHISPER_BUILD_EXAMPLES=OFF ../
  make

  echo "Build for Linux complete!"

  artifact_path="$build_path/libwhisper.so"
  target_path="$unity_project/Packages/com.whisper.unity/Plugins/Linux/libwhisper.so"
  cp "$artifact_path" "$target_path"

  echo "Build files copied to $target_path"
}

build_linux