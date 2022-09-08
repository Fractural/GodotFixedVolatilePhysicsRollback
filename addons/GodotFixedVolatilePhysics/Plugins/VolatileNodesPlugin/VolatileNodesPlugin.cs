using Fractural.Plugin;
using Godot;

#if TOOLS
namespace Volatile.GodotEngine.Plugin
{
    [Tool]
    public class VolatileNodesPlugin : SubPlugin
    {
        public override void Load()
        {
            GD.Print("assets registry scale " + Plugin.AssetsRegistry.Scale);
            Plugin.AddCustomType(nameof(VoltNode2D), nameof(Node2D),
                GD.Load<CSharpScript>("res://addons/GodotFixedVolatilePhysics/Core/VoltNode2D.cs"),
                Plugin.AssetsRegistry.LoadAsset<Texture>("res://addons/GodotFixedVolatilePhysics/Assets/VoltNode2D.svg"));
            Plugin.AddCustomType(nameof(VolatileRect), nameof(Node2D),
                GD.Load<CSharpScript>("res://addons/GodotFixedVolatilePhysics/Core/VolatileShapes/VolatileRect.cs"),
                Plugin.AssetsRegistry.LoadAsset<Texture>("res://addons/GodotFixedVolatilePhysics/Assets/VoltRect.svg"));
            Plugin.AddCustomType(nameof(VolatilePolygon), nameof(Node2D),
                GD.Load<CSharpScript>("res://addons/GodotFixedVolatilePhysics/Core/VolatileShapes/VolatilePolygon.cs"),
                Plugin.AssetsRegistry.LoadAsset<Texture>("res://addons/GodotFixedVolatilePhysics/Assets/VoltPolygon.svg"));
            Plugin.AddCustomType(nameof(VolatileCircle), nameof(Node2D),
                GD.Load<CSharpScript>("res://addons/GodotFixedVolatilePhysics/Core/VolatileShapes/VolatileCircle.cs"),
                Plugin.AssetsRegistry.LoadAsset<Texture>("res://addons/GodotFixedVolatilePhysics/Assets/VoltNode2D.svg"));
            Plugin.AddCustomType(nameof(VolatileRigidBody), nameof(Node2D),
                GD.Load<CSharpScript>("res://addons/GodotFixedVolatilePhysics/Core/VolatileBodies/VolatileRigidBody.cs"),
                Plugin.AssetsRegistry.LoadAsset<Texture>("res://addons/GodotFixedVolatilePhysics/Assets/VoltRigidBody.svg"));
            Plugin.AddCustomType(nameof(VolatileStaticBody), nameof(Node2D),
                GD.Load<CSharpScript>("res://addons/GodotFixedVolatilePhysics/Core/VolatileBodies/VolatileStaticBody.cs"),
                Plugin.AssetsRegistry.LoadAsset<Texture>("res://addons/GodotFixedVolatilePhysics/Assets/VoltStaticBody.svg"));
            Plugin.AddCustomType(nameof(VolatileKinematicBody), nameof(Node2D),
                GD.Load<CSharpScript>("res://addons/GodotFixedVolatilePhysics/Core/VolatileBodies/VolatileKinematicBody.cs"),
                Plugin.AssetsRegistry.LoadAsset<Texture>("res://addons/GodotFixedVolatilePhysics/Assets/VoltKinematicBody.svg"));
            Plugin.AddCustomType(nameof(VolatileArea), nameof(Node2D),
                GD.Load<CSharpScript>("res://addons/GodotFixedVolatilePhysics/Core/VolatileBodies/VolatileArea.cs"),
                Plugin.AssetsRegistry.LoadAsset<Texture>("res://addons/GodotFixedVolatilePhysics/Assets/VoltArea.svg"));
            Plugin.AddCustomType(nameof(VolatileRigidBody), nameof(Node2D),
                GD.Load<CSharpScript>("res://addons/GodotFixedVolatilePhysics/Core/VolatileBodies/VolatileRigidBody.cs"),
                Plugin.AssetsRegistry.LoadAsset<Texture>("res://addons/GodotFixedVolatilePhysics/Assets/VoltRigidBody.svg"));
            Plugin.AddCustomType(nameof(VolatileWorld), nameof(Node2D),
                GD.Load<CSharpScript>("res://addons/GodotFixedVolatilePhysics/Core/VolatileWorld.cs"),
                Plugin.AssetsRegistry.LoadAsset<Texture>("res://addons/GodotFixedVolatilePhysics/Assets/VoltWorld.svg"));
            Plugin.AddCustomType(nameof(SimpleForce), nameof(Node2D),
                GD.Load<CSharpScript>("res://addons/GodotFixedVolatilePhysics/Core/SimpleForce.cs"),
                Plugin.AssetsRegistry.LoadAsset<Texture>("res://addons/GodotFixedVolatilePhysics/Assets/SimpleForce.svg"));
        }

        public override void Unload()
        {
            Plugin.RemoveCustomType(nameof(VoltNode2D));
            Plugin.RemoveCustomType(nameof(VolatileRect));
            Plugin.RemoveCustomType(nameof(VolatilePolygon));
            Plugin.RemoveCustomType(nameof(VolatileCircle));
            Plugin.RemoveCustomType(nameof(VolatileRigidBody));
            Plugin.RemoveCustomType(nameof(VolatileStaticBody));
            Plugin.RemoveCustomType(nameof(VolatileKinematicBody));
            Plugin.RemoveCustomType(nameof(VolatileArea));
            Plugin.RemoveCustomType(nameof(VolatileWorld));
            Plugin.RemoveCustomType(nameof(SimpleForce));
        }
    }
}
#endif