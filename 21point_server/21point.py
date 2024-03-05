from flask import Flask, render_template,request
from flask_sockets import Sockets
import json
import copy
from card import *
import random
import gevent

app = Flask(__name__)
sockets = Sockets(app)
rooms = [0]
users = [0]
client_pool = []

def makeJson(code,action="",message="",submessage="",submessage2=""):
    return '{'+"\"code\":"+str(code)+",\"action\":\"%s\""%(action)+",\"message\":\"%s\""%(message)+",\"submessage\":\"%s\""%(submessage)+",\"submessage2\":\"%s\""%(submessage2)+"}"

def getKey(ws):
    return str(ws).split(' ')[-1].rstrip('>')

        
class User(object):
    status = 0  # 0是不在房间中
    hand = []
    field = []
    jokerfield = []
    deck = []
    chip = 10
    def __init__(self,name,userid):
        self.name = name
        self.userid = userid

class Room(object):
    player1=0
    player2=0
    status="Waitting"
    actions=[]
    cardcnt=0
    endARound=0
    roundcnt=0
    jokerchain=[]
    def __init__(self,roomid):
        self.roomid = roomid

class Client(object):
    userid = 0
    roomid = 0
    def __init__(self,ws):
        self.ws = ws
        
def getUser(id):
    global users
    for i in users[1:]:
        if i.userid == id:
            return i
    return None

def getRoom(id):
    global rooms
    for i in rooms[1:]:
        if i.roomid == id:
            return i
    return None

def getRoomByUser(id):
    global client_pool
    for i in client_pool:
        if i.userid == id:
            return getRoom(i.roomid)
    return None
def getWs(id):
    global client_pool
    for i in client_pool:
        if i.userid == id:
            return i.ws
    return None

def roomSend(room,mes):
    getWs(room.player1).send(mes)
    getWs(room.player2).send(mes)

def GameInit(room):
    getUser(room.player1).deck =copy.copy(defaultdeck)
    getUser(room.player2).deck =copy.copy(defaultdeck)

def getEnemy(room,userid):
    if room.player1 == userid and room.player2 !=0:
        return getUser(room.player2)
    elif room.player2 == userid and room.player1 !=0:
        return getUser(room.player1)
    else:
        return None

def getCard(userid,isHide):
    user = getUser(userid)
    room = getRoomByUser(userid)
    enemyUser = getEnemy(room,userid)
    room.cardcnt+=1
    if len(user.deck) == 0:
        user.deck = copy.copy(defaultdeck)
    cardIndex = random.randint(0,len(user.deck)-1)
    card = Card(kind=user.deck[cardIndex], owner=userid, cardid=room.cardcnt)
    user.deck.pop(cardIndex)
    card.isHide = isHide
    user.field.append(card)
    if isHide == 1:
        roomSend(room,makeJson(200,action="发牌",message=str(0),submessage=str(userid),submessage2=str(card.cardid)))
    else:
        roomSend(room,makeJson(200,action="发牌",message=str(card.kind),submessage=str(userid),submessage2=str(card.cardid)))
    
def startARound(room):
    getUser(room.player1).field =[]
    getUser(room.player2).field =[]
    getCard(room.player1,1)
    gevent.sleep(0.5)
    getCard(room.player2,1)
    gevent.sleep(0.5)
    getCard(room.player1,0)
    gevent.sleep(0.5)
    getCard(room.player2,0)
    gevent.sleep(0.5)
    getJoker(room.player1)
    getJoker(room.player2)
    room.roundcnt +=1
    if room.roundcnt % 2 ==1:
        getWs(room.player1).send(makeJson(200,action="你的回合"))
    else:
        getWs(room.player2).send(makeJson(200,action="你的回合"))

def getRule(room):
    rule = 21
    for i in range(0,len(room.jokerchain)):
        joker = room.jokerchain[i]
        for j in range(0,len(joker.effect)):
            if  joker.effect[j]=="rule":
                rule = joker.count[j]
    return rule

def getPunish(room,userid):
    cnt = 1
    for i in range(0,len(room.jokerchain)):
        joker = room.jokerchain[i]
        for j in range(0,len(joker.effect)):
            if joker.effect[j]=="chip":
                if joker.target[j] == userid:
                    cnt += joker.count[j]
    if cnt<0:
        cnt =0
    return cnt





