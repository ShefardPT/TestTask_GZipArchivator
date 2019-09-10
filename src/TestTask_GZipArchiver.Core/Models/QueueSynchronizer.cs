using System;
using System.Threading;

namespace TestTask_GZipArchiver.Core.Models
{
    // Forses threads do their job (or part of it) one by one strictly.
    public class QueueSynchronizer
    {
        private AutoResetEvent _lock;
        private int _awaitedPos;

        public QueueSynchronizer()
        {
            _lock = new AutoResetEvent(true);
            _awaitedPos = 0;
        }

        public void GetInQueue(int pos)
        {
            Console.WriteLine($"Block {pos} has got in queue.");

            while (pos != _awaitedPos)
            {
                Console.WriteLine($"Block {pos} has tried to move further.");
                
                _lock.WaitOne();
            }

            _lock.Reset();

            Console.WriteLine($"Block {pos} has moved further.");
        }

        public void LeaveQueue()
        {
            Console.WriteLine($"Block {_awaitedPos} has left queue.");

            _awaitedPos++;

            _lock.Set();
        }
    }
}
