# whisper.unity
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://opensource.org/licenses/MIT) [![whisper.cpp](https://img.shields.io/badge/whisper.cpp-v1.5.1-green)](https://github.com/ggerganov/whisper.cpp/releases/tag/v1.5.1)

[![Testing](https://github.com/Macoron/whisper.unity/actions/workflows/test.yml/badge.svg)](https://github.com/Macoron/whisper.unity/actions/workflows/test.yml)

This is Unity3d bindings for the [whisper.cpp](https://github.com/ggerganov/whisper.cpp). It provides high-performance inference of [OpenAI's Whisper](https://github.com/openai/whisper) automatic speech recognition (ASR) model running on your local machine.

> This repository comes with "ggml-tiny.bin" model weights. This is the smallest and fastest version of whisper model, but it has worse quality comparing to other models. If you want better quality, check out [other models weights](#downloading-other-model-weights).

**Main features:**
- Multilingual, supports around 60 languages
- Can translate one language to another (e.g. German speech to English text)
- Different models sizes offering speed and accuracy tradeoffs
- Runs on local users device without Internet connection
- Free and open source, can be used in commercial projects

**Supported platforms:**
- [x] Windows (x86_64, [optional CUDA](#cuda-support))
- [x] MacOS (Intel and ARM, [optional Metal](#metal-support))
- [x] Linux (x86_64, [optional CUDA](#cuda-support))
- [x] iOS (Device and Simulator)
- [x] Android (ARM64)
- [ ] WebGL (see [this issue](https://github.com/Macoron/whisper.unity/issues/20))
- [x] VisionOS 

## Samples

https://user-images.githubusercontent.com/6161335/231581911-446286fd-833e-40a2-94d0-df2911b22cad.mp4

*"whisper-small.bin" model tested in English, German and Russian from microphone*

https://user-images.githubusercontent.com/6161335/231584644-c220a647-028a-42df-9e61-5291aca3fba0.mp4

*"whisper-tiny.bin" model, 50x faster than realtime on Macbook with M1 Pro*

## Getting started
Clone this repository and open it as regular Unity project. It comes with examples and tiny multilanguage model weights.

Alternatively you can add this repository to your project as a **Unity Package**. Add it by this git URL to your Unity Package Manager:
```
https://github.com/Macoron/whisper.unity.git?path=/Packages/com.whisper.unity
```
### CUDA Support
> Unity project compiled with enabled CUDA expects your end-users to have Nvidia GPU and CUDA libraries. Trying to run build without it will result error.

To run inference with CUDA, you would need to have supported GPU and installed CUDA Toolkit (tested with [12.2.0](https://developer.nvidia.com/cuda-12-2-0-download-archive)). 

After that go to the **Project Settings => Whisper => Enable CUDA**. This should force package to use library compiled for CUDA.

### Metal Support
> Whisper.cpp supports Metal only on [Apple7 GPUs](https://developer.apple.com/documentation/metal/mtlgpufamily) family or newer (starting from Apple M1 chips). Trying to run on older hardware will fallback to CPU inference.

To activate Metal inference, go to **Project Settings => Whisper => Enable Metal**. This should force package to use library compiled for Metal.

### Downloading other model weights
You can try different Whisper model weights. For example, you can improve English language transcription by using English-only weights or by trying bigger models.

You can download model weights [from here](https://huggingface.co/ggerganov/whisper.cpp). Just put them into your `StreamingAssets` folder. 

For more information about models differences and formats read [whisper.cpp readme](https://github.com/ggerganov/whisper.cpp#ggml-format) and [OpenAI readme](https://github.com/openai/whisper#available-models-and-languages).

## Compiling C++ libraries from source
This project comes with prebuild libraries of whisper.cpp for all supported platforms. You can rebuild them from source using Github Actions. To do that make fork of this repo and go into `Actions => Build C++ => Run workflow`.  After pipeline completed, download compiled libraries in artifacts tab.

In case you want to build libraries on your machine:
1. Clone the original [whisper.cpp](https://github.com/ggerganov/whisper.cpp) repository
2. Checkout tag [v1.5.1](https://github.com/ggerganov/whisper.cpp/tree/v1.5.1). Other versions might not work with this Unity bindings.
3. Open whisper.unity folder with command line
4. If you are using **Windows** write:
```bash
.\build_cpp.bat cpu path\to\whisper
```
5. If you are using **MacOS** write:
```bash
sh build_cpp.sh path/to/whisper all path/to/ndk/android.toolchain.cmake
```
6. If you are using **Linux** write
```bash
sh build_cpp_linux.sh path/to/whisper cpu
```
7. If build was successful compiled libraries should be automatically update package `Plugins` folder. 
 
Windows will produce only Windows library, Linux will produce only Linux. MacOS will produce MacOS, iOS and Android libraries.

MacOS build script was tested on Mac with ARM processor. For Intel processors you might need change some parameters.

## License
This project is licensed under the MIT License. 

It uses compiled libraries and model weighs of [whisper.cpp](https://github.com/ggerganov/whisper.cpp) which is under MIT license.  

Original [OpenAI Whisper](https://github.com/openai/whisper) code and weights are also under MIT license.