def endRound(room):
    player1User = getUser(room.player1)
    player2User = getUser(room.player2)

    getWs(room.player1).send(makeJson(200,action="回合结束翻面",message=str(player1User.field[0].kind),submessage=str(player2User.field[0].kind)))
    getWs(room.player2).send(makeJson(200,action="回合结束翻面",message=str(player2User.field[0].kind),submessage=str(player1User.field[0].kind)))
    gevent.sleep(0.5)
    player1cnt = 0
    player2cnt = 0
    room.endARound=0
    for i in player1User.field:
        player1cnt += i.kind
    for i in player2User.field:
        player2cnt += i.kind
    rule = getRule(room)
    if player1cnt> rule and player2cnt<= rule:
        player1User.chip -= getPunish(room,room.player1)
    elif player1cnt<= rule and player2cnt> rule:
        player2User.chip -= getPunish(room,room.player2)
    elif player1cnt <=rule and player2cnt<=rule:
        if player1cnt < player2cnt:
            player1User.chip -= getPunish(room,room.player1)
        elif player1cnt > player2cnt:
            player2User.chip -= getPunish(room,room.player2)
    getWs(room.player1).send(makeJson(200,action="失败惩罚",message=str(player1User.chip),submessage=str(player2User.chip)))
    getWs(room.player2).send(makeJson(200,action="失败惩罚",message=str(player2User.chip),submessage=str(player1User.chip)))
    player1User.jokerfield.clear()
    player2User.jokerfield.clear()
    room.jokerchain.clear()
    gevent.sleep(0.5)
    roomSend(room,makeJson(200,action="更新规则",message=str(getRule(room))))
    getWs(player1User.userid).send(makeJson(200,action="更新惩罚",message=str(getPunish(room,player1User.userid)),submessage=str(getPunish(room,player2User.userid))))
    getWs(player2User.userid).send(makeJson(200,action="更新惩罚",message=str(getPunish(room,player2User.userid)),submessage=str(getPunish(room,player1User.userid))))
    startARound(room)
flagg=1
def getJoker(userid):
    user = getUser(userid)
    room = getRoomByUser(userid)
    room.cardcnt += 1
    enemyUser = getEnemy(room,userid)
    cnt = 0
    for k,v in jokers.items():
        cnt += v
    cnt = random.randint(1,cnt)
    joker =""
    for k,v in jokers.items():
        cnt-= v
        if cnt <=0:
            joker =k
            break
    user.hand.append(Card(kind = joker,owner = userid,cardid = room.cardcnt))
    getWs(user.userid).send(makeJson(200,action="获得王牌",message=joker,submessage=str(room.cardcnt)))
    getWs(enemyUser.userid).send(makeJson(200,action="获得王牌",message="unknown",submessage=str(room.cardcnt)))
    gevent.sleep(0.8)

def findCard(userid,cardid):
    user = getUser(userid)
    for i in user.field:
        if i.cardid == cardid:
            return i
    return None
def findJokerFromHand(userid,cardid):
    user = getUser(userid)
    for i in user.hand:
        if i.cardid == cardid:
            return i
    return None

def findJokerFromChain(roomid,cardid):
    room = getRoom(roomid)
    for i in room.jokerchain:
        if i.cardid == cardid:
            return i
    return None

