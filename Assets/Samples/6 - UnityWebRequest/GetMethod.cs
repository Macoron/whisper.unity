using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class GetMethod : MonoBehaviour
{
    InputField outputArea;
    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Start()
    {
        outputArea = GameObject.Find("OutputArea").GetComponent<InputField>();
        GameObject.Find("GetButton").GetComponent<Button>().onClick.AddListener(GetData);
    }

    void GetData() => StartCoroutine(GetData_Coroutine());

    IEnumerator GetData_Coroutine()
    {
        outputArea.text = "Loading...";
        string url = "https://jsonplaceholder.typicode.com/posts/1";
        using(UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
                outputArea.text = request.error;
            else
                outputArea.text = request.downloadHandler.text;
        }
    }
}
