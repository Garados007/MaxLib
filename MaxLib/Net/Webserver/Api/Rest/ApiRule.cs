namespace MaxLib.Net.Webserver.Api.Rest
{
    public abstract class ApiRule
    {
        public bool Required { get; set; } = true;

        public abstract bool Check(RestQueryArgs args);
    }
}
