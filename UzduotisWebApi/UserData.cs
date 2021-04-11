using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading;

namespace UzduotisWebApi
{
    public enum DataMethod
    {
        Create,
        Append,
        Delete,
        DeleteOldData
    }

    public enum Status
    {
        Ok,
        OkGet,
        Error,
        KeyNotFound,
        ExpirationPeriodError
    }

    public class UserData
    {
        private static readonly object ModifyDataLock = new object();

        public static Dictionary<string, object[]> Keys = new Dictionary<string, object[]>();
        public static Dictionary<string, List<object>> Data = new Dictionary<string, List<object>>();

        public static int DefaultExpirationPeriod;
        public static int MaxExpirationPeriod;

        public static string ConnectionString = string.Format("data source = {0}\\Database.db", AppDomain.CurrentDomain.BaseDirectory);

        // All data modification(create, append, delete) is done inside lock, because method RemoveOldUserData runs asyncronously and can cause problems
        public static Status ModifyData(DataMethod method, string key, List<object> value, int? expirationPeriod)
        {
            lock (ModifyDataLock)
            {
                DatabaseConnection db = new DatabaseConnection(ConnectionString);

                switch (method)
                {
                    case DataMethod.DeleteOldData:
                        foreach (var element in Keys)
                        {
                            var expirationDate = (DateTime)element.Value[1];

                            if (expirationDate < DateTime.Now)
                            {
                                ModifyData(DataMethod.Delete, element.Key, null, null);
                            }
                        }
                        break;
                    case DataMethod.Delete:
                        if (Data.ContainsKey(key))
                        {
                            Data.Remove(key);
                            Keys.Remove(key);

                            SQLiteParameter[] sqlParams = new SQLiteParameter[]
                            {
                                new SQLiteParameter("@Key", key)
                            };
                            db.ExecuteQuery("DELETE FROM Data WHERE Key = @Key", sqlParams);

                            sqlParams = new SQLiteParameter[]
                            {
                                new SQLiteParameter("@Key", key)
                            };
                            db.ExecuteQuery("DELETE FROM Keys WHERE Key = @Key", sqlParams);
                        }
                        else
                        {
                            return Status.KeyNotFound;
                        }
                        break;
                    default:
                        if (Data.ContainsKey(key))
                        {
                            if (method == DataMethod.Create)
                            {
                                Data[key] = value;

                                SQLiteParameter[] sqlParams = new SQLiteParameter[]
                                {
                                    new SQLiteParameter("@Key", key)
                                };
                                db.ExecuteQuery("DELETE FROM Data WHERE Key = @Key", sqlParams);
                            }
                            else
                            {
                                foreach (var val in value)
                                {
                                    Data[key].Add(val);
                                }
                            }

                            foreach (var val in value)
                            {
                                SQLiteParameter[] sqlParams = new SQLiteParameter[]
                                {
                                    new SQLiteParameter("@Key", key),
                                    new SQLiteParameter("@Value", val)
                                };
                                db.ExecuteQuery("INSERT INTO Data(Key, Value) VALUES (@Key, @Value)", sqlParams);
                            }
                        }
                        else
                        {
                            var period = Convert.ToInt32(expirationPeriod ?? DefaultExpirationPeriod);
                            Keys.Add(key, new object[] { period, DateTime.Now.AddDays(period) });
                            Data.Add(key, value);

                            SQLiteParameter[] sqlParams = new SQLiteParameter[]
                            {
                                new SQLiteParameter("@Key", key),
                                new SQLiteParameter("@Period", period),
                                new SQLiteParameter("@Date", DateTime.Now.AddDays(period))
                            };
                            db.ExecuteQuery("INSERT INTO Keys(Key, Period, Date) VALUES (@Key, @Period, @Date)", sqlParams);

                            foreach (var val in value)
                            {
                                sqlParams = new SQLiteParameter[]
                                {
                                    new SQLiteParameter("@Key", key),
                                    new SQLiteParameter("@Value", val)
                                };
                                db.ExecuteQuery("INSERT INTO Data(Key, Value) VALUES (@Key, @Value)", sqlParams);
                            }
                        }
                        break;
                }
            }

            return Status.Ok;
        }

        // Asyncronously removes old data from ram and database
        public async static void RemoveOldUserData(int sleepMinutes)
        {
            while (true)
            {
                ModifyData(DataMethod.DeleteOldData, null, null, null);

                Thread.Sleep(sleepMinutes * 60 * 1000);
            }
        }

        //Fills UserData into ram after program is started
        public static void FillUserData()
        {
            DatabaseConnection db = new DatabaseConnection(ConnectionString);
            var result = db.ExecuteQuery("SELECT * FROM Keys", null);

            if (result != null)
            {
                while (result.Read())
                {
                    var key = (string)result[1];
                    var period = Convert.ToInt32(result[2]);
                    var date = (DateTime)result[3];

                    Keys.Add(key, new object[] { period, date });
                }
            }

            result = db.ExecuteQuery("SELECT * FROM Data", null);

            if (result != null)
            {
                while (result.Read())
                {
                    var key = (string)result[1];
                    var value = result[2];

                    if (!Data.ContainsKey(key))
                    {
                        Data.Add(key, new List<object> { value });
                    }
                    else
                    {
                        Data[key].Add(value);
                    }
                }
            }

            db.Close();
        }
    }
}
