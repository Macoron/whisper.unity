using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class PostMethod : MonoBehaviour
{
    InputField outputArea;
    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Start()
    {
        outputArea = GameObject.Find("OutputArea").GetComponent<InputField>();
        GameObject.Find("PostButton").GetComponent<Button>().onClick.AddListener(PostData);
    }

    void PostData() => StartCoroutine(PostData_Coroutine());

    IEnumerator PostData_Coroutine()
    {
        outputArea.text = "Loading...";
        string url = "https://jsonplaceholder.typicode.com/posts";
        WWWForm form = new WWWForm();
        form.AddField("title", "test data");
        using(UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
                outputArea.text = request.error;
            else
                outputArea.text = request.downloadHandler.text;
        }
    }
}
