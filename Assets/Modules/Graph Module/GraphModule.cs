﻿using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using KMHelper;

public class GraphModule : MonoBehaviour
{

    public KMAudio Audio;
    public KMBombInfo Info;
    public KMSelectable[] LEDS;
    public KMSelectable Check;
    public GameObject[] R, L, LEDG, LEDR;

    private static int _moduleIdCounter = 1;
    private int _moduleId;

    int[] WhichGraphDigit = new int[] { 6, 3, 6, 1, 1, 3, 7, 0, 4, 5 }
    // 	                          0 1 2 3 4 5 6 7 8 9
    , WhichGraphLetter = new int[] { 4, 3, 4, 6, 4, 7, 6, 0, 2, 0, 4, 2, 2, 5, 3, 0, 5, 3, 2, 6, 7, 5, 7, 1, 1, 1 };
    //							A B C D E F G H I J K L M N O P Q R S T U V W X Y Z
    Vector2[][] Neighbors = new Vector2[][]
    {
        new Vector2[]{new Vector2(1,2),new Vector2(2,3),new Vector2(1,3),new Vector2(4,6),new Vector2(5,6),new Vector2(5,7),new Vector2(4,7)}//7HPJ
		,new Vector2[]{new Vector2(1,4),new Vector2(1,2),new Vector2(1,6),new Vector2(2,3),new Vector2(2,4),new Vector2(4,7),new Vector2(6,7),new Vector2(7,8),new Vector2(6,8),new Vector2(5,6)}//34XYZ
		,new Vector2[]{new Vector2(1,2),new Vector2(1,3),new Vector2(1,6),new Vector2(2,6),new Vector2(3,6),new Vector2(3,4),new Vector2(4,6),new Vector2(5,6),new Vector2(4,5),new Vector2(4,8),new Vector2(4,7),new Vector2(5,7),new Vector2(7,8),}//SLIM
		,new Vector2[]{new Vector2(1,7),new Vector2(1,2),new Vector2(2,7),new Vector2(6,7),new Vector2(5,6),new Vector2(5,7),new Vector2(4,8),new Vector2(3,8),new Vector2(3,4)}//15BRO
		,new Vector2[]{new Vector2(1,3),new Vector2(2,4),new Vector2(3,6),new Vector2(1,2),new Vector2(3,4),new Vector2(3,7),new Vector2(4,7),new Vector2(5,7),new Vector2(7,8),new Vector2(4,5),new Vector2(3,8),new Vector2(5,8),new Vector2(1,8),new Vector2(1,6),new Vector2(2,6),new Vector2(4,6)}//8CAKE
		,new Vector2[]{new Vector2(1,2),new Vector2(2,3),new Vector2(3,4),new Vector2(4,5),new Vector2(5,6),new Vector2(6,7),new Vector2(7,8),new Vector2(1,8),new Vector2(2,7),new Vector2(3,6),new Vector2(2,6),new Vector2(3,7),new Vector2(1,4),new Vector2(5,8)}//9QVN
		,new Vector2[]{new Vector2(1,3),new Vector2(3,5),new Vector2(5,7),new Vector2(2,7),new Vector2(2,4),new Vector2(4,6),new Vector2(6,8),new Vector2(3,7),new Vector2(4,7),new Vector2(4,8),new Vector2(1,2),new Vector2(5,6)}//20DGT
		,new Vector2[]{new Vector2(2,3),new Vector2(3,8),new Vector2(3,5),new Vector2(3,6),new Vector2(4,5),new Vector2(4,7),new Vector2(4,8),new Vector2(5,7),new Vector2(5,6),new Vector2(7,8),new Vector2(2,8),new Vector2(6,7),new Vector2(1,6),new Vector2(1,7),new Vector2(1,2),new Vector2(2,7)}//6WUF
	};
    Vector2[] Queries;
    string[] graphName = { "7HPJ", "34XYZ", "SLIM", "15BRO", "8CAKE", "9QVN", "20DGT", "6WUF" };
    int[] On, digitCount;
    int graphID = -1;
    bool _isSolved = false, _lightsOn = false;
    Dictionary<Vector2, bool> dict = new Dictionary<Vector2, bool>();

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        MyInit();
        GetComponent<KMBombModule>().OnActivate += ActivateModule;
    }

    void MyInit()
    {
        On = new int[4];

        //Setup Pressing Functions
        Check.OnInteract += delegate () { OnPressCheck(); return false; };
        for (int i = 0; i < 4; i++)
        {
            int j = i;
            LEDS[i].OnInteract += delegate () { OnPressLED(j); return false; };
        }

        // Setup Keys
        Queries = new Vector2[4];
        digitCount = new int[9];

        for (int i = 1; i <= 8; i++)
            digitCount[i] = 0;
        List<Vector2> noRepeat = new List<Vector2>();
        for (int i = 1; i <= 8; i++)
            for (int j = i + 1; j <= 8; j++)
                noRepeat.Add(new Vector2(i, j));
        for (int i = 0; i < 4; i++)
        {
            // Random booleans
            On[i] = Random.Range(0, 2);
            //CHOOSE VALUES
            int pos = Random.Range(0, noRepeat.Count);
            Queries[i] = noRepeat[pos];
            noRepeat.RemoveAt(pos);
            //COUNT DIGITS FOR DECISION
            digitCount[(int)Queries[i].x]++;
            digitCount[(int)Queries[i].y]++;
            //SOMETIMES SWAP GRAPHICS
            float smol = Queries[i].x, big = Queries[i].y;
            if (Random.Range(0, 2) == 1)
            {
                float temp = smol;
                smol = big;
                big = temp;
            }
            L[i].GetComponentInChildren<TextMesh>().text = smol + "";
            R[i].GetComponentInChildren<TextMesh>().text = big + "";
            Debug.LogFormat("[Connection Check #{0}] Button {1} has number set {2},{3}", _moduleId, i, smol, big);
        }
    }

    void ActivateModule()
    {
        // ACCESS SERIAL AND FIND THE KEY
        // USE KMBombInfoExtensions.cs FOR QUERIES
        string serial = Info.GetSerialNumber();
        char dgt;
        int batteryCount = Info.GetBatteryCount();
        bool tem;

        // FIRST CONDITION ALL DIFFERENT
        tem = true;
        for (int i = 1; i <= 8; i++)
            if (digitCount[i] != 1)
            {
                tem = false;
                break;
            }
        if (tem)
        {
            dgt = serial[5];
            Debug.LogFormat("[Connection Check #{0}] All numbers are distinct, use last char of S/N: {1}", _moduleId, serial[5]);
        }
        else if (digitCount[1] > 1) //SECOND CONDITION more than one 1
        {
            dgt = serial[0];
            Debug.LogFormat("[Connection Check #{0}] More than one '1', use first char of S/N: {1}", _moduleId, serial[0]);
        }
        else if (digitCount[7] > 1) //THIRD CONDITION more than one 7
        {
            dgt = serial[5];
            Debug.LogFormat("[Connection Check #{0}] More than one '7', use last char of S/N: {1}", _moduleId, serial[5]);
        }
        else if (digitCount[2] > 2) //4th CONDITION at least three 2
        {
            dgt = serial[1];
            Debug.LogFormat("[Connection Check #{0}] At least three '2', use second char of S/N: {1}", _moduleId, serial[1]);
        }
        else if (digitCount[5] == 0) //5th CONDITION no 5
        {
            dgt = serial[4];
            Debug.LogFormat("[Connection Check #{0}] No '5', use fifth char of S/N: {1}", _moduleId, serial[4]);
        }
        else if (digitCount[8] == 2) //6th CONDITION exact two 8
        {
            dgt = serial[2];
            Debug.LogFormat("[Connection Check #{0}] Exactly two '8', use third char of S/N: {1}", _moduleId, serial[2]);
        }
        else if (batteryCount == 0 || batteryCount > 6) //Battery out of bound
        {
            dgt = serial[5];
            Debug.LogFormat("[Connection Check #{0}] Has {1} batteries, use last char of S/N: {2}", _moduleId, batteryCount, serial[5]);
        }
        else //Battery for digit choosing
        {
            dgt = serial[batteryCount - 1];
            Debug.LogFormat("[Connection Check #{0}] Otherwise, use battery amount {1} as index of S/N: {1}", _moduleId, batteryCount, serial[batteryCount - 1]);
        }

        //CONVERT TO GRAPH ID
        if ('A' <= dgt && dgt <= 'Z')
            graphID = WhichGraphLetter[dgt - 'A'];
        else
            graphID = WhichGraphDigit[dgt - '0'];

        //ADD CONNECTED PAIRS
        for (int i = 0; i < Neighbors[graphID].Length; i++)
            dict.Add(Neighbors[graphID][i], true);

        //LEDs INIT
        for (int i = 0; i < 4; i++)
            if (On[i] > 0)
                LEDG[i].SetActive(true);
            else
                LEDR[i].SetActive(true);

        Debug.LogFormat("[Connection Check #{0}] Target letter is {1}, using graph {2}", _moduleId, dgt, graphName[graphID]);
        _lightsOn = true;
    }

    void OnPressLED(int which)
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, LEDS[which].transform);
        LEDS[which].AddInteractionPunch();

        //LEDS TOGGLE
        if (!_isSolved && _lightsOn)
        {
            On[which] = 1 - On[which];
            LEDG[which].SetActive(On[which] == 1);
            LEDR[which].SetActive(On[which] != 1);
        }
    }
    void OnPressCheck()
    {
        string[] temp = { "false", "true" };

        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Check.transform);
        Check.AddInteractionPunch();

        if (!_isSolved && _lightsOn)
        {
            bool win = true;
            for (int i = 0; i < 4; i++)
            {
                bool tempo;
                bool lineExists = dict.TryGetValue(Queries[i], out tempo);
                if (lineExists != (On[i] == 1))
                {
                    win = false;
                    Debug.LogFormat("[Connection Check #{0}] Pair of {1},{2} with answer {3} is incorrect.", _moduleId, Queries[i].x, Queries[i].y, temp[On[i]]);
                }
                else Debug.LogFormat("[Connection Check #{0}] Pair of {1},{2} with answer {3} is correct.", _moduleId, Queries[i].x, Queries[i].y, temp[On[i]]);
            }
            if (win)
            {
                GetComponent<KMBombModule>().HandlePass();
                Debug.LogFormat("[Connection Check #{0}] Module solved.", _moduleId);
                _isSolved = true;
            }
            else
            {
                GetComponent<KMBombModule>().HandleStrike();
                Debug.LogFormat("[Logic #{0}] Answer is incorrect. Strike!", _moduleId);
            }
        }
    }

    KMSelectable[] ProcessTwitchCommand(string command)
    {
        var btn = new List<KMSelectable>();
        int temp = 0;

        command = command.ToLowerInvariant().Trim();

        if (Regex.IsMatch(command, @"^submit \b(true|false|t|f)\b \b(true|false|t|f)\b \b(true|false|t|f)\b \b(true|false|t|f)\b$"))
        {
            command = command.Substring(7);
            foreach (var cell in command.Trim().Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                if ((cell.Equals("t") || cell.Equals("true")) && On[temp] == 0) btn.Add(LEDS[temp]);
                if ((cell.Equals("f") || cell.Equals("false")) && On[temp] == 1) btn.Add(LEDS[temp]);
                temp++;
            }
            btn.Add(Check);
            return btn.ToArray();
        }

        else return null;
    }
}
