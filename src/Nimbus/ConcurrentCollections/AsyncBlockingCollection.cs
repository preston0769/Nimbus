using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Nimbus.ConcurrentCollections
{
    internal class AsyncBlockingCollection<T> : IDisposable
    {
        readonly SemaphoreSlim _itemsSemaphore = new SemaphoreSlim(0, int.MaxValue);
        readonly ConcurrentQueue<T> _items = new ConcurrentQueue<T>();

        public async Task<T> TryTake(TimeSpan timeout, CancellationToken cancellationToken)
        {
            try
            {
                await _itemsSemaphore.WaitAsync(timeout, cancellationToken);
                try
                {
                    T result;
                    _items.TryDequeue(out result);
                    return result;
                }
                finally
                {
                    _itemsSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                return default(T);
            }
        }

        public Task<T> Take(CancellationToken cancellationToken)
        {
            var timeout = TimeSpan.FromTicks(int.MaxValue); // ConcurrentQueue has a different view on TimeSpan.MaxValue than the rest of the world :(
            return TryTake(timeout, cancellationToken);
        }

        public Task Add(T item)
        {
            return Task.Run(() =>
                            {
                                _items.Enqueue(item);
                                _itemsSemaphore.Release();
                            });
        }

        public void Dispose()
        {
            _itemsSemaphore.Dispose();
        }
    }
}