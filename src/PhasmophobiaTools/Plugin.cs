using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

namespace PhasmophobiaTools;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BasePlugin
{
    public const string PluginGuid = "com.badwolf.phasmophobiatools";
    public const string PluginName = "Phasmophobia Tools";
    public const string PluginVersion = "0.1.0";

    internal static ManualLogSource Logger { get; private set; } = null!;

    public override void Load()
    {
        Logger = Log;
        Logger.LogInfo($"{PluginName} v{PluginVersion} loaded.");
    }
}
