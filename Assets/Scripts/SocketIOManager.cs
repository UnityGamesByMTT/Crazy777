using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using DG.Tweening;
using System.Linq;
using Newtonsoft.Json;
using Best.SocketIO;
using Best.SocketIO.Events;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;
using Best.HTTP.Shared;

public class SocketIOManager : MonoBehaviour
{
    [SerializeField]
    private SlotBehaviour slotManager;

    [SerializeField]
    private UIManager uiManager;

    internal GameData initialData = null;
    internal UIData initUIData = null;
    internal GameData resultData = null;
    internal PlayerData playerdata = null;
    internal Message myMessage = null;
    internal double GambleLimit = 0;
    [SerializeField]
    internal List<string> bonusdata = null;
    private SocketManager manager;

    protected string SocketURI = null;
    protected string TestSocketURI = "https://game-crm-rtp-backend.onrender.com/"; //HACK: Professional / Dev Server Address
    //protected string TestSocketURI = "https://7p68wzhv-5000.inc1.devtunnels.ms/"; //HACK: Personal Test Server Address

    [SerializeField]
    private string testToken;
    internal bool isResultdone = false;

    protected string gameID = "SL-CRZ";

    internal bool isLoaded = false;
    internal bool SetInit = false;

    private const int maxReconnectionAttempts = 6;
    private readonly TimeSpan reconnectionDelay = TimeSpan.FromSeconds(10);

    private void Start()
    {
        SetInit = false;
        OpenSocket();

        //Debug.unityLogger.logEnabled = false;
    }
    void ReceiveAuthToken(string jsonData)
    {
        Debug.Log("Received data: " + jsonData);

        // Parse the JSON data
        var data = JsonUtility.FromJson<AuthTokenData>(jsonData);
        SocketURI = data.socketURL;
        myAuth = data.cookie;

        // Proceed with connecting to the server using myAuth and socketURL
    }

    string myAuth = null;

    private void Awake()
    {
        //HTTPManager.Logger = null;
        isLoaded = false;
    }

    private void OpenSocket()
    {
        //Create and setup SocketOptions
        SocketOptions options = new SocketOptions();
        options.ReconnectionAttempts = maxReconnectionAttempts;
        options.ReconnectionDelay = reconnectionDelay;
        options.Reconnection = true;

        Application.ExternalCall("window.parent.postMessage", "authToken", "*");

#if UNITY_WEBGL && !UNITY_EDITOR
        Application.ExternalEval(@"
            window.addEventListener('message', function(event) {
                if (event.data.type === 'authToken') {
                    var combinedData = JSON.stringify({
                        cookie: event.data.cookie,
                        socketURL: event.data.socketURL
                    });
                    // Send the combined data to Unity
                    SendMessage('SocketManager', 'ReceiveAuthToken', combinedData);
                }});");
        StartCoroutine(WaitForAuthToken(options));
#else
        Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
        {
            return new
            {
                token = testToken,
                gameId = gameID
            };
        };
        options.Auth = authFunction;
        // Proceed with connecting to the server
        SetupSocketManager(options);
#endif
    }

    private IEnumerator WaitForAuthToken(SocketOptions options)
    {
        // Wait until myAuth is not null
        while (myAuth == null)
        {
            yield return null;
        }

        // Once myAuth is set, configure the authFunction
        Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
        {
            return new
            {
                token = myAuth,
                gameId = gameID
            };
        };
        options.Auth = authFunction;

        Debug.Log("Auth function configured with token: " + myAuth);

        // Proceed with connecting to the server
        SetupSocketManager(options);
    }

    private void SetupSocketManager(SocketOptions options)
    {
        // Create and setup SocketManager
#if UNITY_EDITOR
            this.manager = new SocketManager(new Uri(TestSocketURI), options);
#else
        this.manager = new SocketManager(new Uri(SocketURI), options);
#endif

        // Set subscriptions
        this.manager.Socket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnConnected);
        this.manager.Socket.On<string>(SocketIOEventTypes.Disconnect, OnDisconnected);
        this.manager.Socket.On<string>(SocketIOEventTypes.Error, OnError);
        this.manager.Socket.On<string>("message", OnListenEvent);
        this.manager.Socket.On<bool>("socketState", OnSocketState);
        this.manager.Socket.On<string>("internalError", OnSocketError);
        this.manager.Socket.On<string>("alert", OnSocketAlert);
        this.manager.Socket.On<string>("AnotherDevice", OnSocketOtherDevice);

        // Start connecting to the server
        this.manager.Open();
    }

