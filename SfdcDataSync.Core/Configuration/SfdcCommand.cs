namespace SfdcDataSync.Core
{
    public class SfdcCommand
    {
        public SfdcOperation Operation { get; set; }

        public string CommandText { get; set; }

        public string Object { get; set; }

        public string UpsertKeyField { get; set; }
    }
}