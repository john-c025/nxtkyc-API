// Handlers/PostgresTypeHandlers.cs
using System;
using System.Data;
using Dapper;
using Npgsql;
using NpgsqlTypes;

namespace CoreHRAPI.Handlers
{
    public static class PostgresTypeHandlers
    {
        public class JsonTypeHandler : SqlMapper.TypeHandler<object>
        {
            public override object Parse(object value)
            {
                return value is DBNull ? null : value;
            }

            public override void SetValue(IDbDataParameter parameter, object value)
            {
                parameter.Value = value ?? DBNull.Value;
            }
        }

        public class DateTimeHandler : SqlMapper.TypeHandler<DateTime>
        {
            public override DateTime Parse(object value)
            {
                return value is DBNull ? DateTime.MinValue : (DateTime)value;
            }

            public override void SetValue(IDbDataParameter parameter, DateTime value)
            {
                parameter.Value = value == DateTime.MinValue ? DBNull.Value : value;
            }
        }

        public class ArrayTypeHandler<T> : SqlMapper.TypeHandler<T[]>
        {
            public override T[] Parse(object value)
            {
                if (value is T[] array)
                {
                    return array;
                }
                return value is DBNull ? Array.Empty<T>() : throw new ArgumentException($"Cannot convert {value.GetType()} to {typeof(T[])}");
            }

            public override void SetValue(IDbDataParameter parameter, T[] value)
            {
                if (parameter is NpgsqlParameter npgsqlParameter)
                {
                    npgsqlParameter.Value = value == null || value.Length == 0 ? DBNull.Value : value;
                    npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Array | GetNpgsqlDbType(typeof(T));
                }
            }

            private NpgsqlDbType GetNpgsqlDbType(Type type)
            {
                if (type == typeof(int)) return NpgsqlDbType.Integer;
                if (type == typeof(string)) return NpgsqlDbType.Text;
                throw new ArgumentException($"Unsupported type: {type}");
            }
        }
    }
}