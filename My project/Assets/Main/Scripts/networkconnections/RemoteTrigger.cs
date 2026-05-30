using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class RemoteTrigger : MonoBehaviour
{
    private UdpClient udpClient;
    private Thread listenThread;
    private bool signalReceived = false;
    private int port = 5005;

    void Start()
    {
        // Spin up the background thread to listen for the Python script
        listenThread = new Thread(new ThreadStart(ListenForUDP));
        listenThread.IsBackground = true;
        listenThread.Start();
        Debug.Log($"<color=cyan>[Network] Listening for UDP triggers on port {port}...</color>");
    }

    private void ListenForUDP()
    {
        // Bind to the port and listen to any incoming IP address on the local network
        udpClient = new UdpClient(port);
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

        while (true)
        {
            try
            {
                // This line blocks the background thread until a packet arrives
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string text = Encoding.UTF8.GetString(data);
                
                if (text == "TRIGGER_TRIAL")
                {
                    // We cannot call Unity API methods from a background thread.
                    // We flip this boolean so the main Update() loop can handle it safely.
                    signalReceived = true; 
                }
            }
            catch (SocketException) 
            { 
                // Catch socket closures during application quit to prevent thread crashes
                break; 
            }
        }
    }

    void Update()
    {
        // Main Unity Thread Execution
        if (signalReceived)
        {
            signalReceived = false;
            Debug.Log("<color=green>[Network] Remote Signal Received! Advancing FSM State.</color>");
            
            // Talk DIRECTLY to the Singleton
            if (Director.Instance != null)
            {
                Director.Instance.ButtonPressed();
            }
            else
            {
                Debug.LogError("RemoteTrigger heard the signal, but Director.Instance is missing!");
            }
        }
    }

    void OnApplicationQuit()
    {
        // Critically important: Close the socket and kill the thread when the app closes
        if (udpClient != null)
        {
            udpClient.Close();
        }
        if (listenThread != null && listenThread.IsAlive)
        {
            listenThread.Abort();
        }
    }
}