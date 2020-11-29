using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using MySql.Data.MySqlClient;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.MySql.Tvans.Options;

namespace Serilog.Sinks.MySql.Tvans.Sinks
{
  public class MySqlSink : ILogEventSink
  {
    private readonly string _connectionString;
    private readonly MySqlColumnOptions _columnOptions;
    private readonly MySqlSinkOptions _sinkOptions;

    public MySqlSink(
      string connectionString,
      MySqlSinkOptions sinkOptions,
      MySqlColumnOptions columnOptions)
    {
      _connectionString = connectionString;
      _columnOptions = columnOptions;
      _sinkOptions = sinkOptions;

      if (sinkOptions.CreateTable)
      {
        CreateTable();
      }
    }

    public IEnumerable<IColumnOptions> GetInsertColumns() => _columnOptions
      .GetAll()
      .Where(c => c.Name != null)
      // exclude id column with auto increment because it 
      // does not accept a value
      .Where(c => !(c is IdColumnOptions idc &&
                    idc.DataType.Type == Kind.AutoIncrementInt));

    public void Emit(LogEvent logEvent)
    {
      using var con = GetConnection();
      var cmd = GetInsertCommand(con);

      var logMessageString = new StringWriter(new StringBuilder());
      logEvent.RenderMessage(logMessageString);

      foreach (var column in GetInsertColumns())
      {
        var value = column switch
        {
          IdColumnOptions idColumn => idColumn.DataType.Type == Kind.AutoIncrementInt 
            ? "NULL" 
            : Guid.NewGuid().ToString(),
          TimeStampColumnOptions tsColumn => tsColumn.UseUtc
            ? logEvent.Timestamp.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fff")
            : logEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
          ExceptionColumnOptions _ => logEvent.Exception?.ToString(),
          MessageColumnOptions _ => logMessageString.ToString(),
          MessageTemplateColumnOptions _ => logEvent.MessageTemplate.ToString(),
          LogEventColumnOptions logEventColumn => logEvent.Properties.Any()
            ? Serialize(logEvent.Properties, logEventColumn.EventSerializer) 
            : string.Empty,
          LevelColumnOptions _ => logEvent.Level.ToString(),
          _ => throw new ArgumentOutOfRangeException(nameof(column))
        };
        cmd.Parameters["@" + column.Name].Value = value;
      }

      try
      {
        cmd.ExecuteNonQuery();
      }
      catch (Exception exc)
      {
        SelfLog.WriteLine("{0}: {1}", exc.Message, exc.StackTrace);
      }
    }

    public string Serialize(IReadOnlyDictionary<string, LogEventPropertyValue> dict, EventSerializer serializer)
    {
      var unwrapped = dict.ToDictionary(
          kvp => kvp.Key, 
          kvp => kvp.Value
            .ToString()
            .TrimStart('"')
            .TrimEnd('"'));

      return serializer switch
      {
          EventSerializer.Json => JsonSerializer.Serialize(unwrapped),
          EventSerializer.Xml => throw new NotImplementedException(),
          _ => throw new NotImplementedException(),
      };
    }

    public MySqlConnection GetConnection()
    {
      try
      {
        var con = new MySqlConnection(_connectionString);
        con.Open();
        return con;
      }
      catch (Exception exc)
      {
        SelfLog.WriteLine("Error getting connection: {0}, stacktrace: {1}", 
          exc.Message, 
          exc.StackTrace);
      }
      return null;
    }

    public void CreateTable()
    {
      var sb = new StringBuilder();
      using var con = GetConnection();
      var columns = _columnOptions.GetAll()
        .Where(c => c.Name != null)
        .ToList();

      sb.Append($"CREATE TABLE IF NOT EXISTS {_sinkOptions.TableName} (");
      
      var index = 0;
      foreach (var column in columns)
      {
        index++;
        sb.Append(column.Name + $" {GetDataTypeString(column)}{(index == columns.Count ? string.Empty : ", ")}");
      }

      sb.Append(")");

      var cmd = new MySqlCommand(sb.ToString(), con);
      cmd.ExecuteNonQuery();
    }

    public string GetDataTypeString(IColumnOptions column)
    {
      var type = column.DataType;
      return (type.Type, type.Length) switch
      {
        (Kind.TimeStamp, _) => "TIMESTAMP DEFAULT CURRENT_TIMESTAMP",
        (Kind.Text, _) => "TEXT",
        (Kind.AutoIncrementInt, _) => "INT NOT NULL AUTO_INCREMENT PRIMARY KEY",
        (Kind.Guid, _) => "CHAR(36) PRIMARY KEY",
        _ => type.Type + $"({type.Length})"
      };
    }

    public MySqlCommand GetInsertCommand(MySqlConnection connection)
    {
      var cmd = new MySqlCommand();
      var sb = new StringBuilder();

      var columns = GetInsertColumns().ToList();

      var columnNames = columns
        .Select(c => c.Name)
        .Aggregate((c, c2) => c + ", " + c2);

      sb.Append($"INSERT INTO {_sinkOptions.TableName} ({columnNames})");
      
      var columnVariables = columns
        .Select(c => "@" + c.Name)
        .Aggregate((c, c2) => c + ", " + c2);

      sb.Append($"VALUES ({columnVariables})");

      foreach (var column in columns)
      {
        cmd.Parameters.Add($"@{column.Name}", MySqlDbType.VarChar);
      }

      cmd.CommandText = sb.ToString();
      cmd.Connection = connection;
      return cmd;
    }

  }
}
