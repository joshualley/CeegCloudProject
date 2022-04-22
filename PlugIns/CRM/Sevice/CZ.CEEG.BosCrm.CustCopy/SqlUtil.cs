using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;

namespace CZ.Utils
{
    public class SqlUtil
    {
        private static readonly string connStr = "Data Source=10.4.200.187;Initial Catalog=AIS202104NTB;User ID=sa;Password=czkingdee";

        /// <summary>
        /// 执行增、删、改的方法
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="pms"></param>
        /// <returns></returns>
        public static int ExecuteNonQuery(string sql, params SqlParameter[] pms)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    if (pms != null)
                    {
                        cmd.Parameters.AddRange(pms);
                    }
                    con.Open();
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// 执行返回DataTable的方法
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="pms"></param>
        /// <returns></returns>
        public static DataTable ExecuteDataTable(string sql, params SqlParameter[] pms)
        {
            DataTable dt = new DataTable();
            using (SqlDataAdapter adapter = new SqlDataAdapter(sql, connStr))
            {
                if (pms != null)
                {
                    adapter.SelectCommand.Parameters.AddRange(pms);
                }
                adapter.Fill(dt);
            }
            return dt;
        }

        /// <summary>
        /// 执行查询并返回一个实体的方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static IEnumerable<T> ExecuteEntity<T>(string sql)
        {
            DataTable dt = ExecuteDataTable(sql, null);
            Type type = typeof(T);
            IEnumerable<PropertyInfo> properties = type.GetProperties();
            List<T> entities = new List<T>();
            foreach (DataRow row in dt.Rows)
            {
                T entity = Activator.CreateInstance<T>();
                foreach (var p in properties)
                {
                    Type pType = p.PropertyType;
                    if (pType == typeof(string))
                    {
                        p.SetValue(entity, row[p.Name].ToString(), null);
                    }
                    else if (pType == typeof(bool))
                    {
                        p.SetValue(entity, Convert.ToBoolean(row[p.Name]), null);
                    }
                    else if (pType == typeof(int))
                    {
                        p.SetValue(entity, Convert.ToInt32(row[p.Name]), null);
                    }
                    else if (pType == typeof(long))
                    {
                        p.SetValue(entity, Convert.ToInt64(row[p.Name]), null);
                    }
                    else if (pType == typeof(float))
                    {
                        p.SetValue(entity, float.Parse(row[p.Name].ToString()), null);
                    }
                    else if (pType == typeof(double))
                    {
                        p.SetValue(entity, Convert.ToDouble(row[p.Name]), null);
                    }
                    else if (pType == typeof(decimal))
                    {
                        p.SetValue(entity, Convert.ToDecimal(row[p.Name]), null);
                    }
                    else if (pType == typeof(DateTime))
                    {
                        p.SetValue(entity, Convert.ToDateTime(row[p.Name]), null);
                    }
                    // 其他类型不进行处理
                }
                entities.Add(entity);
            }
            return entities;
        }
    }
}
