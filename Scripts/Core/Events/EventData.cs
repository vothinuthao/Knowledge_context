namespace Core
{
    /// <summary>
    /// Event data container
    /// </summary>
    public class EventData
    {
        public EventTypeInGame TypeInGame { get; private set; }
        public System.EventArgs Args { get; private set; }
        
        public EventData(EventTypeInGame typeInGame, System.EventArgs args)
        {
            TypeInGame = typeInGame;
            Args = args;
        }
    }
}