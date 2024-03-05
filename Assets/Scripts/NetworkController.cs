using Newtonsoft.Json.Linq;
using System;
using UnityEngine;
using BestHTTP.WebSocket;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.Networking;
using TMPro;
using System.Threading;
using UnityEngine.SceneManagement;
using System.Linq;

public class NetworkController : MonoBehaviour
{

    public static NetworkController instance;
    private string wsaddress = "";
    private string httpaddress = "";
    public WebSocket webSocket;
    private bool lockReconnect = false;
    private Coroutine _pingCor, _clientPing, _serverPing;
    public GameController gameController;
    private int index = 0;
    public int userID = 0;
    private int roomID = 1;
    public TextMeshProUGUI textMeshProUGUI;
    public TMP_InputField tmp;
    public Camera startMenuCamera;
    private string userName;
    public ThrottlerDispatcher throttler = new ThrottlerDispatcher();
    public int flag = 7;
    // Start is called before the first frame update
    void Start()
    {
        tmp.onValueChanged.AddListener((arg) => { userName = arg; });
    }

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnCreateUserButtonClick()
    {
        throttler.Throttle(CreateUser);
    }
    private void CreateUser()
    {
        if (userName != "" && userName != null)
        {
            StartCoroutine( IECreateUser(userName));
        }
    }
    IEnumerator IECreateUser(string name)
    {
        UnityWebRequest req = UnityWebRequest.Get(httpaddress + "createuser?name=" + name);
        yield return req.SendWebRequest();
        JObject res = Loads(req);
        if ( (int)res["code"] == 200)
        {
            userID = (int)res["message"];
            CreateWebSocket();
        }
        else if ((int)res["code"] == 400)
        {
            //TODO
        }
    }

    private JObject Loads(UnityWebRequest req)
    {
        return JObject.Parse(req.downloadHandler.text);
    }

    private string MakeJson(int id, int code, string action = "", string message = "", string submessage = "")
    {
        return "{" + string.Format("\"id\":{0},\"code\":{1},\"action\":\"{2}\",\"message\":\"{3}\",\"submessage\":\"{4}\"", id,code,action,message,submessage) + "}";
    }

    void CreateWebSocket()
    {
        try
        {
            webSocket = new WebSocket(new Uri(wsaddress));
#if !UNITY_WEBGL
            webSocket.StartPingThread = true; 
#endif
            InitHandle();
            webSocket.Open();
        }
        catch (Exception e)
        {
            Debug.Log("websocket连接异常:" + e.Message);
            ReConnect();
        }

    }
    void OnOpen(WebSocket ws)
    {
        webSocket.Send(MakeJson(userID, 200, action: "加入房间", message: roomID.ToString()));
    }
    void InitHandle()
    {
        RemoveHandle();
        webSocket.OnOpen += OnOpen;
        webSocket.OnMessage += OnMessageReceived;
        webSocket.OnClosed += OnClosed;
        webSocket.OnError += OnError;
    }

    void RemoveHandle()
    {
        try
        {
            webSocket.OnOpen -= OnOpen;
            webSocket.OnMessage -= OnMessageReceived;
            webSocket.OnClosed -= OnClosed;
            webSocket.OnError -= OnError;
        }
        catch (Exception e)
        {
            print(e.Message);
        }
        
    }



