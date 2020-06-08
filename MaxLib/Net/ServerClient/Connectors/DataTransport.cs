namespace MaxLib.Net.ServerClient.Connectors
{
    public class LogoutUser
    {
        public User User { get; private set; }

        public Connector Connector { get; private set; }

        public ConnectionLostEventArgument Argument { get; private set; }

        public LogoutUser(User user, Connector connector, ConnectionLostEventArgument argument)
        {
            User = user;
            Connector = connector;
            Argument = argument;
        }
    }

}
