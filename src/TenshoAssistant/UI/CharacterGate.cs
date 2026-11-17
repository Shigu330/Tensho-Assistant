namespace TenshoAssistant.UI;

/// <summary>
/// X-539 (17/11, user capture of the title screen: « Tensho should open its windows only once logged in with a character »): on the
/// title screen and the character selection, Tensho drew « Suivi Tensho » and its chat window already. Nothing of Tensho is drawn until a
/// character is logged in and loaded; the windows' open state is never touched, so they come back as they were after the login, and leave
/// at the logout (back to the title screen) without losing it. The /tensho command says in one line that a character is needed.
/// </summary>
internal static class CharacterGate
{
    /// <summary>Whether Tensho draws its windows and overlays this frame: only with a character logged in and loaded.</summary>
    public static bool Draws(bool loggedIn,bool playerLoaded)=>loggedIn && playerLoaded;
}
