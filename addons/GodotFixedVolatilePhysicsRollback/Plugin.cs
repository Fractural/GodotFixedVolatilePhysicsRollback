using Fractural.Plugin;
using Godot;
using Godot.Attributes;

#if TOOLS
namespace Volatile.GodotEngine.Rollback.Plugin
{
    [Tool]
    public class Plugin : ExtendedPlugin
    {
        public override string PluginName => "Godot Fixed Volatile Physics";

        protected override void Load()
        {
            AddSubPlugin(new NodesPlugin());
        }

        protected override void Unload()
        {

        }
    }
}
#endif