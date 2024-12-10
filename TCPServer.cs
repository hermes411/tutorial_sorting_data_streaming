using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System;
using Fusion;
using UnityEngine.XR;

public class TCPServer : MonoBehaviour
{
    private TcpListener tcpListener;
    private TcpClient tcpClient;
    private NetworkStream networkStream;
    private float batteryLevel;
    private Thread listenerThread;

    public Transform headsetTransform;  // Assign the headset (camera) transform here
    private NetworkRunner networkRunner;  // Fusion's NetworkRunner

    void Start()
    {
        // Assume the NetworkRunner is assigned or obtained dynamically in your scene
        networkRunner = FindObjectOfType<NetworkRunner>();
        if (networkRunner == null)
        {
            Debug.LogError("NetworkRunner not found in the scene.");
            return;
        }

        // Use the actual local network IP address of the machine running the Unity server
        string localIP = "192.168.137.41"; // Replace this with the actual local IP address of the Unity machine
        int port = 8888;

        // Set up TCP listener on the specified IP and port
        tcpListener = new TcpListener(IPAddress.Parse(localIP), port);
        tcpListener.Start();
        Debug.Log("Server started and listening on IP: " + localIP + " Port: " + port);

        // Start listening for incoming connections on a separate thread
        listenerThread = new Thread(ListenForClients);
        listenerThread.Start();
    }

    void ListenForClients()
    {
        while (!(networkRunner.IsRunning && networkRunner.SessionInfo.IsValid))
        {
            // do nothing
        }

        while (true)
        {
            try
            {
                // Wait for a client to connect
                tcpClient = tcpListener.AcceptTcpClient();
                // networkStream = tcpClient.GetStream();
                Debug.Log("Client connected!");

                // Continuously send data to the client
                while (tcpClient.Connected)
                {
                    StreamBatteryStatus();
                    Thread.Sleep(1000);
                    StreamHeadsetPosition();
                    Thread.Sleep(1000);
                    StreamControllerTrackingData();
                    Thread.Sleep(1000);
                    StreamEyeTrackingData();

                    // Wait a bit before sending again
                    Thread.Sleep(1000); // Send every second
                }

                // Close the connection when done
                tcpClient.Close();
                tcpClient = null;  // Ensure the previous client is cleared
                Debug.Log("Client disconnected.");
            }
            catch (Exception ex)
            {
                Debug.LogError("Error while listening for client: " + ex.Message);
            }
        }
    }

    // Streams the current headset battery status and writes it to the file
    private void StreamBatteryStatus()
    {
        // Retrieve the player name or ID (replace this with your method to get the name/ID)
        // string userNameOrId = GetPlayerNameOrId(playerRef);
        string userNameOrId = GetPlayerNameOrId();
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string data_type = "BatteryStatus";
        string battery_status = $"Battery Status - {SystemInfo.batteryLevel * 100}%";

        string message = $"{userNameOrId};{data_type};{timestamp}: {battery_status}\n";

        // Send data over TRCP
        networkStream = tcpClient.GetStream();
        byte[] data = Encoding.UTF8.GetBytes(message);
        networkStream.Write(data, 0, data.Length);
    }

    // Streams the current headset position and writes it to the file
    private void StreamHeadsetPosition()
    {
        if (headsetTransform != null)
        {
            Vector3 position = headsetTransform.position;

            // Retrieve the player name or ID (replace this with your method to get the name/ID)
            // string userNameOrId = GetPlayerNameOrId(playerRef);
            string userNameOrId = GetPlayerNameOrId();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string data_type = "HeadsetLocation";
            string headset_position = $"Headset Position - X: {position.x}, Y: {position.y}, Z: {position.z}";

            string message = $"{userNameOrId};{data_type};{timestamp}: {headset_position}\n";

            // Send data over TRCP
            networkStream = tcpClient.GetStream();
            byte[] data = Encoding.UTF8.GetBytes(message);
            networkStream.Write(data, 0, data.Length);
        }
        else
        {
            Debug.LogWarning("Headset Transform is not assigned!");
        }
    }

