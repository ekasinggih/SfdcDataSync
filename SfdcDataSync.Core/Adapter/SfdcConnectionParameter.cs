namespace SfdcDataSync.Core.Adapter
{
    public class SfdcConnectionParameter
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }
        public string ApiPassword { get => string.Concat(Password, Token); }
        public string Endpoint { get; set; }
    }
}
