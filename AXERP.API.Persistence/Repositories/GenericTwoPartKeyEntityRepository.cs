using AXERP.API.Domain.Interfaces.Repositories;
using AXERP.API.Domain.Interfaces.UnitOfWork;
using AXERP.API.Persistence.Utils;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Text;

namespace AXERP.API.Persistence.Repositories
{
    public class GenericTwoPartKeyEntityRepository<RowType, KeyTypeA, KeyTypeB> : ITwoPartKeyRepository<RowType, KeyTypeA, KeyTypeB> where RowType : class
    {
        private readonly IConnectionProvider _connectionProvider;
        private SqlConnection _connection => _connectionProvider.Connection;
        private SqlTransaction? _sqlTransaction => _connectionProvider.Transaction;

        public GenericTwoPartKeyEntityRepository(IConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public int Add(List<RowType> entities, bool insertId = false)
        {
            if (!entities.Any())
            {
                return 0;
            }

            int result;

            var t = typeof(RowType);

            string tableName = t.GetTableName();
            string columns = t.GetColumnNamesAsSqlColumnList(null, null, !insertId);
            string properties = t.GetColumnNamesAsSqlParamList(null, !insertId);
            string query = $"INSERT INTO {tableName} ({columns}) VALUES ({properties})";

            result = _connection.Execute(query, entities, transaction: _sqlTransaction);

            return result;
        }

        public RowType Add(object entity, bool insertId = false)
        {
            RowType result;

            var t = typeof(RowType);

            string tableName = t.GetTableName();
            string columns = t.GetColumnNamesAsSqlColumnList(null, null, !insertId);
            string inserted = t.GetColumnNamesAsSqlColumnList(null, "inserted", false);
            string properties = t.GetColumnNamesAsSqlParamList(null, !insertId);
            string query = $"INSERT INTO {tableName} ({columns}) OUTPUT {inserted} VALUES ({properties})";

            result = _connection.QuerySingle<RowType>(query, entity, transaction: _sqlTransaction);

            return result;
        }

        public RowType Add(RowType entity, bool insertId = false)
        {
            RowType result;

            var t = typeof(RowType);

            string tableName = t.GetTableName();
            string columns = t.GetColumnNamesAsSqlColumnList(null, null, !insertId);
            string inserted = t.GetColumnNamesAsSqlColumnList(null, "inserted", false);
            string properties = t.GetColumnNamesAsSqlParamList(null, !insertId);
            string query = $"INSERT INTO {tableName} ({columns}) OUTPUT {inserted} VALUES ({properties})";

            result = _connection.QuerySingle<RowType>(query, entity, transaction: _sqlTransaction);

            return result;
        }

        public bool Delete(RowType entity)
        {
            int rowsEffected = 0;

            var t = typeof(RowType);

            string tableName = t.GetTableName();
            var keys = t.GetKeyColumnNames();
            string query = $"DELETE FROM {tableName} WHERE {keys[0]} = @{keys[0]} and {keys[1]} = @{keys[1]}";

            rowsEffected = _connection.Execute(query, entity, transaction: _sqlTransaction);

            return rowsEffected == 1;
        }

        public int Delete(IEnumerable<RowType> entities)
        {
            if (!entities.Any())
            {
                return 0;
            }

            int rowsEffected = 0;

            var t = typeof(RowType);

            string tableName = t.GetTableName();
            var keys = t.GetKeyColumnNames();
            string query = $"DELETE FROM {tableName} WHERE {keys[0]} = @{keys[0]} and {keys[1]} = @{keys[1]}";

            rowsEffected = _connection.Execute(query, entities, transaction: _sqlTransaction);

            return rowsEffected;
        }

        public bool Delete(KeyTypeA idA, KeyTypeB idB)
        {
            int rowsEffected = 0;

            var t = typeof(RowType);

            string tableName = t.GetTableName();
            var keys = t.GetKeyColumnNames();
            string query = $"DELETE FROM {tableName} WHERE {keys[0]} = @idA and {keys[1]} = @idB";

            rowsEffected = _connection.Execute(query, new { idA = idA, idB = idB }, transaction: _sqlTransaction);

            return rowsEffected == 1;
        }

        public int Delete(IEnumerable<(KeyTypeA, KeyTypeB)> ids)
        {
            if (!ids.Any())
            {
                return 0;
            }

            int rowsEffected = 0;

            var t = typeof(RowType);

            string tableName = t.GetTableName();
            var keys = t.GetKeyColumnNames();
            string query = $"DELETE FROM {tableName} WHERE {keys[0]} = @idA and {keys[1]} = @idB";

            rowsEffected = _connection.Execute(query, ids.Select(x => new { idA = x.Item1, idB = x.Item2 }), transaction: _sqlTransaction);

            return rowsEffected;
        }

        public int Delete(string column, object? value)
        {
            int rows;

            string tableName = typeof(RowType).GetTableName();

            if (typeof(RowType).FilterValidColumn(column) == null)
            {
                throw new Exception($"Invalid column '{column}' for table '{tableName}'");
            }

            SqlBuilder query = new SqlBuilder();

            var tmp = query.AddTemplate(
                @$"DELETE FROM {typeof(RowType).GetTableName()} /**where**/"
            );

            if (value != null)
            {
                query.Where($"{column} = @{nameof(value)}", new { value });
            }
            else
            {
                query.Where($"{column} is null");
            }

            rows = _connection.Execute(tmp.RawSql, transaction: _sqlTransaction);

            return rows;
        }

        public int Delete(string column, IEnumerable<object?> values)
        {
            if (!values.Any())
            {
                return 0;
            }

            int rows;

            string tableName = typeof(RowType).GetTableName();

            if (typeof(RowType).FilterValidColumn(column) == null)
            {
                throw new Exception($"Invalid column '{column}' for table '{tableName}'");
            }

            SqlBuilder query = new SqlBuilder();

            var tmp = query.AddTemplate(
                @$"DELETE FROM {typeof(RowType).GetTableName()} /**where**/"
            );

            if (values != null && values.Any())
            {
                query.Where($"{column} = @value");
            }

            rows = _connection.Execute(tmp.RawSql, values.Select(x => new { value = x }), transaction: _sqlTransaction);

            return rows;
        }

        public IEnumerable<RowType> GetAll()
        {
            IEnumerable<RowType> result = _connection.Query<RowType>($"SELECT * FROM {typeof(RowType).GetTableName()}", transaction: _sqlTransaction);
            return result;
        }

        public RowType GetById(KeyTypeA IdA, KeyTypeB IdB)
        {
            RowType result = null;

            var t = typeof(RowType);

            string tableName = t.GetTableName();
            var keys = t.GetKeyColumnNames();

            string query = $"SELECT * FROM {tableName} WHERE {keys[0]} = '{IdA}' and {keys[1]} = '{IdB}'";

            result = _connection.QuerySingle<RowType>(query, transaction: _sqlTransaction);

            return result;
        }

        public bool Update(RowType entity, List<string>? columnFilter = null)
        {
            int rowsEffected = 0;

            var t = typeof(RowType);

            string tableName = t.GetTableName();
            string cols = t.GetColumnNamesAsSqlAssignmentList(columnFilter, true);
            var keys = t.GetKeyColumnNames();

            StringBuilder query = new StringBuilder();

            query.Append($"UPDATE {tableName} SET {cols}");

            query.Append($" WHERE {keys[0]} = @{keys[0]} and {keys[1]} = @{keys[1]}");

            rowsEffected = _connection.Execute(query.ToString(), entity, transaction: _sqlTransaction);

            return rowsEffected == 1;
        }

        public int Update(IEnumerable<RowType> entities, List<string>? columnFilter = null)
        {
            if (!entities.Any())
            {
                return 0;
            }

            int rowsEffected = 0;

            var t = typeof(RowType);

            string tableName = t.GetTableName();
            string cols = t.GetColumnNamesAsSqlAssignmentList(columnFilter, true);
            var keys = t.GetKeyColumnNames();

            StringBuilder query = new StringBuilder();

            query.Append($"UPDATE {tableName} SET {cols}");

            query.Append($" WHERE {keys[0]} = @{keys[0]} and {keys[1]} = @{keys[1]}");

            rowsEffected = _connection.Execute(query.ToString(), entities, transaction: _sqlTransaction);

            return rowsEffected;
        }

        public List<RowType> Where(string column, object? value)
        {
            var rows = new List<RowType>();

            string tableName = typeof(RowType).GetTableName();

            if (typeof(RowType).FilterValidColumn(column) == null)
            {
                throw new Exception($"Invalid column '{column}' for table '{tableName}'");
            }

            SqlBuilder query = new SqlBuilder();

            var tmp = query.AddTemplate(
                @$"SELECT * FROM {typeof(RowType).GetTableName()} /**where**/"
            );

            if (value != null)
            {
                query.Where($"{column} = @{nameof(value)}", new { value });
            }
            else
            {
                query.Where($"{column} is null");
            }

            rows = _connection.Query<RowType>(tmp.RawSql, transaction: _sqlTransaction).ToList();

            return rows;
        }
    }
}
