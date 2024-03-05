using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;
using TMPro;
using System.Security.Cryptography;
using System.Linq;
using JetBrains.Annotations;
using System.Security;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using static UnityEngine.GraphicsBuffer;
using BestHTTP.WebSocket;
using System;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;

public class GameController : MonoBehaviour
{
    public UnityEngine.UI.Button joinRoomButton;
    public NetworkController networkController;
    public TMP_InputField usernameInput;
    private string baseUrl = "http://106.14.75.194:8080";
    public string status = "开始";
    public Dictionary<string, string> cardDescription = new Dictionary<string, string>();

    private string getRoom = "/room";
    private string roomBeat = "/roombeat";
    private string createRoom = "/new";
    private string joinRoom = "/join";
    private string startGame = "/startgame";
    private string gameBeat = "/gamebeat";
    private string gameDraw = "/game/draw";
    private string gamePass = "/game/pass";
    private string gameUse = "/game/use";

    private string username;
    public string token;
    private string enemyToken;
    private string user;
    private string nextAction = "搜索房间";
    private List<Card> selfCardFieldList = new  List<Card>();
    private List<Card> enemyCardFieldList = new List<Card>();
    private List<Card> selfHandList = new List<Card>();
    private List<Card> enemyHandList = new List<Card>();
    private List<Card> selfSpFieldList = new List<Card>();
    private List<Card> enemySpFieldList = new List<Card>();
    private int matchInit = 0;
    private bool waiting = false;
    private string turn = "";

    public float offset = 10;
    private int index;
    public float moveDuration = 0.4f;

    public Canvas roomMenu;
    public Canvas performMenu;
    public Canvas showCardMenu;
    public TextMeshProUGUI selfname;
    public TextMeshProUGUI enemyname;
    public Canvas startButton;
    public Canvas description;

    public GameObject selfDeck;
    public GameObject enemyDeck;
    public GameObject selfCardField;
    public GameObject enemyCardField;
    public Canvas chips;
    public Card cardPrefab;
    public TextMeshProUGUI selfChip;
    public TextMeshProUGUI enemyChip;
    public TextMeshProUGUI selfSubChip;
    public TextMeshProUGUI enemySubChip;
    public TextMeshProUGUI selfScore;
    public TextMeshProUGUI enemyScore;
    public GameObject selfHand;
    public GameObject enemyHand;
    public GameObject selfSpField;  
    public GameObject enemySpField;
    public Canvas score;
    public bool canUseSpCard;
    public string gameStatus = "Waitting";

    public bool netLock = false ;
    public string userName;
    public int userID;
    public string enemyName;
    public int enemyID;
    public ActionQueue actionQueue;

    // Start is called before the first frame update
    void Start()
    {
        description.enabled = false;
        chips.enabled = false;
        canUseSpCard = false;
        showCardMenu.enabled = false;
        score.enabled = false;
        chips.enabled = false;
        performMenu.enabled = false;
        enemyDeck.SetActive(false);
        selfDeck.SetActive(false);
        startButton.enabled = false;
        roomMenu.enabled = true;
        actionQueue = ActionQueue.InitOneActionQueue();
        actionQueue.StartQueue();
        cardDescription.Add("0", "未知");
        cardDescription.Add("1", "1");
        cardDescription.Add("2", "2");
        cardDescription.Add("3", "3");
        cardDescription.Add("4", "4");
        cardDescription.Add("5", "5");
        cardDescription.Add("6", "6");
        cardDescription.Add("7", "7");
        cardDescription.Add("8", "8");
        cardDescription.Add("9", "9");
        cardDescription.Add("10", "10");
        cardDescription.Add("11", "11");
        cardDescription.Add("enemyChipPlusTwo", "对手筹码加2");
        cardDescription.Add("selfChipSubOne", "自己筹码减1");
        cardDescription.Add("bothChipSubTwo", "双方筹码减2");
        cardDescription.Add("selfHandGet1", "若牌库中有1，获得1");
        cardDescription.Add("selfHandGet3", "若牌库中有3，获得3");
        cardDescription.Add("selfHandGet5", "若牌库中有5，获得5");
        cardDescription.Add("selfHandReturn1", "将自己最后一张牌送回牌库");
        cardDescription.Add("enemyHandReturn1", "将对手最后一张牌送回牌库");
        cardDescription.Add("change1HandWithEnemy", "与对手交换最后一张牌");
        cardDescription.Add("breakEnemyLastSpCard", "破坏对手最后一张场上的王牌");
        cardDescription.Add("changeRuleTo17", "将游戏规则改为最靠近17点的获胜");
        cardDescription.Add("changeRuleTo24", "将游戏规则改为最靠近24点的获胜");
        cardDescription.Add("changeRuleTo27", "将游戏规则改为最靠近27点的获胜");
        cardDescription.Add("enemyChipPlusEnemySpCardDivTwo", "对手的筹码加上对手手中王牌数量除二");
        cardDescription.Add("getTwoSpCardAndChipPlusTwo", "自己获得两张王牌，同时自己筹码加二（被破坏后不需要归还王牌）");
        //       StartCoroutine(HeartBeat());

    }

