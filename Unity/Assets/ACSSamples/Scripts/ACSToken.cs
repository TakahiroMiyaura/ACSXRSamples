// Copyright (c) 2023 Takahiro Miyaura
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ACSToken : MonoBehaviour
{
    [SerializeField]
    private string ACSFunctionURL;

    public string Token { get; private set; }

    public bool HasToken => !string.IsNullOrEmpty(Token);

    // Start is called before the first frame update
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
    }

    public IEnumerator GetToken()
    {
        // Create a new web request to the ACSFunctionURL
        var www = UnityWebRequest.Get(ACSFunctionURL);

        // Wait for the response to finish
        yield return www.SendWebRequest();

        // Make sure the request was successful
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            // Parse the response content as JSON using the SimpleJSON package
            var tokenInfo = JsonUtility.FromJson<TokenInfo>(www.downloadHandler.text);

            // Get the value of the "token" key in the JSON object
            Token = tokenInfo.token;

            // Set the return value to the token value
            Debug.Log("Token value: " + Token);
        }
    }

    [Serializable]
    public class TokenInfo
    {
        public string expiresOn;
        public string token;
    }
}