    void OnMessageReceived(WebSocket ws, string message)
    {
        JObject data = Loads(message);
        print(message);
        if (data["message"].ToString() == "加入成功")
        {
            StartCoroutine(LoadRoomScene());
        } else if (data["action"].ToString() == "获取房间信息")
        {
            if ((int)data["submessage"] != 0)
            {
                gameController.UpdateEnemyName(name: data["message"].ToString());
                gameController.startButton.enabled = true;
                gameController.enemyID = (int)data["submessage"];
                gameController.enemyName= data["message"].ToString();
            }
            else
            {
                gameController.UpdateEnemyName(name: "");
                gameController.startButton.enabled = false;
            }
        } else if (data["action"].ToString() == "发牌")
        {
            gameController.actionQueue.AddAction(gameController.getCard((int)data["submessage"], data["message"].ToString(), (int)data["submessage2"]));
        } else if (data["action"].ToString() == "开始游戏")
        {
            gameController.GameStart();
        }else if (data["action"].ToString() == "你的回合")
        {
            gameController.actionQueue.AddAction(gameController.showChoose());
        }else if (data["action"].ToString()== "回合结束翻面")
        {
            gameController.actionQueue.AddAction(gameController.EndARound(data["message"].ToString(), data["submessage"].ToString()));
        }else if (data["action"].ToString() == "失败惩罚")
        {
            gameController.actionQueue.AddAction(gameController.UpdateChip(data["message"].ToString(), data["submessage"].ToString()));
        }else if (data["action"].ToString() == "获得王牌")
        {
            gameController.actionQueue.AddAction(gameController.getJoker(data["message"].ToString() == "unknown" ? gameController.enemyID : gameController.userID, data["message"].ToString(), (int)data["submessage"]));
        }else if (data["action"].ToString() == "使用王牌")
        {
            gameController.actionQueue.AddAction(gameController.showSpCard ((int)data["message"], (int)data["submessage2"],data["submessage"].ToString()));
        }else if (data["action"].ToString() == "返回牌库")
        {
            gameController.actionQueue.AddAction(gameController.BackToDeck((int)data["message"], (int)data["submessage"]));
        }else if (data["action"].ToString() == "交换数字牌")
        {
            gameController.actionQueue.AddAction(gameController.ChangeCardWithEnemy());
        }else if (data["action"].ToString() == "破坏王牌")
        {
            gameController.actionQueue.AddAction(gameController.BreakSpCard((int)data["message"], (int)data["submessage"]));
        }else if (data["action"].ToString() == "更新规则")
        {
            gameController.actionQueue.AddAction(gameController.UpdateRule((int)data["message"]));
        }else if ( data["action"].ToString() == "更新惩罚")
        {
            gameController.actionQueue.AddAction(gameController.UpdatePunish((int)data["message"], (int)data["submessage"]));
        }
    }


    IEnumerator LoadRoomScene()
    {
        yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Additive);//叠加方式加载转换场景
        yield return SceneManager.UnloadSceneAsync("StartMenu");
        Scene newScene = SceneManager.GetSceneByName("SampleScene");
        SceneManager.SetActiveScene(newScene);
        gameController = newScene.GetRootGameObjects().FirstOrDefault(x => x.name == "GameController").GetComponent<GameController>();
        gameController.networkController = this;
        webSocket.Send(MakeJson(userID, 200, action:"获取房间信息"));
        gameController.UpdateSelfName(userName);
        gameController.userID = userID;
        gameController.userName = userName;
    }


    void OnClosed(WebSocket ws, UInt16 code, string message)
    {
        Debug.LogFormat("OnClosed: code={0}, msg={1}", code, message);
        webSocket = null;
        ReConnect();
    }

    void OnError(WebSocket ws, string ex)
    {
        string errorMsg = string.Empty;
#if !UNITY_WEBGL || UNITY_EDITOR
        if (ws.InternalRequest.Response != null)
        {
            errorMsg = string.Format("Status Code from Server: {0} and Message: {1}", ws.InternalRequest.Response.StatusCode, ws.InternalRequest.Response.Message);
        }
#endif
        Debug.LogFormat("OnError: error occured: {0}\n", (ex != null ? ex : "Unknown Error " + errorMsg));
        webSocket = null;
        //ReConnect();

    }

    void ReConnect()
    {
        if (this.lockReconnect)
            return;
        this.lockReconnect = true;
        StartCoroutine(SetReConnect());
    }

    private IEnumerator SetReConnect()
    {
        Debug.Log("正在重连websocket");
        yield return new WaitForSeconds(5);
        CreateWebSocket();
        lockReconnect = false;
    }


    private JObject Loads(string mes)
    {
        return JObject.Parse(mes);
    }
}

public class ThrottlerDispatcher
{
    private bool isRunning;

    /// <summary>
    /// 函数节流
    /// </summary>
    /// <param name="action"></param>
    /// <param name="delay"></param>
    int delay = 500;
    public void Throttle(Action action)
    {
        if (!isRunning)
        {
            action?.Invoke();
            isRunning = true;
            new Timer(_ =>
            {
                isRunning = false;
            }, null, delay, Timeout.Infinite);
        }
    }
}