    private JObject Loads(string mes)
    {
        return JObject.Parse(mes);
    }

    IEnumerator HeartBeat()
    {
        while (true)
        {
            JObject res;
            if (nextAction == "搜索房间")
            {
                description.enabled = false;
                chips.enabled = false;
                canUseSpCard = false;
                showCardMenu.enabled = false;
                score.enabled = false;
                chips.enabled = false;
                performMenu.enabled = false;
                enemyDeck.SetActive(false);
                selfDeck.SetActive(false);
                roomMenu.enabled = false;
                UnityWebRequest req = UnityWebRequest.Get(baseUrl + getRoom);
                yield return req.SendWebRequest();
                res = JObject.Parse(req.downloadHandler.text);
                if ((string)res["message"] == "无房间")
                {
                    joinRoomButton.GetComponentInChildren<TextMeshProUGUI>().text = "创建房间";
                }
                else
                {
                    joinRoomButton.GetComponentInChildren<TextMeshProUGUI>().text = "加入房间";
                }
            }
            else if (nextAction == "加入或创建房间")
            {
                username = usernameInput.text;
                token = username + UnityEngine.Random.Range(0, 1000000).ToString();
                user = "?" + "name=" + username + "&token=" + token;
                if (joinRoomButton.GetComponentInChildren<TextMeshProUGUI>().text == "创建房间")
                {
                    UnityWebRequest req = UnityWebRequest.Get(baseUrl + createRoom + user);
                    yield return req.SendWebRequest();
                    res = JObject.Parse(req.downloadHandler.text);
                }
                else
                {
                    UnityWebRequest req = UnityWebRequest.Get(baseUrl + joinRoom + user);
                    yield return req.SendWebRequest();
                    res = JObject.Parse(req.downloadHandler.text);
                }
                if ((int)res["code"] == 200)
                {
                    nextAction = "房间预备";

                }
                else nextAction = "搜索房间";
            }
            else if (nextAction == "房间预备")
            {
                roomMenu.enabled = true;
                UnityWebRequest req = UnityWebRequest.Get(baseUrl + roomBeat + user);
                yield return req.SendWebRequest();
                res = JObject.Parse(req.downloadHandler.text);
                if ((int)res["isPlaying"] == 1)
                {
                    nextAction = "游戏中";
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }
                if ((res["gamersToken"].ToArray().GetValue(0).ToString() == token) && (res["gamersToken"].ToArray().Length == 2))
                {
                    startButton.enabled = true;
                }
                else
                {
                    startButton.enabled=false;
                }
                for (int i = 0; i < res["gamers"].ToArray().Length; i++)
                {
                    if (res["gamersToken"].ToArray().GetValue(i).ToString() == token)
                    {
                        selfname.text = res["gamers"].ToArray().GetValue(i).ToString();
                    }
                    else
                    {
                        enemyname.text = res["gamers"].ToArray().GetValue(i).ToString();
                    }
                }
            }
            else if (nextAction == "开始游戏")
            {
                UnityWebRequest req = UnityWebRequest.Get(baseUrl + startGame);
                yield return req.SendWebRequest();
                res = JObject.Parse(req.downloadHandler.text);
                if ((int)res["code"] == 200)
                {
                    nextAction = "游戏中";
                    startButton.enabled = false;
                }
            }
            else if (nextAction == "游戏中")
            {
                score.enabled = false;
                chips.enabled = true;
                enemyDeck.SetActive(true);
                selfDeck.SetActive(true);
                startButton.enabled = false;
                UnityWebRequest req = UnityWebRequest.Get(baseUrl + gameBeat + user);
                yield return req.SendWebRequest();
                res = JObject.Parse(req.downloadHandler.text);
                if ((int)res["code"] != 200 || (int)res["index"] <= index) continue;
                if(matchInit == 1)
                {
                    turn = res["gameStatus"]["turn"].ToString();
                    selfSubChip.text = res["gameStatus"]["chipSub"][token].ToString();
                    enemySubChip.text = res["gameStatus"]["chipSub"][enemyToken].ToString();
                }
                if ((int)res["gameStatus"]["end"] == 1)
                {
                    selfCardFieldList[0].kind = res["gameStatus"]["lastMatch"][token].ToString();
                    enemyCardFieldList[0].kind = res["gameStatus"]["lastMatch"][enemyToken].ToString();
                    enemyCardFieldList[0].LoadFace();
                    selfCardFieldList[0].LoadFace();
                    selfCardFieldList[0].GetComponent<Animation>().Play("turn_up");
                    enemyCardFieldList[0].GetComponent<Animation>().Play("turn_up");
                    yield return new WaitForSeconds(1);
                    selfChip.text = "手指数：" + res["gameStatus"]["chip"][token].ToString();
                    enemyChip.text = "手指数：" + res["gameStatus"]["chip"][enemyToken].ToString();
                    enemyScore.text = "0";
                    selfScore.text = "0";
                    foreach (var item in enemyCardFieldList)
                    {
                        enemyScore.text = (int.Parse(enemyScore.text)+int.Parse(item.kind)).ToString();
                    }
                    foreach (var item in selfCardFieldList)
                    {
                        selfScore.text = (int.Parse(selfScore.text) + int.Parse(item.kind)).ToString();
                    }
                    score.enabled = true;
                    yield return new WaitForSeconds(4);
                    foreach (var card in selfCardFieldList)
                    {
                        card.dis();
                    }
                    foreach (var card in enemyCardFieldList) { card.dis(); }
                    score.enabled = false;
                    matchInit = 0;
                    selfCardFieldList.Clear();
                    enemyCardFieldList.Clear();
                }
        /*        if (matchInit == 0)
                {
                    foreach (var x in JObject.Parse(res["gameStatus"]["cardField"].ToString()))
                    {
                        if (token != x.Key)
                        {
                            enemyToken = x.Key;
                        }
                    }
                    selfSubChip.text = "1";
                    enemyChip.text = "1";
                    Card newcard = makeCard(token, res["gameStatus"]["cardField"][token].ToArray().GetValue(0).ToString(),0);
                    StartCoroutine(drawCard(newcard));
                    selfCardFieldList.Add(newcard);

                    newcard = makeCard(enemyToken, res["gameStatus"]["cardField"][enemyToken].ToArray().GetValue(0).ToString(), 0);
                    StartCoroutine(drawCard(newcard));
                    enemyCardFieldList.Add(newcard);

                    yield return new WaitForSeconds(moveDuration);

                    newcard = makeCard(token, res["gameStatus"]["cardField"][token].ToArray().GetValue(1).ToString(), 0);
                    StartCoroutine(drawCard(newcard));
                    selfCardFieldList.Add(newcard);
                   
                    newcard = makeCard(enemyToken, res["gameStatus"]["cardField"][enemyToken].ToArray().GetValue(1).ToString(), 0);
                    StartCoroutine(drawCard(newcard));
                    enemyCardFieldList.Add(newcard);
   
                    yield return new WaitForSeconds(moveDuration);
                    matchInit = 1;
                }

                //使用王牌
                if (res["gameStatus"]["lastUseCard"].ToString() != "null")
                {
                    yield return StartCoroutine(useSpCard(res["gameStatus"]["lastUseCard"]["owner"].ToString(), res["gameStatus"]["lastUseCard"]["id"].ToString(), res["gameStatus"]["lastUseCardKind"].ToString(), res["gameStatus"]["lastUseCard"]["card"].ToString()));
                }

                //增加手中王牌
                if (res["gameStatus"]["hand"][token].ToArray().Length > selfHandList.Count)
                {
                    int oldLen = selfHandList.Count;
                    int newLen = res["gameStatus"]["hand"][token].ToArray().Length;
                    List<Dictionary<string, string>> cardToken = (JArray.Parse(res["gameStatus"]["hand"][token].ToString())).ToObject<List<Dictionary<string, string>>>();
                    for (int i = oldLen; i < newLen; i++)
                    {
                        Card newCard = makeCard(token, cardToken[i]["card"],1);
                        newCard.id = cardToken[i]["id"].ToString();
                        yield return StartCoroutine(drawSpCard(newCard));
                        selfHandList.Add(newCard);
                    }
                }
                if (res["gameStatus"]["hand"][enemyToken].ToArray().Length > enemyHandList.Count)
                {
                    int oldLen = enemyHandList.Count;
                    int newLen = res["gameStatus"]["hand"][enemyToken].ToArray().Length;
                    List<Dictionary<string, string>> cardToken = (JArray.Parse(res["gameStatus"]["hand"][enemyToken].ToString())).ToObject<List<Dictionary<string, string>>>();
                    for (int i = oldLen; i < newLen; i++)
                    {
                        Card newCard = makeCard(enemyToken, cardToken[i]["card"],1);
                        newCard.id = cardToken[i]["id"].ToString();
                        yield return StartCoroutine(drawSpCard(newCard));
                        enemyHandList.Add(newCard);
                    }
                }

                //减少手中王牌
                if (res["gameStatus"]["hand"][token].ToArray().Length < selfHandList.Count)
                {
                    int oldLen = selfHandList.Count;
                    int newLen = res["gameStatus"]["hand"][token].ToArray().Length;
                    for (int i = oldLen-1; i >= newLen; i--)
                    {
                        Card delCard = selfHandList[i];
                        selfHandList.Remove(delCard);
                        delCard.dis();
                    }
                }
                if (res["gameStatus"]["hand"][enemyToken].ToArray().Length < enemyHandList.Count)
                {
                    int oldLen = enemyHandList.Count;
                    int newLen = res["gameStatus"]["hand"][enemyToken].ToArray().Length;
                    for (int i = oldLen - 1; i >= newLen; i--)
                    {
                        Card delCard = enemyHandList[i];
                        enemyHandList.Remove(delCard);
                        delCard.dis();
                    }
                }

                //增加场上数字牌
                if (res["gameStatus"]["cardField"][token].ToArray().Length > selfCardFieldList.Count)
                {
                    int oldLen = selfCardFieldList.Count;
                    int newLen = res["gameStatus"]["cardField"][token].ToArray().Length;
                    for (int i =  oldLen ; i < newLen; i++)
                    {
                        Card newCard = makeCard(token, res["gameStatus"]["cardField"][token].ToArray().GetValue(i).ToString(),0);
                        yield return StartCoroutine(drawCard(newCard));
                        selfCardFieldList.Add(newCard);
                    }
                }
                if (res["gameStatus"]["cardField"][enemyToken].ToArray().Length > enemyCardFieldList.Count)
                {
                    int oldLen = enemyCardFieldList.Count;
                    int newLen = res["gameStatus"]["cardField"][enemyToken].ToArray().Length;
                    for (int i = oldLen ; i < newLen; i++)
                    {
                        Card newCard = makeCard(enemyToken, res["gameStatus"]["cardField"][enemyToken].ToArray().GetValue(i).ToString(), 0);
                        yield return StartCoroutine(drawCard(newCard));
                        enemyCardFieldList.Add(newCard);
                    }
                }
*/
                //减少场上数字牌
                if (res["gameStatus"]["cardField"][token].ToArray().Length < selfCardFieldList.Count)
                {
                    int oldLen = selfCardFieldList.Count;
                    int newLen = res["gameStatus"]["cardField"][token].ToArray().Length;
                    for (int i = oldLen-1; i >= newLen; i--)
                    {
                        Card delCard = selfCardFieldList[i];
                        yield return StartCoroutine(returnCard(delCard));
                        selfCardFieldList.Remove(delCard);
                        delCard.dis();
                    }
                }
                if (res["gameStatus"]["cardField"][enemyToken].ToArray().Length < enemyCardFieldList.Count)
                {
                    int oldLen = enemyCardFieldList.Count;
                    int newLen = res["gameStatus"]["cardField"][enemyToken].ToArray().Length;
                    for (int i = oldLen-1; i >= newLen; i--)
                    {
                        Card delCard = enemyCardFieldList[i];
                        yield return StartCoroutine(returnCard(delCard));
                        enemyCardFieldList.Remove(delCard);
                        delCard.dis();
                    }
                }
                selfChip.text = "手指数：" + res["gameStatus"]["chip"][token].ToString();
                enemyChip.text = "手指数："+ res["gameStatus"]["chip"][enemyToken].ToString();
                selfSubChip.text = res["gameStatus"]["chipSub"][token].ToString();
                enemySubChip.text = res["gameStatus"]["chipSub"][enemyToken].ToString();
                if (res["gameStatus"]["turn"].ToString() == token) {
                    waiting = true;
                    performMenu.enabled = true;
                    canUseSpCard = true;
                    yield return new WaitWhile(() =>waiting);
                }
                index = (int)res["index"];
            }
            yield return new WaitForSeconds(0.8f);
        }
    }


