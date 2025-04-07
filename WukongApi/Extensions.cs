using System;
using System.Diagnostics.CodeAnalysis;
using Photon.Client;
using ReadyM.Relay.Client;
using UnrealEngine;
using UnrealEngine.Runtime;

namespace WukongApi
{
    public static class Extensions
    {
        public static bool Equals(this float a, float b, float tolerance)
        {
            return MathF.Abs(a - b) < tolerance;
        }

        public static bool IsNullOrDestroyed([NotNullWhen(false)] this UObject? obj)
        {
            return (object?)obj == null || obj.IsDestroyed || SharedRuntimeState.IsShutdown || obj.HasAnyFlags(EObjectFlags.FinishDestroyed) || obj.IsPendingKill;
        }
        
        [Obsolete]
        public static void RegisterType(
            this RelayClient client,
            Type customType,
            byte code,
            SerializeStreamMethod serializeMethod,
            DeserializeStreamMethod deserializeMethod)
        {
            client.RegisterType(customType, code, (writer, customObject) =>
            {
                var stream = new StreamBuffer();
                serializeMethod(stream, customObject);
                writer.PutBytesWithLength(stream.GetBuffer());
            }, reader =>
            {
                var bytes = reader.GetBytesWithLength();
                var buffer = new StreamBuffer(bytes);
                return deserializeMethod(buffer, 0);
            });
        }
    }
}