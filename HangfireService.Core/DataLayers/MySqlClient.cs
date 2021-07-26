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

        public IEnumerable<T> Query<T>(string name, CommandType commandType = CommandType.StoredProcedure, DynamicParameters parameters = null, int? commandTimeout = null)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                result = connection.Query<T>(name, parameters, commandTimeout: commandTimeout, commandType: commandType);
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

        public int Execute(string name, CommandType commandType = CommandType.StoredProcedure, DynamicParameters parameters = null)
        {
            var sw = Stopwatch.StartNew();
            var result = 0;
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                result = connection.Execute(name, parameters, commandType: commandType);
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
        
        public int Execute(string name, DynamicParameters parameters, MySqlConnection connection, MySqlTransaction trans)
        {
            var result = 0;
            var sw = Stopwatch.StartNew();
            try
            {
                result = connection.Execute(name, parameters, commandType: CommandType.StoredProcedure,
                    transaction: trans);
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }
    }
}