    public bool IsNumeric(string input)
    {
        Regex regex = new Regex("^[0-9]+$");
        return regex.IsMatch(input);
    }
    IEnumerator drawCard(Card card)
    {
        print(card.owner );
        print(userID);
        if (card.owner == userID)
        {
            int target = selfCardFieldList.Count;
            float eTime = 0;
            Vector3 initialPos = card.transform.position;
            if(card.kind!="0") card.GetComponent<Animation>().Play("turn_up");
            while(eTime < moveDuration)
            {
                Vector3 v = new Vector3(offset * target, 0, 0);
                float t = eTime / moveDuration;
                Vector3 newPos = Vector3.Lerp(initialPos, selfCardField.transform.position + v,t);
                card.transform.position = newPos;
                yield return null;
                eTime += Time.deltaTime;
            }
            yield break;
        }
        else
        {
            int target = enemyCardFieldList.Count;
            float eTime = 0;
            Vector3 initialPos = card.transform.position;
            if (card.kind != "0") card.GetComponent<Animation>().Play("turn_up");
            while (eTime < moveDuration)
            {
                Vector3 v = new Vector3(offset * target, 0, 0);
                float t = eTime / moveDuration;
                Vector3 newPos = Vector3.Lerp(initialPos, enemyCardField.transform.position - v, t);
                card.transform.position = newPos;
                yield return null;
                eTime += Time.deltaTime;
            }
            yield break;
        }
    }

