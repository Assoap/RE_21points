using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class Card : MonoBehaviour
{

    public Sprite faceUp;
    public Sprite faceDown;
    // Start is called before the first frame update
    public string kind;
    public GameObject description;
    public int owner;
    public int id = 0;
    static public string[] inFieldCard =
    {
    "enemyChipPlusTwo",
    "selfChipSubOne",
    "bothChipSubTwo",
    "changeRuleTo17",
    "changeRuleTo24",
    "changeRuleTo27",
    "enemyChipPlusEnemySpCardDivTwo",
    "getTwoSpCardAndChipPlusTwo",
    };
    private void Start()
    {
        LoadFace();
        GetComponent<SpriteRenderer>().sprite = faceDown;
       
    }
    void Update()
    {
     
    }
    public void LoadFace()
    {
        if(kind != "0" && kind != "unknown")
            faceUp = Resources.Load<Sprite>("card_"+kind);
        if (!IsNumeric(kind))
            faceDown = Resources.Load<Sprite>("spback");
        else
            faceDown = Resources.Load<Sprite>("back");
    }

    public void TurnUp()
    {
        if(kind != "0" && kind != "unknown")
            GetComponent<SpriteRenderer>().sprite = faceUp;
    }

    public void TurnDown()
    {
        GetComponent<SpriteRenderer>().sprite = faceDown;
    }
    public void dis()
    {
        Destroy(gameObject);
    }

    public void OnMouseUp()
    {
        if (kind == "unknown" || IsNumeric(kind)) return;
        GameController gc = GameObject.Find("GameController").GetComponent<GameController>();
        if (gc.userID == owner && gc.canUseSpCard == true)
        {
            gc.Use(this);
            gc.canUseSpCard = false;
        }
    }
    private bool IsNumeric(string input)
    {
        Regex regex = new Regex("^[0-9]+$");
        return regex.IsMatch(input);
    }
}

