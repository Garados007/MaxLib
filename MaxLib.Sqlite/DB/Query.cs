using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace MaxLib.DB
{
    public class Query : IDisposable
    {
        public SQLiteCommand Command { get; set; }

        public List<SQLiteParameter> Parameter { get; set; }

        public Database Database { get; private set; }

        public Query(Database database, string sql)
        {
            Database = database ?? throw new ArgumentNullException("database");
            Command = new SQLiteCommand(database.Connection);
            Command.CommandText = sql ?? throw new ArgumentNullException("sql");
            var count = sql.Count((c) => c == '?');
            Parameter = new List<SQLiteParameter>(count);
            for (int i = 0; i < count; ++i)
                Parameter.Add(new SQLiteParameter());
            Command.Parameters.AddRange(Parameter.ToArray());
        }

        public object this[int index]
        {
            get { return Parameter[index].Value; }
            set
            {
                if (index < 0 || index >= Parameter.Count) return;
                Parameter[index].Value = value;
            }
        }

        public void SetValues(params object[] values)
        {
            for (int i = 0; i < values.Length; ++i)
                this[i] = values[i];
        }

        public int ExecuteNonQuery(bool doCommit = true)
        {
            int result = 0;
            Database.Execute(() => result = Command.ExecuteNonQuery(), doCommit);
            return result;
            //return Command.ExecuteNonQuery();
        }

        public SQLiteDataReader ExecuteReader(bool doCommit = true)
        {
            SQLiteDataReader reader = null;
            Database.Execute(() => reader = Command.ExecuteReader(), doCommit);
            return reader;
            //return Command.ExecuteReader();
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

                Command.Dispose();
                Command = null;
                Parameter.Clear();
                disposedValue = true;
            }
        }

        // TODO: Finalizer nur überschreiben, wenn Dispose(bool disposing) weiter oben Code für die Freigabe nicht verwalteter Ressourcen enthält.
        // ~Query() {
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