    /*IEnumerator useSpCard(string owner,string id,string k,string kind)
    {
        if(owner == token)
        {
            foreach(var i in selfHandList)
            {
                if (i.id == id){
                    yield return StartCoroutine(showSpCard(i));
                    if(i.kind == "change1HandWithEnemy")
                    {
                        yield return StartCoroutine(changeCardWithEnemy());
                    }
                    if(k == "inField")
                    {
                        float eTime = 0;
                        Vector3 initialPos = i.transform.position;
                        if (i.kind != "-1") i.GetComponent<Animation>().Play("turn_up");
                        while (eTime < moveDuration)
                        {
                            Vector3 v = new Vector3(offset * selfSpFieldList.Count, 0, 0);
                            float t = eTime / moveDuration;
                            Vector3 newPos = Vector3.Lerp(initialPos, selfSpField.transform.position + v, t);
                            i.transform.position = newPos;
                            yield return null;
                            eTime += Time.deltaTime;
                        }
                        selfSpFieldList.Add(i);
                        selfHandList.Remove(i);
                    }
                    else
                    {
                        selfHandList.Remove(i);
                        i.dis();
                    }
                    for(int j = 0; j < selfHandList.Count; j++)
                    {
                        Vector3 newPos = new Vector3(offset * j ,0 ,0);
                        selfHandList[j].transform.position = newPos + selfHand.transform.position;
                    }
                    yield break;
                }
            }
        }
        else
        {
            foreach (var i in enemyHandList)
            {
                if (i.id == id)
                {
                    i.kind = kind;
                    i.LoadFace();
                    yield return StartCoroutine(showSpCard(i));
                    if (i.kind == "change1HandWithEnemy")
                    {
                        yield return StartCoroutine(changeCardWithEnemy());
                    }
                    if (k == "inField")
                    {
                        float eTime = 0;
                        Vector3 initialPos = i.transform.position;
                        if (i.kind != "-1") i.GetComponent<Animation>().Play("turn_up");
                        while (eTime < moveDuration)
                        {
                            Vector3 v = new Vector3(offset * enemySpFieldList.Count, 0, 0);
                            float t = eTime / moveDuration;
                            Vector3 newPos = Vector3.Lerp(initialPos, enemySpField.transform.position - v, t);
                            i.transform.position = newPos;
                            yield return null;
                            eTime += Time.deltaTime;
                        }
                        enemySpFieldList.Add(i);
                        enemyHandList.Remove(i);
                    }
                    else
                    {
                        enemyHandList.Remove(i);
                        i.dis();
                    }
                    for (int j = 0; j < enemyHandList.Count; j++)
                    {
                        Vector3 newPos = new Vector3(offset * j, 0, 0);
                        enemyHandList[j].transform.position = newPos - enemyHand.transform.position;
                    }
                    yield break;
                }
            }
        }
    }*/

