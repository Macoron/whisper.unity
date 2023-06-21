using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Whisper.Utils
{
    public static class UiUtils
    {
        /// <summary>
        /// Scroll <see cref="ScrollRect"/> down to the bottom.
        /// </summary>
        public static async void ScrollDown(ScrollRect scroll)
        {
            await Task.Yield();
            Canvas.ForceUpdateCanvases ();
            scroll.normalizedPosition = new Vector2(0, 0);
            Canvas.ForceUpdateCanvases ();
        }
    }
}