using System;
using Client;
using UnityEngine;

public class Unity_Client : MonoBehaviour
{
    // Variables
    private Client.Client client;

    // Properties
    public Client.Client Client { get { return client; } }


    public string ServerIP { get { return client.ServerIP; } set { client.ServerIP = value; } }
    public int ServerPort { get { return client.ServerPort; } set { client.ServerPort = value; } }
    public bool IsLoggedIn { get { return client.IsLoggedIn; } }
    public string Username { get { return client.Username; } }
    public string Password { get { return client.Password; } }

    #region EventHandlers

    public event EventHandler ServerOK
    { add { client.ServerOK += value; } remove { client.ServerOK -= value; } }

    public event EventHandler ServerFailed
    { add { client.ServerFailed += value; } remove { client.ServerFailed -= value; } }

    public event EventHandler LoginOK
    { add { client.LoginOK += value; } remove { client.LoginOK -= value; } }

    public event ErrorEventHandler LoginFailed
    { add { client.LoginFailed += value; } remove { client.LoginFailed -= value; } }

    public event EventHandler RegisterOK
    { add { client.RegisterOK += value; } remove { client.RegisterOK -= value; } }

    public event ErrorEventHandler RegisterFailed
    { add { client.RegisterFailed += value; } remove { client.RegisterFailed -= value; } }

    public event EventHandler Disconnected
    { add { client.Disconnected += value; } remove { client.Disconnected -= value; } }

    public event AvailEventHandler UserAvailable
    { add { client.UserAvailable += value; } remove { client.UserAvailable -= value; } }

    public event ReceivedEventHandler MessageReceived
    { add { client.MessageReceived += value; } remove { client.MessageReceived -= value; } }

    #endregion EventHandlers

    private void Awake()
    {
        client = new Client.Client();
    }

    private void OnDestroy()
    {
        if (client != null)
            client.Dispose();
    }

    private void Start()
    {
    }

    private void Client_UserAvailable(object sender, Client.UserAvailEventArgs e)
    {
        Debug.Log(client.Username + "says: " + e.UserName + " is " + (e.IsAvailable ? "" : "not ") + "connected.");
    }

    private void Update()
    {
    }

    public void Register(string user, string pass)
    {
        client.Register(user, pass);
    }

    public void Login(string user, string pass)
    {
        client.Login(user, pass);
    }

    public void Disconnect()
    {
        client.Disconnect();
    }

    public void IsAvailable(string username)
    {
        client.IsAvailable(username);
    }

    private void Client_RegisterOK(object sender, System.EventArgs e)
    {
        Debug.Log("Register OK.");
    }

    private void Client_RegisterFailed(object sender, Client.ErrorEventArgs e)
    {
        Debug.Log("Register Failed: " + e.Error);
    }

    private void Client_LoginOK(object sender, System.EventArgs e)
    {
        Debug.Log("Login OK.");
    }

    private void Client_LoginFailed(object sender, Client.ErrorEventArgs e)
    {
        Debug.Log("Login Failed: " + e.Error);
    }

    private void Client_Disconnected(object sender, System.EventArgs e)
    {
        Debug.Log("Disconnect.");
    }
}