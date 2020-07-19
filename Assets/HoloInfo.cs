using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

public class HoloInfo : MonoBehaviour
{
    private void Awake()
    {
        Application.logMessageReceivedThreaded += LogMessageReceived;
        //Application.logMessageReceived += LogMessageReceived;
    }

    private void Start()
    {

    }

    private void Update()
    {
        LogUpdate();
    }

    #region log

    public TMP_Text LogText;
    public string LogString { get; protected set; }
    public int MaxNumMessages;
    protected List<string> Logs = new List<string>();
    string condition;
    bool logDirty = false;

    public string LogLastNString(int n = 20)
    {
        StringBuilder sb = new StringBuilder(2000);
        int start = Math.Max(Logs.Count - n, 0);
        for (; start < Logs.Count; ++start)
        {
            sb.AppendLine(Logs[start]);
        }
        return sb.ToString(); ;
    }

    void LogUpdate()
    {
        if (logDirty)
        {
            LogText.text = LogLastNString(MaxNumMessages);
            LogString += condition + "\n";
            logDirty = false;
        }
    }

    protected void LogMessageReceived(string condition, string stackTrace, LogType type)
    {
        Logs.Add(condition);
        logDirty = true;
        this.condition = condition;
    }

    #endregion
}