    // Streams the current hand tracking data (position, rotation)
    private void StreamControllerTrackingData()
    {
        // Get hand tracking data from XR Input Subsystem
        Vector3 leftHandPosition = Vector3.zero;
        Vector3 rightHandPosition = Vector3.zero;
        Quaternion leftHandRotation = Quaternion.identity;
        Quaternion rightHandRotation = Quaternion.identity;

        // Get positions and rotations for both hands
        var nodeStates = new System.Collections.Generic.List<XRNodeState>();
        InputTracking.GetNodeStates(nodeStates);

        foreach (var nodeState in nodeStates)
        {
            if (nodeState.nodeType == XRNode.LeftHand)
            {
                nodeState.TryGetPosition(out leftHandPosition);
                nodeState.TryGetRotation(out leftHandRotation);
            }
            if (nodeState.nodeType == XRNode.RightHand)
            {
                nodeState.TryGetPosition(out rightHandPosition);
                nodeState.TryGetRotation(out rightHandRotation);
            }
        }

        // Prepare data for UDP message
        string userNameOrId = GetPlayerNameOrId();
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string data_type = "ControllerTracking";
        string controllerData = $"Left Controller Position - X: {leftHandPosition.x}, Y: {leftHandPosition.y}, Z: {leftHandPosition.z}, Rotation: {leftHandRotation.eulerAngles} / " +
                          $"Right Controller Position - X: {rightHandPosition.x}, Y: {rightHandPosition.y}, Z: {rightHandPosition.z}, Rotation: {rightHandRotation.eulerAngles}";

        string message = $"{userNameOrId};{data_type};{timestamp}: {controllerData}\n";

        // Send data over TRCP
        networkStream = tcpClient.GetStream();
        byte[] data = Encoding.UTF8.GetBytes(message);
        networkStream.Write(data, 0, data.Length);
    }

    void OnApplicationQuit()
    {
        // Close the TCP listener and any active connections
        Debug.Log("Application is quitting. Closing connections...");
        tcpListener.Stop();

        // Close the client connection if any
        if (tcpClient != null && tcpClient.Connected)
        {
            tcpClient.Close();
            tcpClient = null;
        }

        // Abort the listener thread
        if (listenerThread != null && listenerThread.IsAlive)
        {
            listenerThread.Abort();
        }
    }

    // Streams the current eye tracking data (position, gaze direction)
    private void StreamEyeTrackingData()
    {
        // Get eye-tracking data from the XR Input Subsystem
        Vector3 leftEyePosition = Vector3.zero;
        Vector3 rightEyePosition = Vector3.zero;
        Vector3 leftGazeDirection = Vector3.zero;
        Vector3 rightGazeDirection = Vector3.zero;

        // Get eye position and gaze direction for both eyes
        var nodeStates = new System.Collections.Generic.List<XRNodeState>();
        InputTracking.GetNodeStates(nodeStates);

        foreach (var nodeState in nodeStates)
        {
            if (nodeState.nodeType == XRNode.LeftEye)
            {
                nodeState.TryGetPosition(out leftEyePosition);
                nodeState.TryGetRotation(out Quaternion leftRotation);
                leftGazeDirection = leftRotation * Vector3.forward;
            }
            if (nodeState.nodeType == XRNode.RightEye)
            {
                nodeState.TryGetPosition(out rightEyePosition);
                nodeState.TryGetRotation(out Quaternion rightRotation);
                rightGazeDirection = rightRotation * Vector3.forward;
            }
        }

        // Prepare data for UDP message
        string userNameOrId = GetPlayerNameOrId();
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string data_type = "EyeTracking";
        string eyeData = $"Left Eye Position - X: {leftEyePosition.x}, Y: {leftEyePosition.y}, Z: {leftEyePosition.z}, Gaze: {leftGazeDirection} / " +
                         $"Right Eye Position - X: {rightEyePosition.x}, Y: {rightEyePosition.y}, Z: {rightEyePosition.z}, Gaze: {rightGazeDirection}";

        string message = $"{userNameOrId};{data_type};{timestamp}: {eyeData}\n";

        /// Send data over TRCP
        networkStream = tcpClient.GetStream();
        byte[] data = Encoding.UTF8.GetBytes(message);
        networkStream.Write(data, 0, data.Length);
    }

    // Helper method to retrieve player name or ID
    // used to be PlayerRef playerRef as the input
    private string GetPlayerNameOrId()
    {
        // Replace with your method to get the player name or ID
        // For now, we use the PlayerRef's PlayerId as a unique identifier
        // return $"Player_{playerRef.PlayerId}";
        return "Player_1";
    }
}
