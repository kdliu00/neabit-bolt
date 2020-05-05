﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Amazon.GameLift;
using Amazon.GameLift.Model;
using Bolt;
using UdpKit;
using Bolt.Matchmaking;

public class GameLiftClient : GlobalEventListener
{

    private const string GameLiftFleetId = "";
    private const string AccessKey = "AKIAQMSDIM7C7YPYMKO6";
    private const string SecretKey = "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";

    private AmazonGameLiftConfig ClientConfig;
    private Credentials ClientCredentials;
    private AmazonGameLiftClient ClientInstance;

    private GameSession CurrentGameSession;
    private PlayerSession CurrentPlayerSession;

    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        StartGameLiftClient();

        // Testing
        List<GameProperty> gameProperties = new List<GameProperty>
        {
            new GameProperty
            {
                Key = "m",
                Value = "NetworkTest"
            }
        };
        CreateGameSession(4, "asdfasdfasdfasdf", gameProperties);
        //FindGameSession("hasAvailablePlayerSessions=true", "playerSessionCount DESC");
    }

    public void StartGameLiftClient()
    {
        ClientConfig = new AmazonGameLiftConfig
        {
            ServiceURL = "http://localhost:9080"
            //RegionEndpoint = RegionEndpoint.USWest2
        };

        ClientCredentials = new Credentials
        {
            AccessKeyId = AccessKey,
            SecretAccessKey = SecretKey
        };

        ClientInstance = new AmazonGameLiftClient(ClientCredentials, ClientConfig);
    }

    public override void BoltStartDone()
    {
        if (BoltNetwork.IsClient)
        {
            ClientToken token = new ClientToken
            {
                UserId = Launcher.UserID,
                Username = Launcher.Username,
                SessionId = CurrentPlayerSession.PlayerSessionId
            };
            //UdpEndPoint endPoint = new UdpEndPoint(UdpIPv4Address.Parse(CurrentGameSession.IpAddress), (ushort)CurrentGameSession.Port);
            BoltMatchmaking.JoinSession(CurrentGameSession.GameSessionId, token);
        }
    }

    #region Non-matchmaking Game Session Management

    public void CreateGameSession(int maxPlayers, string sessionToken, List<GameProperty> gameProperties)
    {
        var gameSessionRequest = new CreateGameSessionRequest
        {
            IdempotencyToken = sessionToken,
            MaximumPlayerSessionCount = maxPlayers,
            GameProperties = gameProperties,
            FleetId = GameLiftFleetId
        };

        CreateGameSessionResponse gameSessionResponse = null;
        try
        {
            gameSessionResponse = ClientInstance.CreateGameSession(gameSessionRequest);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception.Message);
            return;
        }

        if (gameSessionResponse == null)
        {
            Debug.LogError("Could not create game session!");
            Debug.LogError(gameSessionResponse.ResponseMetadata.ToString());
        }
        else
        {
            Debug.LogFormat("Successfully created game session %s", gameSessionResponse.GameSession.GameSessionId);
        }
    }

    /// <summary>
    /// Finds and joins the first available game session based on filterQuery and sortQuery.
    /// </summary>
    /// <param name="filterQuery">String containing the search criteria for the session search. If no filter expression is included, the request returns results for all game sessions in the fleet that are in ACTIVE status.</param>
    /// <param name="sortQuery">Instructions on how to sort the search results. If no sort expression is included, the request returns results in random order.</param>
    public void FindGameSession(string filterQuery, string sortQuery)
    {
        var gameSessionsRequest = new SearchGameSessionsRequest
        {
            FleetId = GameLiftFleetId,
            FilterExpression = filterQuery,
            SortExpression = sortQuery,
            Limit = 1
        };

        SearchGameSessionsResponse gameSessionsResponse = null;
        try
        {
            gameSessionsResponse = ClientInstance.SearchGameSessions(gameSessionsRequest);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception.Message);
            return;
        }

        if (gameSessionsResponse == null)
        {
            Debug.LogErrorFormat("Could not find any game session with the following parameters: %s", filterQuery);
        }
        else
        {
            Debug.LogFormat("Successfully found game session with the following parameters: %s", filterQuery);
            JoinGameSession(gameSessionsResponse.GameSessions[0]);
        }
    }

    public void JoinGameSession(GameSession selectedGameSession)
    {
        Debug.LogFormat("%d/%d players in selected game session.", selectedGameSession.CurrentPlayerSessionCount, selectedGameSession.MaximumPlayerSessionCount);
        CurrentGameSession = selectedGameSession;

        var playerSessionRequest = new CreatePlayerSessionRequest
        {
            GameSessionId = selectedGameSession.GameSessionId,
            PlayerData = Launcher.Username,
            PlayerId = Launcher.UserID
        };

        CreatePlayerSessionResponse playerSessionResponse = null;
        try
        {
            playerSessionResponse = ClientInstance.CreatePlayerSession(playerSessionRequest);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception.Message);
            return;
        }

        if (playerSessionResponse == null)
        {
            Debug.LogErrorFormat("Unable to create session for player %s with game session %s", Launcher.UserID, selectedGameSession.GameSessionId);
        }
        else
        {
            CurrentPlayerSession = playerSessionResponse.PlayerSession;
            Debug.LogFormat("Successfully created player session %s with game session %s", CurrentPlayerSession.PlayerSessionId, selectedGameSession.GameSessionId);
            BoltLauncher.StartClient();
        }
    }

    #endregion

}
