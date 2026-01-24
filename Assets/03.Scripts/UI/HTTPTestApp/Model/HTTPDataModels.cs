using System;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public class HealthCheckResponse
{
    public string status { get; set; }
    public string timestamp { get; set; }
    public string version { get; set; }

    override public string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
} 

