namespace EasySave.Services.Priority;

/// <summary>
/// Manages parallel file transfers with priority and size-based constraints.
/// </summary>
public class ParallelTransferManager
{
    private readonly Queue<FileTransferTask> _taskQueue = new();
    private readonly HashSet<FileTransferTask> _activeTransfers = new();
    private readonly object _lock = new();
    private readonly long _maxParallelFileSize; // in bytes
    private readonly int _maxParallelTransfers;

    public event EventHandler<FileTransferTask>? TransferStarted;
    public event EventHandler<FileTransferTask>? TransferCompleted;
    public event EventHandler<(FileTransferTask task, Exception error)>? TransferFailed;

    public ParallelTransferManager(long maxParallelFileSizeKB = 1024, int maxParallelTransfers = 3)
    {
        _maxParallelFileSize = maxParallelFileSizeKB * 1024;
        _maxParallelTransfers = maxParallelTransfers;
    }

    /// <summary>
    /// Enqueues a file transfer task.
    /// Priority files are inserted at the beginning of the queue.
    /// </summary>
    public void EnqueueTask(FileTransferTask task)
    {
        lock (_lock)
        {
            if (task.IsPriority)
            {
                // Insert priority tasks at the beginning (after active priority transfers)
                var nonPriorityTasks = new Queue<FileTransferTask>();
                while (_taskQueue.Count > 0 && _taskQueue.Peek().IsPriority)
                {
                    nonPriorityTasks.Enqueue(_taskQueue.Dequeue());
                }

                _taskQueue.Enqueue(task);

                while (nonPriorityTasks.Count > 0)
                {
                    _taskQueue.Enqueue(nonPriorityTasks.Dequeue());
                }
            }
            else
            {
                _taskQueue.Enqueue(task);
            }
        }
    }

    /// <summary>
    /// Dequeues the next available task based on constraints.
    /// Returns null if no task can be started due to constraints.
    /// </summary>
    public FileTransferTask? DequeueNextTask()
    {
        lock (_lock)
        {
            // Check if there are pending priority tasks
            bool hasPendingPriorityTasks = _taskQueue.Any(t => t.IsPriority);

            // If there are pending priority tasks and all active transfers are non-priority, don't proceed
            if (hasPendingPriorityTasks)
            {
                var activePriorityCount = _activeTransfers.Count(t => t.IsPriority);
                var activeNonPriorityCount = _activeTransfers.Count(t => !t.IsPriority);

                if (activeNonPriorityCount > 0 && activePriorityCount == 0)
                {
                    // Stop non-priority transfers if priority is pending
                    return null;
                }
            }

            // Check transfer limits
            if (_activeTransfers.Count >= _maxParallelTransfers)
                return null;

            // Check file size constraint: cannot start two large files in parallel
            long totalLargeFileSize = _activeTransfers
                .Where(t => t.FileSize > _maxParallelFileSize)
                .Sum(t => t.FileSize);

            if (totalLargeFileSize > 0)
            {
                // Already transferring a large file, can only add small files
                var nextTask = _taskQueue.FirstOrDefault(t => t.FileSize <= _maxParallelFileSize);
                if (nextTask != null)
                {
                    var newQueue = new Queue<FileTransferTask>(_taskQueue.Where(t => t != nextTask));
                    _taskQueue.Clear();
                    foreach (var item in newQueue)
                    {
                        _taskQueue.Enqueue(item);
                    }
                    _activeTransfers.Add(nextTask);
                    return nextTask;
                }
                return null;
            }

            // Standard case: dequeue next task
            if (_taskQueue.Count > 0)
            {
                var task = _taskQueue.Dequeue();
                _activeTransfers.Add(task);
                return task;
            }

            return null;
        }
    }

    /// <summary>
    /// Marks a task as completed.
    /// </summary>
    public void CompleteTask(FileTransferTask task)
    {
        lock (_lock)
        {
            _activeTransfers.Remove(task);
            TransferCompleted?.Invoke(this, task);
        }
    }

    /// <summary>
    /// Marks a task as failed.
    /// </summary>
    public void FailTask(FileTransferTask task, Exception error)
    {
        lock (_lock)
        {
            _activeTransfers.Remove(task);
            TransferFailed?.Invoke(this, (task, error));
        }
    }

    /// <summary>
    /// Gets the current queue state.
    /// </summary>
    public (int pendingTasks, int activeTasks, int pendingPriorityTasks) GetQueueState()
    {
        lock (_lock)
        {
            return (
                _taskQueue.Count,
                _activeTransfers.Count,
                _taskQueue.Count(t => t.IsPriority)
            );
        }
    }

    /// <summary>
    /// Clears all queued and active tasks.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _taskQueue.Clear();
            _activeTransfers.Clear();
        }
    }
}
