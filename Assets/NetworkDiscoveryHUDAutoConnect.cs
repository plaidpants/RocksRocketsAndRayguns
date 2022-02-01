using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror.Discovery
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Network/NetworkDiscoveryHUDAutoConnect")]
    [HelpURL("https://mirror-networking.com/docs/Components/NetworkDiscovery.html")]
    [RequireComponent(typeof(NetworkDiscovery))]
    public class NetworkDiscoveryHUDAutoConnect : MonoBehaviour
    {
        readonly Dictionary<long, ServerResponse> discoveredServers = new Dictionary<long, ServerResponse>();
        Vector2 scrollViewPos = Vector2.zero;

        public NetworkDiscovery networkDiscovery;

        bool triedConnect = false;

        void tryConnect()
        {
            // try all found servers
            foreach (ServerResponse info in discoveredServers.Values)
            {
                if (!NetworkClient.isConnected && !NetworkServer.active && !NetworkClient.active)
                {
                    Connect(info);
                }
            }
            
            // no server connected try hosting instead
            if (!NetworkClient.isConnected && !NetworkServer.active && !NetworkClient.active)
            {
                discoveredServers.Clear();
                NetworkManager.singleton.StartHost();
                networkDiscovery.AdvertiseServer();
            }

            // flag that we tried
            triedConnect = true;
        }

        void Start()
        {
            discoveredServers.Clear();
            networkDiscovery.StartDiscovery();
            // wait 2 + random seconds before trying to connect so we find any servers out there already
            Invoke("tryConnect", 2 + (int)UnityEngine.Random.Range(0,4));
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (networkDiscovery == null)
            {
                networkDiscovery = GetComponent<NetworkDiscovery>();
                UnityEditor.Events.UnityEventTools.AddPersistentListener(networkDiscovery.OnServerFound, OnDiscoveredServer);
                UnityEditor.Undo.RecordObjects(new UnityEngine.Object[] { this, networkDiscovery }, "Set NetworkDiscovery");
            }
        }
#endif

        void Update()
        {
            // after we tried to connect, if we lose the connection, shutdown the app and allow the user to restart it.
            if (triedConnect && !NetworkClient.isConnected && !NetworkServer.active && !NetworkClient.active)
            {
                // lost the connection, need to restart the app
                Application.Quit();
            }
        }

        void OnGUI()
        {
            if (NetworkManager.singleton == null)
                return;

            if (NetworkServer.active || NetworkClient.active)
                return;

            if (!NetworkClient.isConnected && !NetworkServer.active && !NetworkClient.active)
                return;
                //DrawGUI();
        }

        void DrawGUI()
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Find Servers"))
            {
                discoveredServers.Clear();
                networkDiscovery.StartDiscovery();
            }

            // LAN Host
            if (GUILayout.Button("Start Host"))
            {
                discoveredServers.Clear();
                NetworkManager.singleton.StartHost();
                networkDiscovery.AdvertiseServer();
            }

            // Dedicated server
            if (GUILayout.Button("Start Server"))
            {
                discoveredServers.Clear();
                NetworkManager.singleton.StartServer();

                networkDiscovery.AdvertiseServer();
            }

            GUILayout.EndHorizontal();

            // show list of found server

            GUILayout.Label($"Discovered Servers [{discoveredServers.Count}]:");

            // servers
            scrollViewPos = GUILayout.BeginScrollView(scrollViewPos);

            foreach (ServerResponse info in discoveredServers.Values)
                if (GUILayout.Button(info.EndPoint.Address.ToString()))
                    Connect(info);

            GUILayout.EndScrollView();
        }

        void Connect(ServerResponse info)
        {
            NetworkManager.singleton.StartClient(info.uri);
        }

        public void OnDiscoveredServer(ServerResponse info)
        {
            // Note that you can check the versioning to decide if you can connect to the server or not using this method
            discoveredServers[info.serverId] = info;
        }
    }
}
