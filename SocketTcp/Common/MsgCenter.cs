using System;
using System.Collections.Concurrent;

namespace SocketTcp.Common
{
    public class MsgCenter : Singleton<MsgCenter>
    {
        private static ConcurrentDictionary<ushort, Action<byte[]>> _processor = new ConcurrentDictionary<ushort, Action<byte[]>>();

        public void OnMsg(ushort msg_type, byte[] data)
        {
            if (_processor.ContainsKey(msg_type))
            {
                Action<byte[]> action;
                _processor.TryGetValue(msg_type, out action);
                action(data);
            }
        }

        public void RegisterMsg(ushort msg_type, Action<byte[]> action)
        {
            _processor.TryAdd(msg_type, action);
        }

        public void UnregisterMsg(ushort msg_type)
        {
            Action<byte[]> action;
            _processor.TryRemove(msg_type, out action);
        }
    }
}
