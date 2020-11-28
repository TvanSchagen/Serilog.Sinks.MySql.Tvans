using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
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

    public void Emit(LogEvent logEvent)
    {
      using var con = GetConnection();
      var cmd = GetInsertCommand(con);

      var columns = _columnOptions
        .GetAll()
        .Where(c => c.Name != null)
        .ToList();

      var logMessageString = new StringWriter(new StringBuilder());
      logEvent.RenderMessage(logMessageString);

      foreach (var column in columns)
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
            ? logEventColumn.EventSerializer == EventSerializer.Json 
              ? SerializeToJson(logEvent.Properties) 
              : SerializeToXml(logEvent.Properties)
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

    public string SerializeToJson(IReadOnlyDictionary<string, LogEventPropertyValue> dict)
    {
      return JsonConvert.SerializeObject(dict);
    }

    public string SerializeToXml(IReadOnlyDictionary<string, LogEventPropertyValue> dict)
    {
      throw new NotImplementedException();
    }

    public string GetColumnNameFromType(IColumnOptions column)
    {
      return column switch
      {
        IdColumnOptions idColumn => idColumn.Name,
        ExceptionColumnOptions excColumn => excColumn.Name,
        MessageColumnOptions msgColumn => msgColumn.Name,
        MessageTemplateColumnOptions msgTemplateColumn => msgTemplateColumn.Name,
        LogEventColumnOptions logEventColumn => logEventColumn.Name,
        LevelColumnOptions levelColumn => levelColumn.Name,
        _ => throw new ArgumentOutOfRangeException(nameof(column))
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
      using var con = GetConnection();
      var columns = _columnOptions.GetAll()
        .Where(c => c.Name != null)
        .ToList();
      var sb = new StringBuilder();
      sb.Append($"CREATE TABLE IF NOT EXISTS {_sinkOptions.TableName} (");
      var index = 0;
      foreach (var column in columns)
      {
        index++;
        if (column.Name == null)
        {
          throw new ArgumentNullException(column.Name, "An included column cannot be nameless.");
        }
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
        (Kind.Guid, _) => "CHAR(36)",
        _ => type.Type + $"({type.Length})"
      };
    }

    public MySqlCommand GetInsertCommand(MySqlConnection connection)
    {
      var cmd = new MySqlCommand();
      var sb = new StringBuilder();

      var columns = _columnOptions
        .GetAll()
        .Where(c => c.Name != null)
        .ToList();

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
