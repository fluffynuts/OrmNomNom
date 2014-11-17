namespace OrmNomNom.DB
{
    public class DataConstants
    {
        public class Tables
        {
            public class Log
            {
                public const string NAME = "Log";
                public class Columns
                {
                    public const string ID = "Id";
                    public const string DATE = "Date";
                    public const string THREAD = "Thread";
                    public const string LEVEL = "Level";
                    public const string LOGGER = "Logger";
                    public const string MESSAGE = "Message";
                    public const string EXCEPTION = "Exception";
                }
            }
        }
    }
}
