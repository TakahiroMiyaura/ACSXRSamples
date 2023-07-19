// Copyright (c) 2023 Takahiro Miyaura
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Communication.Calling.UnityClient;
using UnityEngine;
using UnityEngine.Video;

internal class AzureCommunicationUWPPlugin
{
    internal async Task Init(string token, string userName)
    {
#if WINDOWS_UWP
        await InitCallAgent(token, userName);
#endif
    }

    internal async Task JoinTeamsCallAsync(string teamsMeetingUrl)
    {
#if WINDOWS_UWP
        GetCameraDevice();
        var joinCallOptions = new JoinCallOptions();
        joinCallOptions.OutgoingVideoOptions = new OutgoingVideoOptions
        {
            Streams = _localVideoStream
        };

        var teamsMeetingLinkLocator = new TeamsMeetingLinkLocator(teamsMeetingUrl);
        _call = await _callAgent.JoinAsync(teamsMeetingLinkLocator, joinCallOptions);

        if (_call != null)
        {
            _call.RemoteParticipantsUpdated += OnRemoteParticipantsUpdatedAsync;
            _call.StateChanged += OnStateChangedAsync;
        }
#endif
    }

    internal async Task JoinGroupCallAsync(string groupGuid)
    {
        var groupId = Guid.Parse(groupGuid);

#if WINDOWS_UWP
        GetCameraDevice();

        var joinCallOptions = new JoinCallOptions();
        joinCallOptions.OutgoingVideoOptions = new OutgoingVideoOptions
        {
            Streams = _localVideoStream
        };

        var groupCallLocator = new GroupCallLocator(groupId);
        _call = await _callAgent.JoinAsync(groupCallLocator, joinCallOptions);

        if (_call != null)
        {
            _call.RemoteParticipantsUpdated += OnRemoteParticipantsUpdatedAsync;
            _call.StateChanged += OnStateChangedAsync;
        }
#endif
    }

    internal async Task LeaveMeeting()
    {
#if WINDOWS_UWP
        await _call.HangUpAsync(new HangUpOptions());
#endif
    }

    internal async void Mute()
    {
#if WINDOWS_UWP
        await _call.MuteAsync();
#endif
    }

    internal async void UnMute()
    {
#if WINDOWS_UWP
        await _call.UnmuteAsync();
#endif
    }
#if WINDOWS_UWP
    private CallClient _callClient;
    private CallAgent _callAgent;
    private Call _call;
    private DeviceManager _deviceManager;
    private LocalVideoStream[] _localVideoStream;
#endif

#if WINDOWS_UWP
    private async Task InitCallAgent(string userToken, string userName)
    {
        var tokenCredential = new CallTokenCredential(userToken);
        _callClient = new CallClient();
        _deviceManager = await _callClient.GetDeviceManager();
        _localVideoStream = new LocalVideoStream[1];

        var callAgentOptions = new CallAgentOptions
        {
            DisplayName = userName
        };
        _callAgent = await _callClient.CreateCallAgent(tokenCredential, callAgentOptions);
        _callAgent.CallsUpdated += OnCallsUpdatedAsync;
        _callAgent.IncomingCallReceived += OnCallAgentIncomingCall;
    }

    /// <summary>
    ///     �C�x���g�F�R�[������M������N���C�A���g���̃r�f�I�J�����̏��ƂƂ��Ɏ󂯓����
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnCallAgentIncomingCall(object sender, IncomingCallReceivedEventArgs e)
    {
        GetCameraDevice();

        var acceptCallOptions = new AcceptCallOptions();
        acceptCallOptions.OutgoingVideoOptions = new OutgoingVideoOptions
        {
            Streams = _localVideoStream
        };
        _call = await e.IncomingCall.AcceptAsync(acceptCallOptions);

        if (_call != null)
        {
            _call.RemoteParticipantsUpdated += OnRemoteParticipantsUpdatedAsync;
            _call.StateChanged += OnStateChangedAsync;
        }
    }


