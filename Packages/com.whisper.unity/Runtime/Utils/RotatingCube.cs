using UnityEngine;

namespace Whisper.Utils
{
    /// <summary>
    /// Simple rotation script to check if Unity didn't hanged
    /// </summary>
    public class RotatingCube : MonoBehaviour
    {
        public float speed = 10f;
    
        private void Update()
        {
            transform.Rotate(Vector3.one * (Time.deltaTime * speed));
        }
    }
}

