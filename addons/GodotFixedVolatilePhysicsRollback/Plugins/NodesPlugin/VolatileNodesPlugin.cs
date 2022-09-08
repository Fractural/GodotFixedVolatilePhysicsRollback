﻿using Fractural.Plugin;
using Godot;

#if TOOLS
namespace Volatile.GodotEngine.Rollback.Plugin
{
    [Tool]
    public class NodesPlugin : SubPlugin
    {
        public override void Load()
        {
            Plugin.AddCustomType(nameof(NetworkVoltNode2D), nameof(Node2D),
                GD.Load<CSharpScript>("res://addons/GodotFixedVolatilePhysicsRollback/Core/NetworkVoltNode2D.cs"),
                GD.Load<Texture>("res://addons/GodotFixedVolatilePhysicsRollback/Assets/NetworkVoltNode2D.svg"));
            Plugin.AddCustomType(nameof(NetworkVolatileArea), nameof(Node2D),
                GD.Load<CSharpScript>("res://addons/GodotFixedVolatilePhysicsRollback/Core/NetworkVolatileArea.cs"),
                GD.Load<Texture>("res://addons/GodotFixedVolatilePhysicsRollback/Assets/NetworkVoltArea.svg"));
            Plugin.AddCustomType(nameof(NetworkVolatileKinematicBody), nameof(Node2D),
                GD.Load<CSharpScript>("res://addons/GodotFixedVolatilePhysicsRollback/Core/NetworkVolatileKinematicBody.cs"),
                GD.Load<Texture>("res://addons/GodotFixedVolatilePhysicsRollback/Assets/NetworkVoltKinematicBody.svg"));
            Plugin.AddCustomType(nameof(NetworkVolatileRigidBody), nameof(Node2D),
                GD.Load<CSharpScript>("res://addons/GodotFixedVolatilePhysicsRollback/Core/NetworkVolatileRigidBody.cs"),
                GD.Load<Texture>("res://addons/GodotFixedVolatilePhysicsRollback/Assets/NetworkVoltRigidBody.svg"));
        }

        public override void Unload()
        {
            Plugin.RemoveCustomType(nameof(NetworkVoltNode2D));
            Plugin.RemoveCustomType(nameof(NetworkVolatileArea));
            Plugin.RemoveCustomType(nameof(NetworkVolatileKinematicBody));
            Plugin.RemoveCustomType(nameof(NetworkVolatileRigidBody));
        }
    }
}
#endif