def handleInFieldCardEffect(card,userid):
    if card.kind == "enemyChipPlusTwo":
        card.effect=["chip"]
        card.count =[2]
        card.target = [getEnemy(getRoomByUser(userid),userid).userid]
    elif card.kind == "selfChipSubOne":
        card.effect=["chip"]
        card.count =[-1]
        card.target = [userid]
    elif card.kind == "bothChipSubTwo":
        card.effect=["chip","chip"]
        card.count = [-2,-2]
        card.target = [getEnemy(getRoomByUser(userid),userid).userid,userid]
    elif card.kind == "changeRuleTo17":
        card.effect = ["rule"]
        card.count =[17]
    elif card.kind == "changeRuleTo24":
        card.effect = ["rule"]
        card.count =[24]
    elif card.kind == "changeRuleTo27":
        card.effect = ["rule"]
        card.count =[27]
    elif card.kind =="enemyChipPlusEnemySpCardDivTwo":
        card.effect = ["chip"]
        card.count = [len(getEnemy(getRoomByUser(userid),userid).hand)//2]
        card.target=[getEnemy(getRoomByUser(userid),userid).userid]
    elif card.kind =="getTwoSpCardAndChipPlusTwo":
        card.effect =["chip"]
        card.count = [2]
        card.target= [userid]

def handleOutFieldCardEffect(joker,userid):
    user = getUser(userid)
    room = getRoomByUser(userid)
    enemyUser = getEnemy(room,userid)
    if joker.kind == "selfHandGet1":
        for i in range(0,len(user.deck)):
            if user.deck[i] == 1:
                room.cardcnt +=1
                card = Card(kind=user.deck[i], owner=userid, cardid=room.cardcnt)
                user.deck.pop(i)
                card.isHide = 0
                user.field.append(card)
                roomSend(room,makeJson(200,action="发牌",message=str(card.kind),submessage=str(userid),submessage2=str(card.cardid)))
                break
    elif joker.kind == "selfHandGet3":
        for i in range(0,len(user.deck)):
            if user.deck[i] == 3:
                room.cardcnt +=1
                card = Card(kind=user.deck[i], owner=userid, cardid=room.cardcnt)
                user.deck.pop(i)
                card.isHide = 0
                user.field.append(card)
                roomSend(room,makeJson(200,action="发牌",message=str(card.kind),submessage=str(userid),submessage2=str(card.cardid)))
                break
    elif joker.kind == "selfHandGet5":
        for i in range(0,len(user.deck)):
            if user.deck[i] == 5:
                room.cardcnt +=1
                card = Card(kind=user.deck[i], owner=userid, cardid=room.cardcnt)
                user.deck.pop(i)
                card.isHide = 0
                user.field.append(card)
                roomSend(room,makeJson(200,action="发牌",message=str(card.kind),submessage=str(userid),submessage2=str(card.cardid)))
                break
    elif joker.kind =="selfHandReturn1":
        if len(user.field) <=1:
            return
        card = user.field[-1]
        user.deck.append(card.kind)
        user.field.remove(card)
        roomSend(room,makeJson(200,action="返回牌库",message=str(card.cardid),submessage=str(userid)))
        del card
    elif joker.kind =="enemyHandReturn1":
        if len(enemyUser.field) <=1:
            return
        card = enemyUser.field[-1]
        enemyUser.deck.append(card.kind)
        enemyUser.field.remove(card)
        roomSend(room,makeJson(200,action="返回牌库",message=str(card.cardid),submessage=str(enemyUser.userid)))
        del card
    elif joker.kind =="change1HandWithEnemy":
        if len(user.field) <= 1 or len(enemyUser.field) <=1:
            return
        card1 = user.field[-1]
        card2= enemyUser.field[-1]
        enemyUser.field.remove(card2)
        user.field.remove(card1)
        user.field.append(card2)
        enemyUser.field.append(card1)
        getWs(user.userid).send(makeJson(200,action="交换数字牌",message=str(card1.cardid),submessage=str(card2.cardid)))
        getWs(enemyUser.userid).send(makeJson(200,action="交换数字牌",message=str(card2.cardid),submessage=str(card1.cardid)))
    elif joker.kind == "breakEnemyLastSpCard":
        if len(enemyUser.jokerfield) <1:
            return
        card = enemyUser.jokerfield[-1]
        enemyUser.jokerfield.remove(card)
        room.jokerchain.remove(card)
        roomSend(room,makeJson(200,action="破坏王牌",message=str(card.cardid),submessage=str(enemyUser.userid)))
        gevent.sleep(0.2)
        roomSend(room,makeJson(200,action="更新规则",message=str(getRule(room))))
        getWs(user.userid).send(makeJson(200,action="更新惩罚",message=str(getPunish(room,user.userid)),submessage=str(getPunish(room,enemyUser.userid))))
        getWs(enemyUser.userid).send(makeJson(200,action="更新惩罚",message=str(getPunish(room,enemyUser.userid)),submessage=str(getPunish(room,user.userid))))
    elif joker.kind == "changeRuleTo17":
        roomSend(room,makeJson(200,action="更新规则",message=str(17)))
    elif joker.kind == "changeRuleTo24":
        roomSend(room,makeJson(200,action="更新规则",message=str(24)))
    elif joker.kind == "changeRuleTo27":
        roomSend(room,makeJson(200,action="更新规则",message=str(27)))
    elif joker.kind == "getTwoSpCardAndChipPlusTwo":
        getJoker(userid)
        gevent.sleep(0.3)
        getJoker(userid)
    gevent.sleep(0.5)

@sockets.route('/game')
def game(ws):
    client= Client(ws)
    client_pool.append(client)
    while not ws.closed:
        message = ws.receive()
        print(message)
        if  message is None:
            continue
        data = json.loads(message)
        if data['action']=="加入房间":
            roomid = int(data['message'])
            userid = int(data['id'])
            room = getRoom(roomid)
            if room.player1 == 0:
                room.player1 = userid
                client.userid = userid
                client.roomid = roomid
                ws.send(makeJson(200,action="加入房间",message="加入成功",submessage=str(roomid)))
                if room.player2 != 0:
                    user = getUser(room.player1)
                    getWs(room.player2).send(makeJson(200,action="获取房间信息",message=user.name,submessage = str(user.userid)))
            elif room.player2 == 0:
                room.player2 = userid
                client.userid = userid
                client.roomid = roomid
                ws.send(makeJson(200,action="加入房间",message="加入成功",submessage=str(roomid)))
                if room.player1 != 0:
                    user = getUser(room.player2)
                    getWs(room.player1).send(makeJson(200,action="获取房间信息",message=user.name,submessage = str(user.userid)))
            else:
                ws.send(makeJson(400,action="加入房间",message="房间已满"))
                break
        elif data['action'] == "获取房间信息":
            userid = int(data['id'])
            room = getRoomByUser(userid)
            enemyUser = getEnemy(room,userid)
            if enemyUser is not None:
                ws.send(makeJson(200,action="获取房间信息",message=enemyUser.name,submessage = str(enemyUser.userid)))
            else:
                ws.send(makeJson(200,action="获取房间信息",submessage="0"))
        elif data['action']  == "开始游戏":
            userid = int(data['id'])
            room = getRoomByUser(userid)
            enemyUser = getEnemy(room,userid)
            if enemyUser is not None and room.status == "Waitting":
                room.status = "ingame"
                roomSend(room,makeJson(200,action="开始游戏"))
                GameInit(room)
                gevent.sleep(0.5)
                startARound(room)
        elif data['action'] == "要牌":
            userid = int(data['id'])
            room = getRoomByUser(userid)
            enemyUser = getEnemy(room,userid)
            k = random.randint(1,3)
            while k==1:
                getJoker(userid)
                k = random.randint(1,3)
            getCard(userid,0)
            room.endARound = 0
            gevent.sleep(0.5)
            getWs(enemyUser.userid).send(makeJson(200,action="你的回合"))
        elif data['action'] == "不要牌":
            userid = int(data['id'])
            room = getRoomByUser(userid)
            enemyUser =getEnemy(room,userid)
            if room.endARound == 0:
                room.endARound =1
                getWs(enemyUser.userid).send(makeJson(200,action="你的回合"))
            elif room.endARound ==1:
                endRound(room)
        elif data['action']  == "使用王牌":
            userid = int(data['id'])
            user = getUser(userid)
            room = getRoomByUser(userid)
            room.endARound =0
            enemyUser = getEnemy(room,userid)
            joker = findJokerFromHand(userid,int(data["message"]))
            getWs(enemyUser.userid).send(makeJson(200,action="使用王牌",message=joker.cardid,submessage = joker.kind,submessage2 = userid))
            if joker.kind in inFieldCard:
                user.jokerfield.append(joker)
                handleInFieldCardEffect(joker,userid)
                room.jokerchain.append(joker)
                roomSend(room,makeJson(200,action="更新规则",message=str(getRule(room))))
                getWs(user.userid).send(makeJson(200,action="更新惩罚",message=str(getPunish(room,user.userid)),submessage=str(getPunish(room,enemyUser.userid))))
                getWs(enemyUser.userid).send(makeJson(200,action="更新惩罚",message=str(getPunish(room,enemyUser.userid)),submessage=str(getPunish(room,user.userid))))
            if joker.kind in outFieldCard:
                handleOutFieldCardEffect(joker,userid)
            user.hand.remove(joker)
            gevent.sleep(0.3)
            ws.send(makeJson(200,action="你的回合"))
            
            



    if client.roomid != 0:
        room = getRoom(client.roomid)
        enemyUser = getEnemy(room,client.userid)
        if enemyUser is not None:
            enemyWs = getWs(enemyUser.userid)
            enemyWs.send(makeJson(200,action="获取房间信息",submessage="0"))
        if room.player1 == client.userid:
            room.player1 = 0
        elif room.player2 == client.userid:
            room.player2 = 0
    client_pool.remove(client)
                        
# @sockets.route('/echo')
# def echo(ws):
#     client_pool.append(ws)
#     while not ws.closed:
#         message = ws.receive()
#         for i in client_pool:
#             i.send(message)
#     client_pool.remove(ws)



@app.route('/createuser')
def hello():
    name = request.args.get('name')
    global users
    for i in users[1:]:
        if i.name == name:
            return makeJson(400,message="命名重复")
    users[0] += 1
    users.append(User(name,users[0]))
    return makeJson(200,action="getUserID",message=str(users[0]))

if __name__ == "__main__":
    from gevent import pywsgi
    from geventwebsocket.handler import WebSocketHandler
    room1 = Room(1)
    rooms.append(room1)
    server = pywsgi.WSGIServer(('0.0.0.0', 9888), app, handler_class=WebSocketHandler)
    server.serve_forever()
    #exec("%s_effect(\"123\")"%("enemyChipPlusTwo"))
    
