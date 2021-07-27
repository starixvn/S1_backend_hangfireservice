using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using Dapper;
using HangfireService.Core.DataLayers.Interfaces;
using HangfireService.Core.Models.Enums;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Serilog.Context;

namespace HangfireService.Core.DataLayers
{
    public sealed class MySqlClient: IDataBaseInterface
    {
        private readonly string _connectionString;

        private readonly ILogger<MySqlClient> _logger;

        public DataBase DataBase { get; set; }

        public MySqlClient(string connectionString, ILogger<MySqlClient> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string name, CommandType commandType = CommandType.StoredProcedure, DynamicParameters parameters = null, int? commandTimeout = null)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                result = await connection.QueryAsync<T>(name, parameters, commandType: commandType, commandTimeout: commandTimeout);
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogInformation($"{name} {sw.Elapsed:c}");
            return result;
        }

        public async Task<T> QueryFirstAsync<T>(string name, CommandType commandType = CommandType.StoredProcedure, DynamicParameters parameters = null, int? commandTimeout = null)
        {
            T result;
            var resultCode = string.Empty;
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                result = await connection.QueryFirstOrDefaultAsync<T>(name, parameters, commandType: commandType, commandTimeout: commandTimeout);
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogInformation($"{name} {resultCode} {sw.Elapsed:c}");
            return result;
        }

        public async Task<int> ExecuteAsync(string name, CommandType commandType = CommandType.StoredProcedure, DynamicParameters parameters = null)
        {
            var sw = Stopwatch.StartNew();
            int result;
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                result = await connection.ExecuteAsync(name, param: parameters, commandType: commandType);
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogInformation($"{name} {sw.Elapsed:c}");
            return result;
        }
    }
}