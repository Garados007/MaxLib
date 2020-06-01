using MaxLib.DB;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MaxLib.Sqlite.DB
{
    public class AsyncTransaction : IDisposable
    {
        public Database Database { get; }

        public Task TransactionReady { get; private set; }

        private Database.TransactionDisposer transaction;

        private readonly object lockQueue = new object();
        private readonly Queue<(Action, CancellationTokenSource)> jobQueue = new Queue<(Action, CancellationTokenSource)>();

        readonly CancellationTokenSource disposer = new CancellationTokenSource();
        CancellationTokenSource newJobs = new CancellationTokenSource();
        CancellationTokenSource waitForCompletion = null;

        public AsyncTransaction(Database database)
        {
            Database = database ?? throw new ArgumentNullException(nameof(database));
            _ = RunTransaction();
        }

        private async Task RunTransaction()
        {
            //load transaction token
            using (var canceler = new CancellationTokenSource())
            {
                TransactionReady = WaitUntil(canceler.Token);
                transaction = Database.Transaction();
                canceler.Cancel();
            }
            while (!disposer.IsCancellationRequested)
            {
                int count;
                lock (lockQueue)
                    count = jobQueue.Count;
                if (count == 0)
                {
                    if (newJobs != null)
                        using (var multi = CancellationTokenSource.CreateLinkedTokenSource(disposer.Token, newJobs.Token))
                        {
                            waitForCompletion?.Cancel();
                            await WaitUntil(multi.Token);
                            newJobs?.Dispose();
                            newJobs = null;
                        }
                    else newJobs = new CancellationTokenSource();
                }
                else
                {
                    Action job;
                    CancellationTokenSource canceler;
                    lock (lockQueue)
                        (job, canceler) = jobQueue.Dequeue();
                    job();
                    canceler.Cancel();
                }
            }
        }

        private async Task WaitUntil(CancellationToken token)
        {
            //return Task.Delay(-1, token).ContinueWith(task => { });
            try { await Task.Delay(-1, token); }
            catch (TaskCanceledException e)
            {
            }
            catch (AggregateException e)
            {
                e.Handle(ex => ex is TaskCanceledException);
            }
        }

        public Task Job(Action action)
        {
            _ = action ?? throw new ArgumentNullException(nameof(action));
            var canceler = new CancellationTokenSource();
            lock (lockQueue)
                jobQueue.Enqueue((action, canceler));
            if (newJobs != null && !newJobs.IsCancellationRequested)
                newJobs.Cancel();
            return WaitUntil(canceler.Token);
        }

        public async Task<T> Job<T>(Func<T> func)
        {
            _ = func ?? throw new ArgumentNullException(nameof(func));
            T result = default;
            await Job(() =>
            {
                result = func();
            });
            return result;
        }

        public async Task WaitForCompletion()
        {
            if (newJobs != null && !newJobs.IsCancellationRequested)
                return;
            await TransactionReady;
            waitForCompletion = waitForCompletion ?? new CancellationTokenSource();
            await WaitUntil(waitForCompletion.Token);
            waitForCompletion.Dispose();
            waitForCompletion = null;
        }

        public void Dispose()
        {
            disposer.Cancel();
            disposer.Dispose();
            newJobs?.Cancel();
            newJobs?.Dispose();
            transaction?.Dispose();
            lock (lockQueue)
                while (jobQueue.Count > 0)
                {
                    var canceler = jobQueue.Dequeue().Item2;
                    canceler.Cancel();
                    canceler.Dispose();
                }
            waitForCompletion?.Cancel();
            waitForCompletion?.Dispose();
        }

        public async Task DisposeAsync()
        {
            await WaitForCompletion();
            Dispose();
        }
    }
}
