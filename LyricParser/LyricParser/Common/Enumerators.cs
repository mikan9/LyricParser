namespace LyricParser.Common
{
    public enum Category
    {
        None = -1,
        Anime = 0,
        Touhou = 1,
        Western = 2,
        JP = 3,
        Other = 4
    }

    // Enum for the supported media players
    public enum Player
    {
        Winamp = 0,
        Spotify = 1,
        Youtube = 2,
        GooglePlayMusic = 3
    }

    public enum Status
    {
        Done,
        Searching,
        Parsing,
        Failed,
        Standby,
        SaveSuccessFul,
        SaveFailed,
        Saving
    }
}