    //Connected event handler implementation
    void OnConnected(ConnectResponse resp)
    {
        Debug.Log("Connected!");
        SendPing();
    }

    private void OnDisconnected(string response)
    {
        Debug.Log("Disconnected from the server");
        StopAllCoroutines();
        //uiManager.DisconnectionPopup();
    }

    private void OnError(string response)
    {
        Debug.LogError("Error: " + response);
    }

    private void OnListenEvent(string data)
    {
        Debug.Log("Received some_event with data: " + data);
        ParseResponse(data);
    }
    private void OnSocketState(bool state)
    {
        if (state)
        {
            Debug.Log("my state is " + state);
        }
        else
        {

        }
    }
    private void OnSocketError(string data)
    {
        Debug.Log("Received error with data: " + data);
    }
    private void OnSocketAlert(string data)
    {
        Debug.Log("Received alert with data: " + data);
    }

    private void OnSocketOtherDevice(string data)
    {
        Debug.Log("Received Device Error with data: " + data);
        //uiManager.ADfunction();
    }

    private void SendPing()
    {
        InvokeRepeating("AliveRequest", 0f, 3f);
    }

    private void AliveRequest()
    {
        SendDataWithNamespace("YES I AM ALIVE");
    }

    private void ParseResponse(string jsonObject)
    {
        Debug.Log(string.Concat("<color=cyan><b>From SocketManager: ", jsonObject, "</b></color>"));
        Root myData = JsonConvert.DeserializeObject<Root>(jsonObject);
        string id = myData.id;

        switch (id)
        {
            case "InitData":
                {
                    initialData = myData.message.GameData;
                    initUIData = myData.message.UIData;
                    playerdata = myData.message.PlayerData;
                    //bonusdata = myData.message.BonusData;
                    if (!SetInit)
                    {
                        //Debug.Log(jsonObject);
                        //List<string> InitialReels = ConvertListOfListsToStrings(initUIData.paylines.symbols);
                        List<string> InitialReels = GetReelList(initUIData.paylines.symbols);
                        InitialReels = RemoveQuotes(InitialReels);
                        PopulateSlotSocket(InitialReels);
                        SetInit = true;
                    }
                    else
                    {
                        RefreshUI();
                    }
                    break;
                }
            case "ResultData":
                {
                    //myData.message.GameData.FinalResultReel = ConvertListOfListsToStrings(myData.message.GameData.ResultReel);
                    //myData.message.GameData.FinalsymbolsToEmit = TransformAndRemoveRecurring(myData.message.GameData.symbolsToEmit);
                    //Debug.Log(myData.message.GameData.resultSymbols);
                    resultData = myData.message.GameData;
                    playerdata = myData.message.PlayerData;
                    isResultdone = true;
                    break;
                }
            case "ExitUser":
                {
                    if (this.manager != null)
                    {
                        Debug.Log("Dispose my Socket");
                        this.manager.Close();
                    }
                    Application.ExternalCall("window.parent.postMessage", "onExit", "*");
                    break;
                }
        }
    }

    private void RefreshUI()
    {
        //uiManager.InitialiseUIData(initUIData.AbtLogo.link, initUIData.AbtLogo.logoSprite, initUIData.ToULink, initUIData.PopLink, initUIData.paylines);
    }

    private void PopulateSlotSocket(List<string> LineIds)
    {
        slotManager.shuffleInitialMatrix();

        Debug.Log(string.Concat("<color=blue><b>", LineIds.Count, "</b></color>"));
        //for (int i = 0; i < LineIds.Count; i++)
        //{
        //    //slotManager.FetchLines(LineIds[i], i);
        //    Debug.Log(string.Concat("<color=green><b>", i, "</b></color>"));
        //}

        slotManager.SetInitialUI();

        isLoaded = true;
        Application.ExternalCall("window.parent.postMessage", "OnEnter", "*");
    }

    internal void CloseSocket()
    {
        SendDataWithNamespace("EXIT");
        DOVirtual.DelayedCall(0.1f, () =>
        {
            if (this.manager != null)
            {
                this.manager.Close();
            }
        });
    }

    internal void AccumulateResult(double currBet)
    {
        isResultdone = false;
        MessageData message = new MessageData();
        message.data = new BetData();
        message.data.currentBet = currBet;
        message.data.spins = 1;
        message.data.currentLines = 3;
        message.id = "SPIN";
        // Serialize message data to JSON
        string json = JsonUtility.ToJson(message);
        SendDataWithNamespace("message", json);
    }

