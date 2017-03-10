using SocketTcp.Model;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace SocketTcp
{
    public class InThread
    {
        private AutoResetEvent are = new AutoResetEvent(true);
        private Boolean RUN = false;
        private Thread thread;
        private ConcurrentQueue<DataModel> queue = new ConcurrentQueue<DataModel>();

        public InThread()
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
            are.Dispose();
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
                DataModel item;
                queue.TryDequeue(out item);

                if (item != null)
                {
                    SocketManager.Instance.OnMsg(item);
                }
                else
                {
                    try
                    {
                        are.WaitOne();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
    }
}
