namespace FeverCordSvc.Infrastructure
{
    public static class QueueExtensions
    {
        public static Queue<T> DequeueMany<T>(this Queue<T> queue, int num)
        {
            var temp = new Queue<T>();
            for (int i = 0; i < num && queue.Count != 0; i++)
            {
                temp.Enqueue(queue.Dequeue());
            }

            return temp;
        }
    }
}
