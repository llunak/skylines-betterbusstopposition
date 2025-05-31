using HarmonyLib;

namespace BetterBusStopPosition
{

public static class Patcher
{
    private const string harmonyId = "llunak.BetterBusStopPosition";
    private static bool patched = false;

    public static void PatchAll()
    {
        if( patched )
            return;
        var harmony = new Harmony( harmonyId );
        harmony.PatchAll( typeof( Patcher ).Assembly );
        patched = true;
    }

    public static void UnpatchAll()
    {
        if( !patched )
            return;
        var harmony = new Harmony( harmonyId );
        harmony.UnpatchAll( harmonyId );
        patched = false;
    }
}

}
