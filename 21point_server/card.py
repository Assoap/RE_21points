jokers = {
    'enemyChipPlusTwo': 3,
    "selfChipSubOne":3,
    "bothChipSubTwo":3,
    "selfHandGet1": 2,
    "selfHandGet3":2,
    "selfHandGet5":2,
    "selfHandReturn1":3,
    "enemyHandReturn1":3,
    "change1HandWithEnemy":3,
    "breakEnemyLastSpCard":3,
    "changeRuleTo17":2,
    "changeRuleTo24":2,
    "changeRuleTo27":2,
    "enemyChipPlusEnemySpCardDivTwo":2,
    "getTwoSpCardAndChipPlusTwo" :2
}
defaultdeck = [
    1,1,1,
    2,2,2,
    3,3,3,
    4,4,4,
    5,5,5,
    6,6,6,
    7,7,7,
    8,8,8,
    9,9,9,
    10,10,10,10,
    11,11,11
]

        # cardDescription.Add("enemyChipPlusTwo", "对手筹码加2")
        # cardDescription.Add("selfChipSubOne", "自己筹码减1")
        # cardDescription.Add("bothChipSubTwo", "双方筹码减2")
        # cardDescription.Add("selfHandGet1", "若牌库中有1，获得1")
        # cardDescription.Add("selfHandGet3", "若牌库中有3，获得3")
        # cardDescription.Add("selfHandGet5", "若牌库中有5，获得5")
        # cardDescription.Add("selfHandReturn1", "将自己最后一张牌送回牌库")
        # cardDescription.Add("enemyHandReturn1", "将对手最后一张牌送回牌库")
        # cardDescription.Add("change1HandWithEnemy", "与对手交换最后一张牌")
        # cardDescription.Add("breakEnemyLastSpCard", "破坏对手最后一张场上的王牌")
        # cardDescription.Add("changeRuleTo17", "将游戏规则改为最靠近17点的获胜")
        # cardDescription.Add("changeRuleTo24", "将游戏规则改为最靠近24点的获胜")
        # cardDescription.Add("changeRuleTo27", "将游戏规则改为最靠近27点的获胜")
        # cardDescription.Add("enemyChipPlusEnemySpCardDivTwo", "对手的筹码加上对手手中王牌数量除二")
        # cardDescription.Add("getTwoSpCardAndChipPlusOne", "自己获得两张王牌，同时自己筹码加一（被破坏后不需要归还王牌）")
class Card(object):
    isHide = 0 #0代表展示,1代表覆盖
    effect = 0
    count = 0
    target = 0
    def __init__(self,kind="1",owner="1",cardid="1"):
        self.kind = kind
        self.owner = owner
        self.cardid =cardid
inFieldCard=[
    "enemyChipPlusTwo",
    "selfChipSubOne",
    "bothChipSubTwo",
    "changeRuleTo17",
    "changeRuleTo24",
    "changeRuleTo27", 
    "enemyChipPlusEnemySpCardDivTwo", 
    "getTwoSpCardAndChipPlusTwo",
]

outFieldCard=[
    "selfHandGet1",
    "selfHandGet3", 
    "selfHandGet5",
    "selfHandReturn1",
    "enemyHandReturn1",
    "change1HandWithEnemy", 
    "breakEnemyLastSpCard", 
    "changeRuleTo17",
    "changeRuleTo24",
    "changeRuleTo27", 
    "getTwoSpCardAndChipPlusTwo",
]

