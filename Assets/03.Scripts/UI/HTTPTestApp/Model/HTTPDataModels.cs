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

[Serializable]
public class RegisterUserRequest
{
    public string name { get; set; }
    public string email { get; set; }
    public string password { get; set; }
    public RegisterUserRequest(string username, string password, string email)
    {
        this.name = username;
        this.password = password;
        this.email = email;
    }
    override public string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}

