using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MmoTemplate.Network
{
    /// <summary>
    /// Login / connect screen logic. Lives on a UI Canvas in Bootstrap.unity.
    /// Wires the Host / Join / Dedicated-Server buttons to Netcode for GameObjects'
    /// NetworkManager and hands off to the World scene once the connection is live.
    ///
    /// Mirrors the client/server split used elsewhere in this template's Godot
    /// counterpart: the server is authoritative, clients only ever send input.
    /// </summary>
    public class NetworkBootstrap : MonoBehaviour
    {
        [Header("UI (assign in Inspector)")]
        [SerializeField] private InputField nameField;
        [SerializeField] private InputField addressField;
        [SerializeField] private InputField portField;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button dedicatedButton;
        [SerializeField] private Text statusLabel;

        [Header("Scenes")]
        [SerializeField] private string worldSceneName = "World";

        public const ushort DefaultPort = 24565;
        public const string DefaultAddress = "127.0.0.1";

        /// <summary>Chosen on this screen; read by PlayerController when it spawns.</summary>
        public static string LocalPlayerName { get => MmoTemplate.Rpg.GameEvents.LocalPlayerName; private set => MmoTemplate.Rpg.GameEvents.LocalPlayerName = value; }

        private void Awake()
        {
            // Headless dedicated-server launch: -batchmode -nographic -server [-port N]
            if (Application.isBatchMode && System.Environment.GetCommandLineArgs().ContainsArg("-server"))
            {
                StartDedicatedServer(PortFromArgs());
                return;
            }

            if (nameField != null) nameField.text = "Player" + Random.Range(0, 1000);
            if (portField != null) portField.text = DefaultPort.ToString();
            if (addressField != null) addressField.text = DefaultAddress;

            hostButton?.onClick.AddListener(OnHostPressed);
            joinButton?.onClick.AddListener(OnJoinPressed);
            dedicatedButton?.onClick.AddListener(OnDedicatedPressed);

            NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnected;
        }

        private void OnHostPressed()
        {
            ApplyName();
            StartCoroutine(StartSession(() =>
            {
                ApplyTransport(DefaultAddress, ReadPort());
                if (NetworkManager.Singleton.StartHost())
                {
                    GotoWorld();
                    return true;
                }
                SetStatus("Failed to start host.");
                return false;
            }));
        }

        private void OnJoinPressed()
        {
            ApplyName();
            StartCoroutine(StartSession(() =>
            {
                string address = string.IsNullOrWhiteSpace(addressField?.text)
                    ? DefaultAddress
                    : addressField.text.Trim();
                ApplyTransport(address, ReadPort());
                SetStatus($"Connecting to {address}:{ReadPort()} ...");
                // Clients do NOT load the world themselves — the server drives the
                // scene transition through Netcode's scene manager, which is what
                // keeps in-scene networked objects (NPCs, quest state) in sync.
                if (NetworkManager.Singleton.StartClient()) return true;
                SetStatus("Failed to start client.");
                return false;
            }));
        }

        private void OnDedicatedPressed()
        {
            StartCoroutine(StartSession(() => StartDedicatedServer(ReadPort())));
        }

        /// <summary>
        /// Netcode refuses to start while a previous session is still listening
        /// ("Can't start while listening"). NetworkManager survives scene loads,
        /// so returning to this screen after playing leaves the old session up —
        /// shut it down and wait for the socket to close before starting again.
        /// </summary>
        private System.Collections.IEnumerator StartSession(System.Func<bool> start)
        {
            var nm = NetworkManager.Singleton;

            if (nm.IsListening || nm.ShutdownInProgress)
            {
                SetStatus("Closing previous session…");
                nm.Shutdown();
                while (nm.IsListening || nm.ShutdownInProgress)
                    yield return null;
                yield return null; // let the transport release the socket
            }

            start();
        }

        private bool StartDedicatedServer(ushort port)
        {
            LocalPlayerName = "SERVER";
            ApplyTransport("0.0.0.0", port);
            if (NetworkManager.Singleton.StartServer())
            {
                Debug.Log($"[server] Dedicated server listening on port {port}");
                GotoWorld();
                return true;
            }

            Debug.LogError($"[server] Failed to bind port {port}");
            SetStatus($"Failed to bind port {port}. Is it already in use?");
            if (Application.isBatchMode) Application.Quit(1);
            return false;
        }

        private void OnDestroy()
        {
            // This screen is reloaded on disconnect, so the callback would stack
            // up on the surviving NetworkManager if we never detached.
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnected;
        }

        private void OnDisconnected(ulong clientId)
        {
            if (clientId != NetworkManager.Singleton.LocalClientId) return;
            SetStatus("Disconnected from server.");
            // Close the session down fully, otherwise the next Host/Join hits
            // "Can't start while listening".
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("Bootstrap");
        }

        private void ApplyTransport(string address, ushort port)
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetConnectionData(address, port);
        }

        private void ApplyName()
        {
            string n = nameField != null ? nameField.text.Trim() : "";
            LocalPlayerName = string.IsNullOrEmpty(n) ? "Player" + Random.Range(0, 1000) : n;
        }

        private ushort ReadPort()
        {
            if (portField != null && ushort.TryParse(portField.text, out ushort p) && p > 0)
                return p;
            return DefaultPort;
        }

        private ushort PortFromArgs()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-port" && ushort.TryParse(args[i + 1], out ushort p))
                    return p;
            return DefaultPort;
        }

        /// <summary>Server/host only. Netcode's scene manager loads the world for
        /// everyone, including clients that connect later.</summary>
        private void GotoWorld()
        {
            var nm = NetworkManager.Singleton;
            if (nm.IsServer)
                nm.SceneManager.LoadScene(worldSceneName, LoadSceneMode.Single);
            else
                SceneManager.LoadScene(worldSceneName);
        }

        private void SetStatus(string msg)
        {
            if (statusLabel != null) statusLabel.text = msg;
            Debug.Log("[net] " + msg);
        }
    }

    internal static class ArgsExtensions
    {
        public static bool ContainsArg(this string[] args, string flag)
        {
            foreach (var a in args) if (a == flag) return true;
            return false;
        }
    }
}