    /// <summary>
    ///     �C�x���g:��M��Ԃ��ύX���ꂽ�珈������B�����[�g�̃r�f�I�̏�Ԃ��`�F�b�N
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private async void OnCallsUpdatedAsync(object sender, CallsUpdatedEventArgs args)
    {
        var removedParticipants = new List<RemoteParticipant>();
        var addedParticipants = new List<RemoteParticipant>();

        foreach (var call in args.RemovedCalls) removedParticipants.AddRange(call.RemoteParticipants.ToList());

        foreach (var call in args.AddedCalls) addedParticipants.AddRange(call.RemoteParticipants.ToList());

        await OnParticipantChangedAsync(removedParticipants, addedParticipants);
    }


    private async void OnStateChangedAsync(object sender, PropertyChangedEventArgs args)
    {
        switch (((Call)sender).State)
        {
            case CallState.Disconnected:
                break;
        }
    }

    private async void GetCameraDevice()
    {
        if (_deviceManager.Cameras.Count > 0)
        {
            var videoDeviceInfo = _deviceManager.Cameras[0];
            _localVideoStream[0] = new LocalVideoStream(videoDeviceInfo);
            var localUri = await _localVideoStream[0].MediaUriAsync();
        }
    }


    private async void OnRemoteParticipantsUpdatedAsync(object sender, ParticipantsUpdatedEventArgs args)
    {
        await OnParticipantChangedAsync(
            args.RemovedParticipants.ToList(),
            args.AddedParticipants.ToList());
    }


    private async Task OnParticipantChangedAsync(IEnumerable<RemoteParticipant> removedParticipants,
        IEnumerable<RemoteParticipant> addedParticipants)
    {
        foreach (var participant in removedParticipants)
        {
            foreach (var incomingVideoStream in participant.IncomingVideoStreams)
                if (incomingVideoStream is RemoteVideoStream remoteVideoStream)
                    remoteVideoStream.Stop();

            participant.VideoStreamStateChanged -= OnVideoStreamStateChanged;
        }

        foreach (var participant in addedParticipants) participant.VideoStreamStateChanged += OnVideoStreamStateChanged;
    }

    private void OnVideoStreamStateChanged(object sender, VideoStreamStateChangedEventArgs e)
    {
        var callVideoStream = e.Stream;

        switch (callVideoStream.Direction)
        {
            case StreamDirection.Outgoing:
                OnOutgoingVideoStreamStateChanged(callVideoStream as OutgoingVideoStream);
                break;
            case StreamDirection.Incoming:
                OnIncomingVideoStreamStateChanged(callVideoStream as IncomingVideoStream);
                break;
        }
    }

    private void OnOutgoingVideoStreamStateChanged(OutgoingVideoStream callVideoStream)
    {
        if (callVideoStream is LocalVideoStream localStream)
            switch (localStream.State)
            {
                case VideoStreamState.Available:
                case VideoStreamState.Started:
                    break;
                case VideoStreamState.Stopping:
                    break;
                case VideoStreamState.Stopped:
                    break;
                case VideoStreamState.NotAvailable:
                    break;
            }
    }


    private async void OnIncomingVideoStreamStateChanged(IncomingVideoStream incomingVideoStream)
    {
        switch (incomingVideoStream.State)
        {
            case VideoStreamState.Available:
            {
                switch (incomingVideoStream.Kind)
                {
                    case VideoStreamKind.RemoteIncoming:
                        break;

                    case VideoStreamKind.RawIncoming:
                        break;
                }

                break;
            }
            case VideoStreamState.Started:
                break;
            case VideoStreamState.Stopping:
                break;
            case VideoStreamState.Stopped:
                if (incomingVideoStream.Kind == VideoStreamKind.RemoteIncoming)
                    if (incomingVideoStream is RemoteVideoStream remoteVideoStream)
                        remoteVideoStream.Stop();

                break;
            case VideoStreamState.NotAvailable:
                break;
        }
    }
#endif
}