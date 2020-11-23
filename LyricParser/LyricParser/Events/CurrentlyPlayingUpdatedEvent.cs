using Prism.Events;
using Windows.Media.Control;

namespace LyricParser.Events
{
    public class CurrentlyPlayingUpdatedEvent : PubSubEvent<GlobalSystemMediaTransportControlsSession>
    {
    }
}
