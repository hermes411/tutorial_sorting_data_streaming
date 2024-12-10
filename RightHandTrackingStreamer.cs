using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Threading;
using System.Net;

public class RightHandTrackingStreamer : MonoBehaviour
{
    [SerializeField]
    private OVRHand hand;

    [SerializeField]
    private OVRSkeleton handSkeleton;

    private TcpListener tcpListener;
    private TcpClient tcpClient;
    private NetworkStream networkStream;
    private Thread listenerThread;

    // Start is called before the first frame update
    void Start()
    {
        // Use the actual local network IP address of the machine running the Unity server
        string localIP = "192.168.137.41"; // Replace this with the actual local IP address of the Unity machine
        int port = 8890;

        // Set up TCP listener on the specified IP and port
        tcpListener = new TcpListener(IPAddress.Parse(localIP), port);
        tcpListener.Start();
        Debug.Log("Server started and listening on IP: " + localIP + " Port: " + port);

        // Start listening for incoming connections on a separate thread
        listenerThread = new Thread(ListenForClients);
        listenerThread.Start();

        if (!hand) hand = GetComponent<OVRHand>();
        if (!handSkeleton) handSkeleton = GetComponent<OVRSkeleton>();
    }

    void ListenForClients()
    {
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
                    StreamHandTrackingData();

                    // Wait a bit before sending again
                    Thread.Sleep(5000); // Send every second
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

    // Streams the current hand tracking data (position, rotation)
    private void StreamHandTrackingData()
    {

        if (hand && hand.IsTracked && handSkeleton)
        {
            string handData = "";

            foreach (var bone in handSkeleton.Bones)
            {
                handData += $"{handSkeleton.GetSkeletonType()}: boneId -> {bone.Id} pos -> {bone.Transform.position} rotation -> {bone.Transform.rotation} |";
            }

            // Prepare data for UDP message
            string userNameOrId = GetPlayerNameOrId();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string data_type = "RightHandTracking";
            string message = $"{userNameOrId};{data_type};{timestamp}: {handData}\n";

            // Send data over TRCP
            networkStream = tcpClient.GetStream();
            byte[] data = Encoding.UTF8.GetBytes(message);
            networkStream.Write(data, 0, data.Length);
        }
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

    // Helper method to retrieve player name or ID
    private string GetPlayerNameOrId()
    {
        return "Player_1";  // Replace with actual logic to retrieve player ID
    }
}
