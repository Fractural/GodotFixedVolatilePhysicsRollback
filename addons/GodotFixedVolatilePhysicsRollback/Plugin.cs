using Fractural.Plugin;
using Fractural.Plugin.AssetsRegistry;
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
            AssetsRegistry = new EditorAssetsRegistry(this);
            AddSubPlugin(new NodesPlugin());
        }

        protected override void Unload()
        {

        }
    }
}
#endif