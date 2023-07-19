// Copyright (c) 2023 Takahiro Miyaura
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php

using System.Collections;
using Microsoft.MixedReality.Toolkit.UX;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

public class TeamsCallingSamples : MonoBehaviour
{

    #region serialized feilds
    [SerializeField]
    private string teamsLink;

    [SerializeField]
    [Tooltip("User token to join the call.\r\nYou can get this from the Azure Communication Services Function App(ACSToken).\r\nIf you don't create Function App, you set nothing to Token Provider and you can input one created from the Azure Portal.")]
    private string userToken;

    [SerializeField]
    private ACSToken tokenProvider;

    [SerializeField]
    private TextMeshProUGUI statusText;

    [SerializeField]
    private MRTKUGUIInputField userName;
    #endregion

    #region private feilds

    private AzureCommunicationUWPPlugin _communicationUWPPlugin;

    #endregion

    #region public methods

    public void JoinCall()
    {
        if (string.IsNullOrEmpty(teamsLink))
        {
            Debug.LogError("teamsLink is IsNullOrEmpty ");
            return ;
        }

        if (userToken == null)
        {
            Debug.LogError("User token is null, please assign one");
            return ;
        }

        if (userName.text == null)
        {
            Debug.LogError("User name is null, please assign one");
            return ;
        }

        _communicationUWPPlugin ??= new AzureCommunicationUWPPlugin();
        _communicationUWPPlugin.Init(userToken, userName.text).ContinueWith(async (init) =>
        {
            if(init.IsCompletedSuccessfully) 
                await _communicationUWPPlugin.JoinTeamsCallAsync(teamsLink);
        });
    }

    #endregion

    #region unity methods

    private IEnumerator Start()
    {
        if (tokenProvider != null)
        {
            var enumerator = tokenProvider.GetToken();

            yield return enumerator;

            userToken = tokenProvider.Token;
        }
    }

    private void Update()
    {
        if (tokenProvider != null)
        {
            statusText.text = tokenProvider.HasToken ? "OK" : "NG";
        } 
        else if (tokenProvider == null)
        {
            statusText.text = string.IsNullOrEmpty(userToken) ? "NG" : "OK";
        }
    }

    #endregion

    #region public methods

    public void LeaveMeeting()
    {
        _communicationUWPPlugin?.LeaveMeeting();
    }

    public void Mute(FontIconSelector selector)
    {
        if (selector.CurrentIconName.Equals("Icon 20"))
        {
            selector.CurrentIconName = "Icon 105";
            _communicationUWPPlugin?.Mute();
        }
        else
        {
            selector.CurrentIconName = "Icon 20";
            _communicationUWPPlugin?.UnMute();
        }
    }

    #endregion
}