using System;
using Client;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Unity_Client))]
public class Unity_ClientDisplay : MonoBehaviour
{
    [Serializable]
    public class StateHandler<T>
    {
        #region Private Fields

        [SerializeField]
        private bool triggered;

        private T state;
        private Action onTriggerAction;
        private readonly object threadLock;

        #endregion Private Fields

        #region Public Properties

        public bool Triggered { get { lock (threadLock) return triggered; } }

        public T State
        {
            get { lock (threadLock) return state; }
            set { lock (threadLock) { triggered = true; state = value; } }
        }

        public Action OnTrigger
        {
            get { lock (threadLock) return onTriggerAction; }
            set { lock (threadLock) onTriggerAction = value; }
        }

        #endregion Public Properties

        #region Public Methods

        public StateHandler(T DefaultState, Action OnTrigger)
        {
            threadLock = new object();

            lock (threadLock)
            {
                state = DefaultState;
                onTriggerAction += OnTrigger;
                triggered = false;
            }
        }

        public void Update()
        {
            lock (threadLock)
            {
                if (triggered)
                {
                    onTriggerAction();
                    triggered = false;
                }
            };
        }

        public void Trigger()
        {
            lock (threadLock) triggered = true;
        }

        #endregion Public Methods
    }

    private Unity_Client client;

    public InputField Username;
    public InputField Password;
    public InputField ServerIP;
    public InputField ServerPort;

    public GameObject LoginDisplay;
    public GameObject LoggedOnDisplay;
    public Image availableUI;

    public StateHandler<bool> serverOnChanged;  // Server Connection Status
    public StateHandler<bool> loggedOnChanged;  // Login Status
    public StateHandler<UserAvailEventArgs> availableChanged;   // 'UserAvailable' action response status

    #region Unity Events

    private void Start()
    {
        client = GetComponent<Unity_Client>();

        ServerIP.text = client.ServerIP;
        ServerPort.text = client.ServerPort.ToString();

        SetUpCallbacks();

        SetLoginUIState(false);
    }

    private void Update()
    {
        serverOnChanged.Update();
        loggedOnChanged.Update();
        availableChanged.Update();
    }

    #endregion Unity Events

    #region Helpers

    private void SetUpCallbacks()
    {
        #region Callbacks for transitioning multi-threaded events to main thread, for UI purposes.

        // Server Status
        client.ServerOK += ((object s, EventArgs e) =>
        {
            serverOnChanged.State = true;
        });
        client.ServerFailed += ((object s, EventArgs e) =>
        {
            serverOnChanged.State = false;
        });

        // Login Status
        client.LoginOK += ((object s, EventArgs e) =>
        {
            loggedOnChanged.State = true;
        });
        client.Disconnected += ((object s, EventArgs e) =>
        {
            loggedOnChanged.State = false;
        });

        // User Available Action
        client.UserAvailable += ((object s, UserAvailEventArgs e) =>
        {
            availableChanged.State = e;
        });

        #endregion Callbacks for transitioning multi-threaded events to main thread, for UI purposes.

        #region UI Callbacks

        // Server Status
        serverOnChanged = new StateHandler<bool>(false, () =>
        {
            Debug.Log("Server Status UI State " + serverOnChanged.State.ToString() + ".");
        });

        // Login Status
        loggedOnChanged = new StateHandler<bool>(false, () =>
        {
            SetLoginUIState(loggedOnChanged.State);
            Debug.Log("Login Status UI State " + loggedOnChanged.State.ToString() + ".");
        });

        // User Available Action
        availableChanged = new StateHandler<UserAvailEventArgs>(EventArgs.Empty as UserAvailEventArgs, () =>
        {
            bool state = availableChanged.State.IsAvailable;
            availableUI.color = state ? Color.green : Color.red;
            Debug.Log(client.Username + " says: " + availableChanged.State.UserName + " is " + (state ? "" : "not ") + "connected.");
        });

        #endregion UI Callbacks

        #region Debug Callbacks

        client.RegisterOK += (object s, EventArgs e) =>
        {
            Debug.Log("Register OK.");
        };
        client.RegisterFailed += (object s, ErrorEventArgs e) =>
        {
            Debug.Log("Register Failed: " + e.Error);
        };
        client.LoginOK += (object s, EventArgs e) =>
        {
            Debug.Log("Login OK.");
        };
        client.LoginFailed += (object s, ErrorEventArgs e) =>
        {
            Debug.Log("Login Failed: " + e.Error);
        };
        client.Disconnected += (object s, EventArgs e) =>
        {
            Debug.Log("Disconnect.");
        };

        #endregion Debug Callbacks
    }

    private void SetLoginUIState(bool _isLoggedOn)
    {
        LoggedOnDisplay.SetActive(_isLoggedOn);
        LoginDisplay.SetActive(!_isLoggedOn);
        Username.readOnly = _isLoggedOn;
        Password.readOnly = _isLoggedOn;
        ServerIP.readOnly = _isLoggedOn;
        ServerPort.readOnly = _isLoggedOn;
    }

    #endregion Helpers

    #region Public UI Actions

    public void Register()
    {
        client.Register(Username.text, Password.text);
    }

    public void Login()
    {
        client.Login(Username.text, Password.text);
    }

    public void Disconnect()
    {
        client.Disconnect();
    }

    public void IsAvailable()
    {
        client.IsAvailable(Username.text);
    }

    public void IsAvailable(Text _text)
    {
        client.IsAvailable(_text.text);
    }

    public void ResetUIColor(Image _img)
    {
        _img.color = Color.white;
    }

    #endregion Public UI Actions
}