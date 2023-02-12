using Godot.Collections;
using GodotRollbackNetcode;

namespace Volatile.GodotEngine.Rollback
{
    public static class VoltNetworkType
    {
        public static void AddVoltSerialized<T>(this Dictionary data, object key, T value)
        {
            data[key] = VoltType.Serialize(value);
        }

        public static T GetVoltDeserialized<T>(this Dictionary data, object key)
        {
            return VoltType.Deserialize<T>(data.Get<byte[]>(key));
        }

        public static T GetVoltDeserializedOrDefault<T>(this Dictionary data, object key)
        {
            return VoltType.DeserializeOrDefault<T>(data.Get<byte[]>(key, null));
        }
    }
}