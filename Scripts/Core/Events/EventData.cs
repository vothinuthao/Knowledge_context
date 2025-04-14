namespace Core
{
    /// <summary>
    /// Event data container
    /// </summary>
    public class EventData
    {
        public EventType Type { get; private set; }
        public System.EventArgs Args { get; private set; }
        
        public EventData(EventType type, System.EventArgs args)
        {
            Type = type;
            Args = args;
        }
    }
}