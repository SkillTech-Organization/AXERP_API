﻿using AXERP.API.Business.Interfaces.Repositories;
using AXERP.API.Business.Interfaces.UnitOfWork;
using AXERP.API.Persistence.Utils;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Text;

namespace AXERP.API.Persistence.Repositories
{
    public class GenericEntityRepository<RowType> : IRepository<RowType> where RowType : class
    {
        private readonly IConnectionProvider _connectionProvider;
        private SqlConnection _connection => _connectionProvider.Connection;
        private SqlTransaction? _sqlTransaction => _connectionProvider.Transaction;

        public GenericEntityRepository(IConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public int Add(List<RowType> entities, bool insertId = false)
        {
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
            string key = t.GetKeyColumnName();
            string query = $"DELETE FROM {tableName} WHERE {key} = @{key}";

            rowsEffected = _connection.Execute(query, entity, transaction: _sqlTransaction);

            return rowsEffected > 0 ? true : false;
        }

        public IEnumerable<RowType> GetAll()
        {
            IEnumerable<RowType> result = _connection.Query<RowType>($"SELECT * FROM {typeof(RowType).GetTableName()}", transaction: _sqlTransaction);
            return result;
        }

        public RowType GetById(int Id)
        {
            RowType result = null;

            var t = typeof(RowType);

            string tableName = t.GetTableName();
            string key = t.GetKeyColumnName();

            string query = $"SELECT * FROM {tableName} WHERE {key} = '{Id}'";

            result = _connection.QuerySingle<RowType>(query, transaction: _sqlTransaction);

            return result;
        }

        public bool Update(RowType entity)
        {
            int rowsEffected = 0;
            try
            {
                var t = typeof(RowType);

                string tableName = t.GetTableName();
                string cols = t.GetColumnNamesAsSqlAssignmentList(null, true);
                string key = t.GetKeyColumnName();

                StringBuilder query = new StringBuilder();

                query.Append($"UPDATE {tableName} SET {cols}");

                query.Remove(query.Length - 1, 1);

                query.Append($" WHERE {key} = @{key}");

                rowsEffected = _connection.Execute(query.ToString(), entity, transaction: _sqlTransaction);
            }
            catch (Exception ex) { }

            return rowsEffected > 0 ? true : false;
        }
    }
}
