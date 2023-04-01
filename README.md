# whisper.unity
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://opensource.org/licenses/MIT)

This is Unity3d bindings for the [whisper.cpp](https://github.com/ggerganov/whisper.cpp). It provides high-performance inference of [OpenAI's Whisper](https://github.com/openai/whisper) automatic speech recognition (ASR) model running on your local machine.

> This repository comes with "ggml-tiny.bin" model weights. This is the smallest and fastest version of whisper model, but it has worse quality comparing to other models. If you want better quality, check out [other models weights](#downloading-other-model-weights).

**Supported platforms:**
- Windows (x86_64)
- MacOS (Intel and ARM)
- iOS (Device and Simulator)
- Android (ARM64, unstable, [see this issue](https://github.com/Macoron/whisper.unity/issues/2))

## Getting started
Clone this repository and open it as regular Unity project. It comes with examples and tiny multilanguage model weights.

Alternatively you can add this repository to your project as a **Unity Package**. Add it by this git URL to your Unity Package Manager:
```
https://github.com/Macoron/whisper.unity.git?path=/Packages/com.whisper.unity
```
### Downloading other model weights
You can try different Whisper model weights. For example, you can improve English language transcription by using English-only weights or by trying bigger models.

You can download model weights [from here](https://huggingface.co/datasets/ggerganov/whisper.cpp). Just put them into your `StreamingAssets` folder. 

For more information about models differences and formats read [whisper.cpp readme](https://github.com/ggerganov/whisper.cpp#ggml-format) and [OpenAI readme](https://github.com/openai/whisper#available-models-and-languages).

## Compiling C++ libraries from source
This project comes with prebuild libraries of whisper.cpp for all supported platforms. In case you want to build libraries yourself:
1. Clone the original [whisper.cpp](https://github.com/ggerganov/whisper.cpp) repository
2. Open whisper.unity folder with command line
3. If you are using **Windows** write:
```bash
.\build_cpp.bat path\to\whisper
```
4. If you are using **MacOS** write:
```bash
bash build_cpp.sh path/to/whisper all path/to/ndk/android.toolchain.cmake
```
5. If build was successful compiled libraries should be automatically update package `Plugins` folder. 
 
Windows will produce only Windows library. MacOS will produce MacOS, iOS and Android libraries.

MacOS build script was tested on Mac with ARM processor. For Intel processors you might need change some parameters.

## License
This project is licensed under the MIT License. 

It uses compiled libraries and model weighs of [whisper.cpp](https://github.com/ggerganov/whisper.cpp) which is under MIT license.  

Original [OpenAI Whisper](https://github.com/openai/whisper) code and weights are also under MIT license.
