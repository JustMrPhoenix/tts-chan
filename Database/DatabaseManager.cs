namespace TTS_Chan.Database
{
    public static class DatabaseManager
    {
        public static readonly DatabaseContext Context;
        static DatabaseManager()
        {
            Context = new DatabaseContext();
            Context.Migrate();
        }
    }
}