    IEnumerator drawSpCard(Card card)
    {
        if (card.owner == userID)
        {
            yield return StartCoroutine(drawCard(card));
            yield return new WaitForSeconds(0.5f);
            Vector3 vv =new Vector3(offset * selfHandList.Count, 0, 0);
            card.transform.position = selfHand.transform.position + vv;
            card.GetComponent<Animation>().Play("turn_up");
            yield break;
        }
        else
        {
            yield return StartCoroutine(drawCard(card));
            yield return new WaitForSeconds(0.5f);
            Vector3 vv = new Vector3(offset * enemyHandList.Count, 0, 0);
            card.transform.position = enemyHand.transform.position - vv;
            yield break;
        }
    }

    IEnumerator returnCard(Card card)
    {
        if (card.owner == userID)
        {
            float eTime = 0;
            Vector3 initialPos = card.transform.position;
            if (card.kind != "0") card.GetComponent<Animation>().Play("turn_down");
            while (eTime < moveDuration)
            {
                float t = eTime / moveDuration;
                Vector3 newPos = Vector3.Lerp(initialPos,selfDeck.transform.position , t);
                card.transform.position = newPos;
                yield return null;
                eTime += Time.deltaTime;
            }
            yield break;
        }
        else
        {
            float eTime = 0;
            Vector3 initialPos = card.transform.position;
            if (card.kind != "0") card.GetComponent<Animation>().Play("turn_down");
            while (eTime < moveDuration)
            {
                float t = eTime / moveDuration;
                Vector3 newPos = Vector3.Lerp(initialPos,enemyDeck.transform.position , t);
                card.transform.position = newPos;
                yield return null;
                eTime += Time.deltaTime;
            }
            yield break;
        }
    }

