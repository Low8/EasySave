using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasySave.Models;

namespace EasySave.Services.Interfaces
{
    public interface ITransferCoordinator
    {
        void RegisterFile(string filePath);
        void UnregisterFile(string filePath);
        Task WaitAsync(string filePath, long fileSizeBytes, CancellationToken ct);
        void Release(string filePath, long fileSizeBytes);
    }
}

namespace EasySave.Services
{
    public class TransferCoordinator : EasySave.Services.Interfaces.ITransferCoordinator
    {
        private readonly Func<AppSettings> _getSettings;
        private readonly object _lock = new();
        private int _priorityFileCount;
        private readonly SemaphoreSlim _largeFileSemaphore = new(1, 1);
        private readonly ConcurrentQueue<TaskCompletionSource> _nonPriorityWaiters = new();
        private readonly ConcurrentDictionary<string, byte> _largeFilesInProgress = new();

        public TransferCoordinator(Func<AppSettings> getSettings)
        {
            _getSettings = getSettings;
        }

        public void RegisterFile(string filePath)
        {
            if (IsPriorityFile(filePath))
            {
                lock (_lock)
                {
                    _priorityFileCount++;
                }
            }
        }

        public void UnregisterFile(string filePath)
        {
            if (IsPriorityFile(filePath))
            {
                lock (_lock)
                {
                    _priorityFileCount--;
                    if (_priorityFileCount <= 0)
                    {
                        _priorityFileCount = 0;
                        ReleaseWaiters();
                    }
                }
            }
        }

        public async Task WaitAsync(string filePath, long fileSizeBytes, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!IsPriorityFile(filePath))
            {
                while (true)
                {
                    bool mustWait;
                    lock (_lock)
                    {
                        mustWait = _priorityFileCount > 0;
                    }

                    if (!mustWait)
                        break;

                    var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    using var reg = ct.Register(() => tcs.TrySetCanceled(ct));
                    _nonPriorityWaiters.Enqueue(tcs);
                    await tcs.Task;
                }
            }

            if (IsLargeFile(fileSizeBytes))
            {
                await _largeFileSemaphore.WaitAsync(ct);
                _largeFilesInProgress.TryAdd(filePath, 0);
            }
        }

        public void Release(string filePath, long fileSizeBytes)
        {
            if (IsLargeFile(fileSizeBytes) && _largeFilesInProgress.TryRemove(filePath, out _))
            {
                _largeFileSemaphore.Release();
            }
        }

        private void ReleaseWaiters()
        {
            while (_nonPriorityWaiters.TryDequeue(out var tcs))
            {
                if (!tcs.Task.IsCompleted)
                {
                    tcs.TrySetResult();
                }
            }
        }

        private bool IsPriorityFile(string filePath)
        {
            var settings = _getSettings();
            if (settings?.PriorityExtensions == null || settings.PriorityExtensions.Count == 0)
                return false;

            var ext = Path.GetExtension(filePath);
            if (string.IsNullOrEmpty(ext))
                return false;

            var extWithoutDot = ext.TrimStart('.');
            return settings.PriorityExtensions.Any(p =>
                p.Equals(ext, StringComparison.OrdinalIgnoreCase)
                || p.Equals(extWithoutDot, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsLargeFile(long fileSizeBytes)
        {
            var settings = _getSettings();
            if (settings == null)
                return false;

            long limitKb = settings.MaxFileSizeForParallelTransferKb;
            if (limitKb <= 0)
                return false;

            return fileSizeBytes >= limitKb * 1024;
        }
    }
}
