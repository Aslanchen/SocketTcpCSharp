using SocketTcp.Model;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace SocketTcp
{
    public class OutThread
    {
        private AutoResetEvent are = new AutoResetEvent(true);
        private Boolean RUN = false;
        private Thread thread;
        private ConcurrentQueue<DataModel> queue = new ConcurrentQueue<DataModel>();

        public OutThread()
        {
            thread = new Thread(Run);
        }

        public void Start()
        {
            RUN = true;
            thread.Start();
        }

        public void Stop()
        {
            RUN = false;
            thread.Interrupt();
        }

        public void Enqueue(DataModel item)
        {
            queue.Enqueue(item);
            are.Set();
        }

        private void Run()
        {
            while (RUN)
            {
                are.WaitOne();

                DataModel item;
                queue.TryDequeue(out item);
                if (item != null)
                {
                    SocketManager.Instance.SendMessage(item);
                }
            }
        }
    }
}
