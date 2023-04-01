using UnityEngine;

namespace Whisper.Utils
{
    public static class AudioUtils
    {
        /// <summary>
        /// Transform audio clip samples to mono with desired sample rate.
        /// </summary>
        public static float[] Preprocess(float[] src, int srcSampleRate, int srcChannelsCount, int dstSampleRate)
        {
            // TODO: this probably can be done in one loop (mono + resample)
            var ret = src;
            if (srcChannelsCount > 1)
                ret = ConvertToMono(src, srcChannelsCount);
            if (srcSampleRate != dstSampleRate)
                ret = ChangeSampleRate(ret, srcSampleRate, dstSampleRate);

            return ret;
        }

        /// <summary>
        /// Convert audio buffer to mono.
        /// </summary>
        public static float[] ConvertToMono(float[] src, int channelsCount)
        {
            var srcLength = src.Length;
            var monoLength = srcLength / channelsCount;
            var mono = new float[monoLength];

            for (var i = 0; i < monoLength; i++)
            {
                var sum = 0f;
                for (int j = 0; j < channelsCount; j++)
                    sum += src[i * channelsCount + j];

                mono[i] = sum / channelsCount;
            }

            return mono;
        }

        /// <summary>
        /// Resample audio buffer to new sample rate using linear interpolation.
        /// </summary>
        public static float[] ChangeSampleRate(float[] src, int srcSampleRate, int dstSampleRate)
        {
            var srcLen = src.Length;
            var srcLenSec = (float) srcLen / srcSampleRate;
            var dstLen = Mathf.RoundToInt(srcLenSec * dstSampleRate);
            var dst = new float[dstLen];
            
            for (var i = 0; i < dstLen; i++)
            {
                var index = (float)i / dstLen * srcLen;
                var low = Mathf.FloorToInt(index);
                var dif = index - low;

                if (low + 1 == srcLen)
                    dst[i] = src[low];
                else
                    dst[i] = Mathf.Lerp(src[low], src[low + 1], dif);
            }
            
            return dst;
        }

    }
}