    public IEnumerator showSpCard(int cardID,int ownerID,string kind)
    {
        Card card=null;
        if(ownerID == userID)
        {
            foreach(var i in selfHandList)
            {
                if(i.id == cardID)
                {
                    card = i;
                    selfHandList.Remove(i);
                    for (int j = 0; j < selfHandList.Count; j++)
                    {
                        Vector3 newPos = new Vector3(offset * j, 0, 0);
                        selfHandList[j].transform.position = newPos + selfHand.transform.position;
                    }
                    break;
                }
            }
        }
        else
        {
            foreach (var i in enemyHandList)
            {
                if (i.id == cardID)
                {
                    card = i;
                    enemyHandList.Remove(i);
                    card.kind = kind;
                    card.LoadFace();
                    card.TurnUp();
                    for (int j = 0; j < enemyHandList.Count; j++)
                    {
                        Vector3 newPos = new Vector3(offset * j, 0, 0);
                        enemyHandList[j].transform.position = newPos + enemyHand.transform.position;
                    }
                    break;
                }
            }
        }
        GameObject cardImage = GameObject.Find("cardImage");
        GameObject cardDes = GameObject.Find("cardDes");
        cardImage.GetComponent<UnityEngine.UI.Image>().sprite = card.GetComponent<SpriteRenderer>().sprite;
        cardDes.GetComponent<TextMeshProUGUI>().text = cardDescription[card.kind];
        showCardMenu.enabled = true;
        yield return new WaitForSeconds(3);
        showCardMenu.enabled = false;
        actionQueue.AddAction(HandleSpCard(card));
    }

    IEnumerator HandleSpCard(Card card)
    {
        bool flag = false;
        foreach (string i in Card.inFieldCard)
        {
            if (i == card.kind)
            {
                flag = true;
                break;
            }
        }
        if (!flag)
        {
            card.dis();
        }
        else
        {
            if(card.owner == userID)
            {
                float eTime = 0;
                Vector3 initialPos = card.transform.position;
                card.GetComponent<Animation>().Play("turn_up");
                while (eTime < moveDuration)
                {
                    Vector3 v = new Vector3(offset * selfSpFieldList.Count, 0, 0);
                    float t = eTime / moveDuration;
                    Vector3 newPos = Vector3.Lerp(initialPos, selfSpField.transform.position + v, t);
                    card.transform.position = newPos;
                    yield return null;
                    eTime += Time.deltaTime;
                }
                selfSpFieldList.Add(card);
            }
            else
            {
                float eTime = 0;
                Vector3 initialPos = card.transform.position;
                card.GetComponent<Animation>().Play("turn_up");
                while (eTime < moveDuration)
                {
                    Vector3 v = new Vector3(offset * enemySpFieldList.Count, 0, 0);
                    float t = eTime / moveDuration;
                    Vector3 newPos = Vector3.Lerp(initialPos, enemySpField.transform.position - v, t);
                    card.transform.position = newPos;
                    yield return null;
                    eTime += Time.deltaTime;
                }
                enemySpFieldList.Add(card);
            }
                
        }
        yield break;
    }



    public void Draw() {
        StartCoroutine(DrawIE());
    }
    public void Pass()
    {
        StartCoroutine (PassIE());
    }
    IEnumerator DrawIE()
    {
        UnityWebRequest req = UnityWebRequest.Get(baseUrl + gameDraw + user);
        yield return req.SendWebRequest();
        turn = "";
        waiting = false;
        performMenu.enabled = false;
        yield break;
    }

    IEnumerator PassIE()
    {
        UnityWebRequest req = UnityWebRequest.Get(baseUrl + gamePass + user);
        yield return req.SendWebRequest();
        turn = "";
        waiting = false;
        performMenu.enabled = false;
        yield break;
    }

    public void Use(Card card)
    {
        actionQueue.AddAction(hideChoose());
        actionQueue.AddAction(UseIE(card.id));
        actionQueue.AddAction(showSpCard(card.id,userID,card.kind));
    }

    IEnumerator UseIE(int  cardID)
    {
        networkController.webSocket.Send(MakeJson(userID, 200, action: "使用王牌", message: cardID.ToString()));
        yield break;
    }

    public IEnumerator BackToDeck(int cardID,int owner)
    {
        if (owner == userID)
        {
            foreach (Card card in selfCardFieldList)
            {
                if (card.id == cardID)
                {
                    yield return StartCoroutine(returnCard(card));
                    selfCardFieldList.Remove(card);
                    card.dis();
                    break;
                }
            }
        }
        else
        {
            foreach (Card card in enemyCardFieldList)
            {
                if (card.id == cardID)
                {
                    yield return StartCoroutine(returnCard(card));
                    enemyCardFieldList.Remove(card);
                    card.dis();
                    break;
                }
            }
        }
    }

