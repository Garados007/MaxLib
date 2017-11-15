using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MaxLib.DB
{
    public class Database : IDisposable
    {
        public class TransactionDisposer : IDisposable
        {
            Database db;

            public TransactionDisposer(Database db)
            {
                this.db = db ?? throw new ArgumentNullException("db");
                db.BeginTransaction();
            }

            public void Dispose()
            {
                db.StopTransaction();
            }
        }


        SQLiteFactory factory;
        public SQLiteConnection Connection { get; private set; }
        object lockObject = new object();

        public Database(string file, string checkSql, string createSql)
        {
            if (file == null) throw new ArgumentNullException("file");
            if (checkSql == null) throw new ArgumentNullException("checkSql");
            if (createSql == null) throw new ArgumentNullException("createSql");
            var dir = new System.IO.FileInfo(file).Directory;
            if (!dir.Exists) dir.Create();
            factory = new SQLiteFactory();
            Connection = (SQLiteConnection)factory.CreateConnection();
            Connection.ConnectionString = "Data Source=" + file;
            Connection.Open();
            if (!CheckValidity(checkSql))
                CreateTables(createSql);
        }

        bool CheckValidity(string checkSql)
        {
            using (var query = Create(checkSql))
            using (var reader = query.ExecuteReader())
            {
                if (!reader.Read()) return false;
                return reader.GetInt32(0) == 0;
            }
        }

        void CreateTables(string createSql)
        {
            using (var query = Create(createSql))
                query.ExecuteNonQuery();
        }

        public Query Create(string sql)
        {
            if (disposedValue) throw new ObjectDisposedException(null);
            if (sql == null) throw new ArgumentNullException("sql");
            return new Query(this, sql);
        }

        public void Execute(Action method, bool doCommit = true)
        {
            if (disposedValue) throw new ObjectDisposedException(null);
            if (method == null) throw new ArgumentNullException("method");
            bool NeedWait = false;
            lock (lockObject)
            {
                if (BigTransaction != null && !TransactionThread.Equals(Thread.CurrentThread))
                    NeedWait = true;
            }
            if (NeedWait)
            {
                Mutex.WaitOne();
            }
            lock (lockObject)
            {
                if (NeedWait) Mutex.ReleaseMutex();
                if (BigTransaction == null)
                    using (var transaction = Connection.BeginTransaction())
                    {
                        method();
                        if (doCommit) transaction.Commit();
                    }
                else method();
            }
        }

        static Mutex Mutex = new Mutex(false);
        static SQLiteTransaction BigTransaction = null;
        static Thread TransactionThread;
        static object lockCheckThread = new object();
        static int lockDepth = 0;

        public void BeginTransaction()
        {
            if (disposedValue) throw new ObjectDisposedException(null);
            Mutex.WaitOne();
            lock (lockObject)
            {
                if (BigTransaction != null && TransactionThread != Thread.CurrentThread)
                    throw new InvalidOperationException("transaction is running");
                if (lockDepth++ > 0) return;
                BigTransaction = Connection.BeginTransaction();
                TransactionThread = Thread.CurrentThread;
            }
        }

        public void StopTransaction()
        {
            if (disposedValue) throw new ObjectDisposedException(null);
            lock (lockObject)
            {
                if (BigTransaction == null || TransactionThread != Thread.CurrentThread) return;
                if (--lockDepth > 0) return;
                BigTransaction.Commit();
                BigTransaction = null;
                TransactionThread = null;
            }
            Mutex.ReleaseMutex();
        }

        public TransactionDisposer Transaction()
        {
            return new TransactionDisposer(this);
        }
        
        #region IDisposable Support
        private bool disposedValue = false; // Dient zur Erkennung redundanter Aufrufe.

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: verwalteten Zustand (verwaltete Objekte) entsorgen.
                }

                // TODO: nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalizer weiter unten überschreiben.
                // TODO: große Felder auf Null setzen.

                factory.Dispose();
                Connection.Dispose();

                disposedValue = true;
            }
        }

        // TODO: Finalizer nur überschreiben, wenn Dispose(bool disposing) weiter oben Code für die Freigabe nicht verwalteter Ressourcen enthält.
        // ~Database() {
        //   // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in Dispose(bool disposing) weiter oben ein.
        //   Dispose(false);
        // }

        // Dieser Code wird hinzugefügt, um das Dispose-Muster richtig zu implementieren.
        public void Dispose()
        {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in Dispose(bool disposing) weiter oben ein.
            Dispose(true);
            // TODO: Auskommentierung der folgenden Zeile aufheben, wenn der Finalizer weiter oben überschrieben wird.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
