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

                 if (low + 1 >= srcLen)
                    dst[i] = src[srcLen - 1];
                else
                    dst[i] = Mathf.Lerp(src[low], src[low + 1], dif);
            }
            
            return dst;
        }

        /// <summary>
        /// Naive energy based Voice Activity Detection (VAD). Returns true if lastSec contains speech.
        /// </summary>
        public static bool SimpleVad(float[] data, int sampleRate, float lastSec, float vadThd, float freqThd)
        {
            // https://github.com/ggerganov/whisper.cpp/blob/a792c4079ce61358134da4c9bc589c15a03b04ad/examples/common.cpp#L697
            var nSamples = data.Length;
            var nSamplesLast = (int) (sampleRate * lastSec);
            
            if (nSamplesLast >= nSamples) 
            {
                // not enough samples - assume no speech
                return false;
            }
            
            if (freqThd > 0.0f) 
                HighPassFilter(data, freqThd, sampleRate);
            
            var energyAll = 0.0f;
            var energyLast = 0.0f;
            
            for (var i = 0; i < nSamples; i++) 
            {
                energyAll += Mathf.Abs(data[i]);

                if (i >= nSamples - nSamplesLast) 
                    energyLast += Mathf.Abs(data[i]);
            }
            
            energyAll /= nSamples;
            energyLast /= nSamplesLast;
            
            return energyLast >  vadThd * energyAll;
        }
        
        /// <summary>
        /// Return a copy of array after high pass filter.
        /// </summary>
        public static void HighPassFilter(float[] data, float cutoff, int sampleRate)
        {
            // https://github.com/ggerganov/whisper.cpp/blob/a792c4079ce61358134da4c9bc589c15a03b04ad/examples/common.cpp#L684
            if (data.Length == 0)
                return;

            var rc = 1.0f / (2.0f * Mathf.PI * cutoff); 
            var dt = 1.0f / sampleRate;
            var alpha = dt / (rc + dt);
            
            var y = data[0];
            for (var i = 1; i < data.Length; i++) 
            {
                y = alpha * (y + data[i] - data[i - 1]);
                data[i] = y;
            }
        }

    }
}