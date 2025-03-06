using System.Collections.Generic;

[System.Serializable]
public class User
{
    public string email;
    public string username;
    public string userType;

    public List<Consorcios> consorcios = new List<Consorcios>();

    public User(string username, string email, string userType)
    {
        this.username = username;
        this.email = email;
        this.userType = userType;
    }
}
