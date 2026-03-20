using JetBrains.Annotations;
using System;

[Serializable]
public class RegisterRequest
{
    public string name;
    public string email;
    public string password;
}

[Serializable]
public class RefreshRequest
{
    public string refreshToken;
}

[Serializable]
public class LoginRequest
{
    public string email;
    public string password;
}

[Serializable]
public class LoginResponse
{
    public string token;
    public string refreshToken;
}

[Serializable]
public class ExistsResponse
{
    public bool exists;
}