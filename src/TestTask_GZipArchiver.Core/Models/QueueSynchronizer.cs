using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace TestTask_GZipArchiver.Core.Models
{
    // Forses threads do their job (or part of it) one by one strictly.
    public class QueueSynchronizer : IDisposable
    {
        private ManualResetEvent _lock;
        private int _awaitedPos;
        
        public QueueSynchronizer()
        {
            _lock = new ManualResetEvent(false);
            _awaitedPos = 0;
        }

        ~QueueSynchronizer()
        {
            _lock.Dispose();
        }

        public void GetInQueue(int pos)
        {
            //Console.WriteLine($"Block {pos} has got in queue.");

            while (pos != _awaitedPos)
            {
                _lock.WaitOne();
            }
        }

        public void LeaveQueue(int pos)
        {
            //Console.WriteLine($"Block {pos} is trying to leave the queue.");
            
            //Console.WriteLine($"Block {pos} has left queue.");
            
            _awaitedPos++;

            _lock.Set();

            //Console.WriteLine($"Block {_awaitedPos} has been passed.");
        }

        public void Reset()
        {
            _awaitedPos = 0;
            _lock.Reset();
        }

        public void Dispose()
        {
            _lock.Dispose();
        }
    }
}
