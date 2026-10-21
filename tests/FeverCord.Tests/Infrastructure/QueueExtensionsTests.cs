using FeverCordSvc.Infrastructure;

namespace FeverCord.Tests.Infrastructure
{
    public class QueueExtensionsTests
    {
        [Fact]
        public void DequeueMany_ReturnsUpToRequestedCount()
        {
            var queue = new Queue<int>(new[] { 1, 2, 3, 4, 5 });

            var taken = queue.DequeueMany(3);

            Assert.Equal(new[] { 1, 2, 3 }, taken);
            Assert.Equal(new[] { 4, 5 }, queue);
        }

        [Fact]
        public void DequeueMany_RequestExceedsCount_ReturnsAllRemaining()
        {
            var queue = new Queue<int>(new[] { 1, 2 });

            var taken = queue.DequeueMany(10);

            Assert.Equal(new[] { 1, 2 }, taken);
            Assert.Empty(queue);
        }

        [Fact]
        public void DequeueMany_EmptyQueue_ReturnsEmpty()
        {
            var queue = new Queue<int>();

            var taken = queue.DequeueMany(5);

            Assert.Empty(taken);
            Assert.Empty(queue);
        }

        [Fact]
        public void DequeueMany_PreservesFifoOrder()
        {
            var queue = new Queue<int>(new[] { 9, 7, 5, 3, 1 });

            var taken = queue.DequeueMany(4);

            Assert.Equal(new[] { 9, 7, 5, 3 }, taken);
        }
    }
}
