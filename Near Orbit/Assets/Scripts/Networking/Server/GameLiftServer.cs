﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Aws.GameLift;
using Aws.GameLift.Server;
using Aws.GameLift.Server.Model;
using UdpKit;
using Bolt;
using Bolt.Photon;
using Bolt.Matchmaking;

public class GameLiftServer : GlobalEventListener
{

    public bool StartedGameLift;
    public GameSession ServerSession;

    void Start()
    {
        DontDestroyOnLoad(this.gameObject);

        //Set the port that your game service is listening on for incoming player connections (hard-coded here for simplicity)
        int listeningPort = 7777;
        //int listeningPort = int.Parse(GetArg("-p", "-port") ?? "7777");

        StartGameLift(listeningPort);
    }

    void OnApplicationQuit()
    {
        //Make sure to call GameLiftServerAPI.Destroy() when the application quits. 
        //This resets the local connection with GameLift's agent.
        if (StartedGameLift)
        {
            GameLiftServerAPI.Destroy();
        }

        BoltNetwork.Shutdown();
    }

    //This is an example of a simple integration with GameLift server SDK that makes game server 
    //processes go active on Amazon GameLift
    public void StartGameLift(int listeningPort)
    {
        StartedGameLift = true;
        Debug.LogFormat("Port: {0}", listeningPort);

        //InitSDK establishes a local connection with the Amazon GameLift agent to enable 
        //further communication.
        var initSDKOutcome = GameLiftServerAPI.InitSDK();
        if (initSDKOutcome.Success)
        {
            ProcessParameters processParameters = new ProcessParameters(
                (gameSession) =>
                {
                    ServerSession = gameSession;
                    BoltLauncher.StartServer(listeningPort);

                    //Respond to new game session activation request.GameLift sends activation request
                    //to the game server along with a game session object containing game properties
                    //and other settings.Once the game server is ready to receive player connections, 
                    //invoke GameLiftServerAPI.ActivateGameSession()
                    GameLiftServerAPI.ActivateGameSession();
                },
                () =>
                {
                    BoltNetwork.Shutdown();

                    //OnProcessTerminate callback. GameLift invokes this callback before shutting down 
                    //an instance hosting this game server. It gives this game server a chance to save
                    //its state, communicate with services, etc., before being shut down. 
                    //In this case, we simply tell GameLift we are indeed going to shut down.
                    GameLiftServerAPI.ProcessEnding();
                },
                () =>
                {
                    //This is the HealthCheck callback.
                    //GameLift invokes this callback every 60 seconds or so.
                    //Here, a game server might want to check the health of dependencies and such.
                    //Simply return true if healthy, false otherwise.
                    //The game server has 60 seconds to respond with its health status. 
                    //GameLift will default to 'false' if the game server doesn't respond in time.
                    //In this case, we're always healthy!
                    return true;
                },
                //Here, the game server tells GameLift what port it is listening on for incoming player 
                //connections. In this example, the port is hardcoded for simplicity. Active game
                //that are on the same instance must have unique ports.
                listeningPort,
                new LogParameters(new List<string>()
                {
                    //Here, the game server tells GameLift what set of files to upload when the game session ends.
                    //GameLift uploads everything specified here for the developers to fetch later.
                    //"/local/game/logs/myserver.log"
                    string.Format("local/game/logs/{0}.log", ServerSession.GameSessionId)
                }));

            //Calling ProcessReady tells GameLift this game server is ready to receive incoming game sessions!
            var processReadyOutcome = GameLiftServerAPI.ProcessReady(processParameters);
            if (processReadyOutcome.Success)
            {
                Debug.Log("ProcessReady success. GameLift ready to host game sessions.");
            }
            else
            {
                Debug.LogErrorFormat("ProcessReady failure: {0}", processReadyOutcome.Error.ToString());
            }
        }
        else
        {
            Debug.LogErrorFormat("InitSDK failure: {0}", initSDKOutcome.Error.ToString());
            Application.Quit();
        }
    }

    public override void ConnectRequest(UdpEndPoint endpoint, IProtocolToken token)
    {
        ClientToken clientToken = (ClientToken)token;

        //Ask GameLift to verify sessionID is valid, it will change player slot from "RESERVED" to "ACTIVE"
        GenericOutcome outCome = GameLiftServerAPI.AcceptPlayerSession(clientToken.SessionId);
        if (outCome.Success)
        {
            BoltNetwork.Accept(endpoint);
        }
        else
        {
            BoltNetwork.Refuse(endpoint);
        }

        /*
        This data type is used to specify which player session(s) to retrieve. 
        It can be used in several ways: 
        (1) provide a PlayerSessionId to request a specific player session; 
        (2) provide a GameSessionId to request all player sessions in the specified game session; or
        (3) provide a PlayerId to request all player sessions for the specified player.
        For large collections of player sessions, 
        use the pagination parameters to retrieve results as sequential pages.
        */
        DescribePlayerSessionsRequest sessions = new DescribePlayerSessionsRequest()
        {
            PlayerSessionId = clientToken.SessionId,
            GameSessionId = ServerSession.GameSessionId,
            PlayerId = clientToken.UserId
        };

        DescribePlayerSessionsOutcome sessionsOutcome = GameLiftServerAPI.DescribePlayerSessions(sessions);
        string playerId = sessionsOutcome.Result.PlayerSessions[0].PlayerId;
        Debug.LogFormat("Player ID: {0}", playerId);
    }

    public override void Connected(BoltConnection connection)
    {
        if (BoltNetwork.IsServer)
        {
            ClientToken myToken = (ClientToken)connection.ConnectToken;
            connection.UserData = myToken.SessionId;
        }
    }

    public override void Disconnected(BoltConnection connection)
    {
        if (BoltNetwork.IsServer)
        {
            GameLiftServerAPI.RemovePlayerSession((string)connection.UserData);
        }
    }

    public override void BoltStartBegin()
    {
        //Register any Protocol Token that are you using
        BoltNetwork.RegisterTokenClass<PhotonRoomProperties>();
        BoltNetwork.RegisterTokenClass<ClientToken>();
    }

    public override void BoltStartDone()
    {
        if (BoltNetwork.IsServer)
        {
            string sessionId = ServerSession.GameSessionId;
            //If GameSessionId was not set, throw an error and shut down the BoltNetwork
            if (sessionId.Length == 0)
            {
                Debug.LogError("GameSessionId not set! Shutting down.");
                BoltNetwork.Shutdown();
                return;
            }

            List<GameProperty> gameProperties = ServerSession.GameProperties;
            PhotonRoomProperties roomProperties = new PhotonRoomProperties();
            roomProperties.IsOpen = true;
            roomProperties.IsVisible = true;

            foreach (GameProperty gameProperty in gameProperties)
            {
                roomProperties.AddRoomProperty(gameProperty.Key, gameProperty.Value);
            }

            // Create the Bolt session
            BoltMatchmaking.CreateSession(sessionId, roomProperties);

            // Load the requested map
            BoltNetwork.LoadScene((string)roomProperties.CustomRoomProperties["m"]);
        }
        else
        {
            Debug.LogError("Attempting to create game not as server! Shutting down.");
            BoltNetwork.Shutdown();
        }
    }
}