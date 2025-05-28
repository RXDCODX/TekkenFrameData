namespace TekkenFrameData.Library.Models.Steam;

public class OnLobbyLinkAvailableEventArgs(ulong userSteamId, string lobbyLink) : EventArgs
{
    public ulong UserSteamId { get; init; } = userSteamId;
    public string LobbyLink { get; init; } = lobbyLink;
}