    public IEnumerator ChangeCardWithEnemy()
    {
        Card scard = selfCardFieldList.Last();
        Card ecard = enemyCardFieldList.Last();
        selfCardFieldList.RemoveAt(selfCardFieldList.Count - 1);
        enemyCardFieldList.RemoveAt(enemyCardFieldList.Count - 1);
        selfCardFieldList.Add(ecard);
        enemyCardFieldList.Add(scard);
        float eTime = 0;
        Vector3 scardPos = scard.transform.position;
        Vector3 ecardPos = ecard.transform.position;
        while (eTime < moveDuration)
        {
            float t = eTime / moveDuration;
            Vector3 newsCardPos = Vector3.Lerp(scardPos, ecardPos, t);
            Vector3 neweCardPos = Vector3.Lerp(ecardPos, scardPos, t);
            scard.transform.position = newsCardPos;
            ecard.transform.position = neweCardPos;
            yield return null;
            eTime += Time.deltaTime;
        }
        yield break;
    }

    public IEnumerator BreakSpCard(int cardID,int owner)
    {
        if(owner == userID)
        {
            foreach(Card card in selfSpFieldList) {
                if(card.id == cardID)
                {
                    selfSpFieldList.Remove(card);
                    card.dis();
                    break;
                }
            }
        }
        else
        {
            foreach(Card card in enemySpFieldList)
            {
                if (card.id == cardID)
                {
                    enemySpFieldList.Remove(card);
                    card.dis();
                    break;
                }
            }
        }
        yield break;
    }

    public void UpdateSelfName(string name)
    {
        selfname.text = name;
    }

    public void UpdateEnemyName(string name)
    {
        enemyname.text = name;
    }

    public void GameStartButton()
    {
        networkController.throttler.Throttle(SendGameStart);
    }

    private void SendGameStart()
    {
        networkController.webSocket.Send(MakeJson(userID, 200, action: "开始游戏"));
    }
    private string MakeJson(int id, int code, string action = "", string message = "", string submessage = "")
    {
        return "{" + string.Format("\"id\":{0},\"code\":{1},\"action\":\"{2}\",\"message\":\"{3}\",\"submessage\":\"{4}\"", id, code, action, message, submessage) + "}";
    }

    public void GameStart()
    {
        gameStatus = "inGame";
        score.enabled = false;
        chips.enabled = true;
        selfChip.text = "手指数：" + "10";
        enemyChip.text = "手指数：" + "10";
        enemyDeck.SetActive(true);
        selfDeck.SetActive(true);
        startButton.enabled = false;
    }
    private Card makeCard(int ownerID, string kind,int cardID)
    {
        if (ownerID == userID)
        {
            Card newCard = Instantiate(cardPrefab, selfDeck.transform.position, Quaternion.identity);
            newCard.LoadFace();
            newCard.kind = kind;
            newCard.id = cardID;
            newCard.owner = ownerID;
            return newCard;
        }
        else
        {
            Card newCard = Instantiate(cardPrefab, enemyDeck.transform.position, Quaternion.identity);
            newCard.LoadFace();
            newCard.kind = kind;
            newCard.id = cardID;
            newCard.owner = ownerID;
            return newCard;
        }
    }
    public IEnumerator getCard(int ownerID, string cardKind,int cardID)
    {
        Card card = makeCard(ownerID, cardKind,cardID);
        yield return StartCoroutine( drawCard(card));
        if(ownerID == userID)
        {
            selfCardFieldList.Add(card);
        }else
        {
            enemyCardFieldList.Add(card);
        }

    }

    public IEnumerator getJoker(int ownerID,string cardKind,int cardID)
    {
        Card card = makeCard(ownerID, cardKind, cardID);
        yield return StartCoroutine(drawSpCard(card));
        if(ownerID == userID)
        {
            selfHandList.Add(card);
        }
        else
        {
            enemyHandList.Add(card);
        }
    }

    public IEnumerator showChoose()
    {
        performMenu.enabled = true;
        canUseSpCard = true;
        yield return null;
    }

    public IEnumerator hideChoose()
    {
        performMenu.enabled = false;
        canUseSpCard = false;
        yield return null;
    }

    public IEnumerator UpdateChip(string selfChipCnt,string enemyChipCnt)
    {
        selfChip.text = "手指数：" + selfChipCnt;
        enemyChip.text = "手指数：" + enemyChipCnt;
        yield return null;
    }

    public IEnumerator UpdateRule(int rule)
    {
        //TODO
        yield return null;
    }
    public IEnumerator UpdatePunish(int selfPunish,int enemyPunish)
    {
        selfSubChip.text = selfPunish.ToString();
        enemySubChip.text=enemyPunish.ToString();
        yield return null;
    }

