using UnityEngine;

public class Unity_Client : MonoBehaviour
{
    private Client.Client client;

    public string Username;
    public string Password;

    private void Start()
    {
        client = new Client.Client();

        client.RegisterFailed += Client_RegisterFailed;
        client.RegisterOK += Client_RegisterOK;
    }

    private void Update()
    {
    }

    public void Register()
    {
        client.Register(Username, Password);
    }

    public void Disconnect()
    {
        client.Disconnect();
    }

    private void Client_RegisterOK(object sender, System.EventArgs e)
    {
        Debug.Log("Register OK!");
    }

    private void Client_RegisterFailed(object sender, Client.IMErrorEventArgs e)
    {
        Debug.Log("Register Failed: " + e.Error);
    }
}