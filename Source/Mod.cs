using ICities;
using CitiesHarmony.API;

namespace BetterBusStopPosition
{

public class Mod : IUserMod
{
    public string Name { get { return "Better Bus Stop Position"; } }
    public string Description { get { return "Make more buses fit a bus stop"; } }

    public void OnEnabled()
    {
        HarmonyHelper.DoOnHarmonyReady( () => Patcher.PatchAll());
    }

    public void On()
    {
        if( HarmonyHelper.IsHarmonyInstalled )
            Patcher.UnpatchAll();
    }
}

}
