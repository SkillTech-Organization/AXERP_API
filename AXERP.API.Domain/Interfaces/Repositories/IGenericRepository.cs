﻿using AXERP.API.Domain.ServiceContracts.Requests;
using AXERP.API.Domain.ServiceContracts.Responses;
using System.Data;

namespace AXERP.API.Domain.Interfaces.Repositories
{
    public interface IGenericRepository
    {
        GenericPagedQueryResponse<dynamic> PagedQuery<RowType>(PagedQueryRequest request);

        IEnumerable<RowType> GetAll<RowType>(string tableName);

        int DeleteAll(string tableName);

        int CountAll(string tableName);

        IEnumerable<IdType> GetAllIDs<IdType>(string tableName);

        IEnumerable<RowType> GetAll<RowType>();

        int DeleteAll<RowType>();

        int CountAll<RowType>();

        IEnumerable<IdType> GetAllIDs<RowType, IdType>();

        DataTable CreateDataTable<T>(IEnumerable<T> list);

        void BulkCopy<T>(List<T> rows, DataRowState? state = null);
    }
}