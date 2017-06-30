using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Unity_Client : MonoBehaviour
{
    private Client.Client client;

    public string Username;
    public string Password;

    public Toggle ToggleAvailableDisplay;
    public bool isAvailableValueChanged = false;
    public bool isAvailableValue = false;

    private void Start()
    {
        client = new Client.Client();
        client.RegisterFailed += Client_RegisterFailed;
        client.RegisterOK += Client_RegisterOK;
        client.LoginFailed += Client_LoginFailed;
        client.LoginOK += Client_LoginOK;
        client.Disconnected += Client_Disconnected;
        client.UserAvailable += Client_UserAvailable;
    }

    private void Client_UserAvailable(object sender, Client.IMAvailEventArgs e)
    {
        Debug.Log(client.UserName + "says: " + e.UserName + " is " + (e.IsAvailable ? "" : "not ") + "connected.");
        isAvailableValueChanged = true;
        isAvailableValue = e.IsAvailable;
    }

    private void OnDestroy()
    {
        if (client.IsLoggedIn)
            client.Disconnect();
    }

    private void Update()
    {
        if (isAvailableValueChanged)
        {
            isAvailableValueChanged = false;
            ToggleAvailableDisplay.isOn = isAvailableValue;
        }
    }

    public void Register()
    {
        client.Register(Username, Password);
    }

    public void Login()
    {
        client.Login(Username, Password);
    }

    public void Disconnect()
    {
        client.Disconnect();
    }

    public void IsAvailable()
    {
        client.IsAvailable(Username);
    }
    public void IsAvailable(Text _text)
    {
        client.IsAvailable(_text.text);
    }



    private void Client_RegisterOK(object sender, System.EventArgs e)
    {
        Debug.Log("Register OK.");
    }

    private void Client_RegisterFailed(object sender, Client.IMErrorEventArgs e)
    {
        Debug.Log("Register Failed: " + e.Error);
    }

    private void Client_LoginOK(object sender, System.EventArgs e)
    {
        Debug.Log("Login OK.");
    }

    private void Client_LoginFailed(object sender, Client.IMErrorEventArgs e)
    {
        Debug.Log("Login Failed: " + e.Error);
    }
    private void Client_Disconnected(object sender, System.EventArgs e)
    {
        Debug.Log("Disconnect.");
    }
}