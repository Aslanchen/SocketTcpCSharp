using System;
using System.Collections.Generic;

namespace SocketTcp.Common
{
    public class MsgCenter : Singleton<MsgCenter>
    {
        private static Dictionary<ushort, Action<byte[]>> _processor = new Dictionary<ushort, Action<byte[]>>();

        public void OnMsg(ushort msg_type, byte[] data)
        {
            if (_processor.ContainsKey(msg_type))
            {
                _processor[msg_type](data);
            }
        }

        public void RegisterMsg(ushort msg_type, Action<byte[]> action)
        {
            _processor[msg_type] = action;
        }

        public void UnregisterMsg(ushort msg_type)
        {
            _processor.Remove(msg_type);
        }
    }
}
