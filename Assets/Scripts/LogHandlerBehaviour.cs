﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogHandlerBehaviour : MonoBehaviour
{
    static string myLog = "";
    private string output;
    private string stack;

    void OnEnable()
    {
        Application.logMessageReceived += Log;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= Log;
    }

    public void Log(string logString, string stackTrace, LogType type)
    {
        output = logString;
        stack = stackTrace;
        myLog = output + "\n" + myLog;
        if (myLog.Length > 2000)
        {
            myLog = myLog.Substring(0, 1800);
        }
    }

    void OnGUI()
    {
        //if (!Application.isEditor) //Do not display in editor ( or you can use the UNITY_EDITOR macro to also disable the rest)
        //{
        GUIStyle style = new GUIStyle();
        style.fontSize = 25;
        style.normal.textColor = Color.green;
        myLog = GUI.TextArea(new Rect(10, 10, Screen.width - Screen.width/2, Screen.height - Screen.height/2), myLog, style);
        //}
    }
}