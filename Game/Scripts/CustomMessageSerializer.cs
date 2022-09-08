using Godot;
using GodotRollbackNetcode;
using System;
using System.Collections.Generic;
using System.Linq;
using GDDictionary = Godot.Collections.Dictionary;

namespace Game
{
    public class CustomMessageSerializer : BaseMessageSerializer
    {
        private Dictionary<string, byte> inputPathMapping;
        private Dictionary<byte, string> inputPathMappingReverse;

        [Flags]
        public enum HeaderFlags
        {
            NONE = 0,
            HAS_INPUT_VECTOR = 1,
            DROP_BOMB = 2,
            TELEPORT = 4,
        }

        public CustomMessageSerializer()
        {
            inputPathMapping = new Dictionary<string, byte>();
            inputPathMapping["/root/Main/VolatileWorld/ServerPlayer"] = 1;
            inputPathMapping["/root/Main/VolatileWorld/ClientPlayer"] = 2;

            inputPathMappingReverse = inputPathMapping.ToDictionary((i) => i.Value, (i) => i.Key);
        }

        public override byte[] SerializeInput(GDDictionary allInput)
        {
            var buffer = new StreamPeerBuffer();
            buffer.Resize(16);

            // HACK: Currently Godot marshalls all GDScript ints into C# ints, even though they
            //       have widly different ranges. GDScript ints are 64-bit, while C# ints are
            //       32-bit.
            //       https://github.com/godotengine/godot/issues/57141
            buffer.Put32((int)allInput["$"]);
            buffer.PutU8(Convert.ToByte(allInput.Count - 1));

            var sortedKeys = allInput.Keys.OfType<string>().OrderBy((key) => key);
            foreach (string path in sortedKeys)
            {
                if (path == "$")
                    continue;
                buffer.PutU8(inputPathMapping[path]);

                HeaderFlags header = HeaderFlags.NONE;

                GDDictionary input = (GDDictionary)allInput[path];
                if (input.Contains("input_vector"))
                    header |= HeaderFlags.HAS_INPUT_VECTOR;
                if (input.Contains("drop_bomb"))
                    header |= HeaderFlags.DROP_BOMB;
                if (input.Contains("teleport"))
                    header |= HeaderFlags.TELEPORT;

                buffer.PutU8((byte)header);

                if (input.Contains("input_vector"))
                {
                    Vector2 inputVector = (Vector2)input["input_vector"];
                    buffer.PutFloat(inputVector.x);
                    buffer.PutFloat(inputVector.y);
                }
            }
            buffer.Resize(buffer.GetPosition());
            return buffer.DataArray;
        }

        public override GDDictionary UnserializeInput(byte[] serialized)
        {
            var buffer = new StreamPeerBuffer();
            buffer.PutData(serialized);
            buffer.Seek(0);

            GDDictionary allInput = new GDDictionary();

            allInput["$"] = buffer.GetU32();

            var inputCount = buffer.GetU8();
            if (inputCount == 0)
                return allInput;

            for (int i = 0; i < inputCount; i++)
            {
                string path = inputPathMappingReverse[buffer.GetU8()];
                GDDictionary input = new GDDictionary();
                HeaderFlags header = (HeaderFlags)buffer.GetU8();

                if (header.HasFlag(HeaderFlags.HAS_INPUT_VECTOR))
                    input["input_vector"] = new Vector2(buffer.GetFloat(), buffer.GetFloat());
                if (header.HasFlag(HeaderFlags.DROP_BOMB))
                    input["drop_bomb"] = true;
                if (header.HasFlag(HeaderFlags.TELEPORT))
                    input["teleport"] = true;

                allInput[path] = input;
            }

            return allInput;
        }
    }
}