    private void SendDataWithNamespace(string eventName, string json = null)
    {
        // Send the message
        if (this.manager.Socket != null && this.manager.Socket.IsOpen)
        {
            if (json != null)
            {
                this.manager.Socket.Emit(eventName, json);
                Debug.Log(string.Concat("<color=yellow><b>", "JSON data sent: " + json, "</b></color>"));
            }
            else
            {
                this.manager.Socket.Emit(eventName);
            }
        }
        else
        {
            Debug.LogWarning("Socket is not connected.");
        }
    }

    private List<string> RemoveQuotes(List<string> stringList)
    {
        for (int i = 0; i < stringList.Count; i++)
        {
            stringList[i] = stringList[i].Replace("\"", ""); // Remove inverted commas
        }
        return stringList;
    }

    //private List<string> ConvertListListIntToListString(List<List<int>> listOfLists)
    //{
    //    List<string> resultList = new List<string>();

    //    foreach (List<int> innerList in listOfLists)
    //    {
    //        // Convert each integer in the inner list to string
    //        List<string> stringList = new List<string>();
    //        foreach (int number in innerList)
    //        {
    //            stringList.Add(number.ToString());
    //        }

    //        // Join the string representation of integers with ","
    //        string joinedString = string.Join(",", stringList.ToArray()).Trim();
    //        resultList.Add(joinedString);
    //    }

    //    return resultList;
    //}

    //private List<string> ConvertListOfListsToStrings(List<List<string>> inputList)
    //{
    //    List<string> outputList = new List<string>();

    //    foreach (List<string> row in inputList)
    //    {
    //        string concatenatedString = string.Join(",", row);
    //        outputList.Add(concatenatedString);
    //    }

    //    return outputList;
    //}

    private List<string> GetReelList(List<Symbol> m_List)
    {
        List<string> m_ResultList = new List<string>();

        foreach(var m in m_List)
        {
            m_ResultList.Add(m.ID.ToString());
        }

        return m_ResultList;
    }

    private List<string> TransformAndRemoveRecurring(List<List<string>> originalList)
    {
        // Flattened list
        List<string> flattenedList = new List<string>();
        foreach (List<string> sublist in originalList)
        {
            flattenedList.AddRange(sublist);
        }

        // Remove recurring elements
        HashSet<string> uniqueElements = new HashSet<string>(flattenedList);

        // Transformed list
        List<string> transformedList = new List<string>();
        foreach (string element in uniqueElements)
        {
            transformedList.Add(element.Replace(",", ""));
        }

        return transformedList;
    }
}

[Serializable]
public class BetData
{
    public double currentBet;
    public double currentLines;
    public double spins;
}

[Serializable]
public class AuthData
{
    public string GameID;
}

[Serializable]
public class MessageData
{
    public BetData data;
    public string id;
}

[Serializable]
public class ExitData
{
    public string id;
}

[Serializable]
public class InitData
{
    public AuthData Data;
    public string id;
}

[Serializable]
public class AbtLogo
{
    public string logoSprite { get; set; }
    public string link { get; set; }
}

public class GameData
{
    public List<double> Bets { get; set; }
    public List<int> autoSpin { get; set; }
    public List<List<int>> resultSymbols { get; set; }
    public bool isFreeSpin { get; set; }
    public int freeSpinCount { get; set; }
}

public class Message
{
    public GameData GameData { get; set; }
    public UIData UIData { get; set; }
    public PlayerData PlayerData { get; set; }
}

public class Paylines
{
    public List<Symbol> symbols { get; set; }
}

public class PlayerData
{
    public double Balance { get; set; }
    public double haveWon { get; set; }
    public double currentWining { get; set; }
    public double totalbet { get; set; }
}

public class Root
{
    public string id { get; set; }
    public Message message { get; set; }
    public string username { get; set; }
}

public class Symbol
{
    public int ID { get; set; }
    public string Name { get; set; }
    public object multiplier { get; set; }
    public object defaultAmount { get; set; }
    public object symbolsCount { get; set; }
    public object increaseValue { get; set; }
    public object description { get; set; }
    public object payout { get; set; }
    public object mixedPayout { get; set; }
    public object defaultPayout { get; set; }
}

public class Multiplier
{
}

public class IncreaseValue
{
}

public class DefaultAmount
{
}

public class DefaultPayout
{
}

public class Description
{
}

public class SymbolsCount
{
}

public class UIData
{
    public Paylines paylines { get; set; }
    public List<object> spclSymbolTxt { get; set; }
    public AbtLogo AbtLogo { get; set; }
    public string ToULink { get; set; }
    public string PopLink { get; set; }
}

[Serializable]
public class AuthTokenData
{
    public string cookie;
    public string socketURL;
}