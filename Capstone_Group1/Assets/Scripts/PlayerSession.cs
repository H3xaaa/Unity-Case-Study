public static class PlayerSession
{
    public static string PlayerName { get; private set; } = "";
    public static string RoomCode { get; private set; } = "";
    public static string PlayerUID { get; private set; } = "";
    public static bool IsHost { get; private set; } = false;

    public static void SetPlayerName(string name)
    {
        PlayerName = name.Trim();
    }

    public static void SetRoom(string code, bool isHost)
    {
        RoomCode = code;
        IsHost = isHost;
    }

    public static void SetUID(string uid)
    {
        PlayerUID = uid;
    }

    public static void Clear()
    {
        PlayerName = "";
        RoomCode = "";
        PlayerUID = "";
        IsHost = false;
    }
}