    public IEnumerator EndARound(string selfHideCardKind,string enemyHideCardKind)
    {
        selfCardFieldList[0].kind = selfHideCardKind;
        enemyCardFieldList[0].kind = enemyHideCardKind;
        enemyCardFieldList[0].LoadFace();
        selfCardFieldList[0].LoadFace();
        selfCardFieldList[0].GetComponent<Animation>().Play("turn_up");
        enemyCardFieldList[0].GetComponent<Animation>().Play("turn_up");
        enemyScore.text = "0";
        selfScore.text = "0";
        foreach (var item in enemyCardFieldList)
        {
            enemyScore.text = (int.Parse(enemyScore.text) + int.Parse(item.kind)).ToString();
        }
        foreach (var item in selfCardFieldList)
        {
            selfScore.text = (int.Parse(selfScore.text) + int.Parse(item.kind)).ToString();
        }
        score.enabled = true;
        yield return new WaitForSeconds(4);
        foreach (var card in selfCardFieldList)
        {
            card.dis();
        }
        foreach (var card in enemyCardFieldList) { card.dis(); }
        foreach (var card in selfSpFieldList) { card.dis(); }
        foreach (var card in enemySpFieldList) { card.dis(); }
        score.enabled = false;
        selfCardFieldList.Clear();
        enemyCardFieldList.Clear();
        selfSpFieldList.Clear();
        enemySpFieldList.Clear();
    }

    public void ChooseGetButton()
    {
        networkController.throttler.Throttle(ChooseGet);
    }
    private void ChooseGet() {
        networkController.webSocket.Send(MakeJson(userID, 200, action:"要牌"));
        actionQueue.AddAction(hideChoose());
    }

    public void ChoosePassButton()
    {
        networkController.throttler.Throttle(ChoosePass);
    }
    private void ChoosePass()
    {
        networkController.webSocket.Send(MakeJson(userID, 200, action: "不要牌"));
        actionQueue.AddAction(hideChoose());
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        if (hit.collider != null && hit.collider.gameObject.GetComponent<Card>().kind != "0" && hit.collider.gameObject.GetComponent<Card>().kind != "unknown")
        {
            description.GetComponentInChildren<TextMeshProUGUI>().text = cardDescription[hit.collider.gameObject.GetComponent<Card>().kind];
            description.enabled = true;
        }
        else if (hit.collider == null)
        {
            description.enabled = false;
        }

    }
}


 
/// <summary>
/// 任务执行队列
/// </summary>
public class ActionQueue : MonoBehaviour
{
    event Action onComplete;
    List<OneAction> actions = new List<OneAction>();
    public static ActionQueue InitOneActionQueue()
    {
        return new GameObject().AddComponent<ActionQueue>();
    }
    /// <summary>
    /// 添加一个任务到队列
    /// </summary>
    /// <param name="startAction">开始时执行的方法</param>
    /// <param name="IsCompleted">判断该节点是否完成</param>
    /// <returns></returns>
    public ActionQueue AddAction(Action startAction, Func<bool> IsCompleted)
    {
        actions.Add(new OneAction(startAction, IsCompleted));
        return this;
    }
    /// <summary>
    /// 添加一个协程方法到队列
    /// </summary>
    /// <param name="enumerator">一个协程</param>
    /// <returns></returns>
    public ActionQueue AddAction(IEnumerator enumerator)
    {
        actions.Add(new OneAction(enumerator));
        return this;
    }
    /// <summary>
    /// 添加一个任务到队列
    /// </summary>
    /// <param name="action">一个方法</param>
    /// <returns></returns>
    public ActionQueue AddAction(Action action)
    {
        actions.Add(new OneAction(action));
        return this;
    }

    /// <summary>
    /// 绑定执行完毕回调
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    public ActionQueue BindCallback(Action callback)
    {
        onComplete += callback;
        return this;
    }
    /// <summary>
    /// 开始执行队列
    /// </summary>
    /// <returns></returns>
    public ActionQueue StartQueue()
    {
        StartCoroutine(StartQueueAsync());
        return this;
    }

    IEnumerator StartQueueAsync()
    {
        while (true)
        {
            if (actions.Count > 0)
            {
                print("执行协程");
                yield return StartCoroutine( actions[0].enumerator);
                actions.RemoveAt(0);
            }
            yield return null;
        }
    }

    class OneAction
    {
        public Action startAction;
        public IEnumerator enumerator;
        public OneAction(Action startAction, Func<bool> IsCompleted)
        {
            this.startAction = startAction;
            //如果没用协程，自己创建一个协程
            enumerator = new CustomEnumerator(IsCompleted);
        }

        public OneAction(IEnumerator enumerator, Action action = null)
        {
            this.startAction = action;
            this.enumerator = enumerator;
        }

        public OneAction(Action action)
        {
            this.startAction = action;
            this.enumerator = null;
        }

        /// <summary>
        /// 自定义的协程
        /// </summary>
        class CustomEnumerator : IEnumerator
        {
            public object Current => null;
            Func<bool> IsCompleted;
            public CustomEnumerator(Func<bool> IsCompleted)
            {
                this.IsCompleted = IsCompleted;
            }
            public bool MoveNext()
            {
                return !IsCompleted();
            }

            public void Reset()
            {
            }
        }
    }
}