namespace MaxLib.Net.ServerClient.Connectors
{
    public enum LoginState
    {
        Wait,
        IsConnecting,
        Connected,
        ServerFull,
        WrongID,
        NotConnectable
    }
}
