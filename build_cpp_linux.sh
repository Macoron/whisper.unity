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

build_cpu() {
  clean_build
  echo "Starting building for CPU..."

  cmake -DCMAKE_BUILD_TYPE=Release \
  -DWHISPER_BUILD_TESTS=OFF -DWHISPER_BUILD_EXAMPLES=OFF ../
  make

  echo "Build for CPU complete!"

  artifact_path="$build_path/libwhisper.so"
  target_path="$unity_project/Packages/com.whisper.unity/Plugins/Linux/libwhisper.so"
  cp "$artifact_path" "$target_path"

  echo "Build files copied to $target_path"
}

build_cuda() {
  clean_build
  echo "Starting building for CUDA..."

  cmake -DWHISPER_CUBLAS=ON -DCMAKE_BUILD_TYPE=Release \
  -DWHISPER_BUILD_TESTS=OFF -DWHISPER_BUILD_EXAMPLES=OFF ../
  make

  echo "Build for CUDA complete!"

  artifact_path="$build_path/libwhisper.so"
  target_path="$unity_project/Packages/com.whisper.unity/Plugins/Linux/libwhisper_cuda.so"
  cp "$artifact_path" "$target_path"

  echo "Build files copied to $target_path"
}

if [ "$targets" = "all" ]; then
  build_cpu
  build_cuda
elif [ "$targets" = "cpu" ]; then
  build_cpu
elif [ "$targets" = "cuda" ]; then
  build_cuda
else
  echo "Unknown targets: $targets"
fi
