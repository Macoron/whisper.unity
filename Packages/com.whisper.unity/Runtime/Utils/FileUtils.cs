using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Whisper.Utils
{
    public static class FileUtils
    {
        /// <summary>
        /// Read file on any platform.
        /// </summary>
        public static byte[] ReadFile(string path)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return ReadFileWebRequest(path);
#else
            return File.ReadAllBytes(path);
#endif
        }
        
        /// <summary>
        /// Async read file on any platform.
        /// </summary>
        public static async Task<byte[]> ReadFileAsync(string path)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return await ReadFileWebRequestAsync(path);
#else
            return await ReadAllBytesAsync(path);
#endif
        }

        /// <summary>
        /// Read Android file using "web-request".
        /// </summary>
        public static byte[] ReadFileWebRequest(string path)
        {
            var request = UnityWebRequest.Get(path);
            request.SendWebRequest();
            
            while (!request.isDone) {}
            
            if (HasError(request))
            {
                Debug.LogError($"Error while opening weights at {path}!");
                if (!string.IsNullOrEmpty(request.error))
                    Debug.LogError(request.error);
                return null;
            }

            return request.downloadHandler.data;
        }
        
        /// <summary>
        /// Async read Android file using "web-request".
        /// </summary>
        public static async Task<byte[]> ReadFileWebRequestAsync(string path)
        {
            var request = UnityWebRequest.Get(path);
            request.SendWebRequest();

            while (!request.isDone)
                await Task.Yield();

            if (HasError(request))
            {
                Debug.LogError($"Error while opening weights at {path}!");
                if (!string.IsNullOrEmpty(request.error))
                    Debug.LogError(request.error);
                return null;
            }

            return request.downloadHandler.data;
        }

        // to suppress obsolete warning
        private static bool HasError(UnityWebRequest request)
        {
#if UNITY_2020_1_OR_NEWER
            return request.result != UnityWebRequest.Result.Success;
#else
            return request.isHttpError || request.isNetworkError;
#endif
        }
        
        // to support .NET Standard 2.0
        private static async Task<byte[]> ReadAllBytesAsync(string path)
        {
#if UNITY_2021_2_OR_NEWER 
            return await File.ReadAllBytesAsync(path);
#else
            var task = Task.Factory.StartNew(() => File.ReadAllBytes(path));
            return await task;
#endif
        }
    }
}