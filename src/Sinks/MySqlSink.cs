﻿/*
 * Copyright 2021 Teun van Schagen

 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at

 *     http://www.apache.org/licenses/LICENSE-2.0

 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.MySql.Tvans.Options;

namespace Serilog.Sinks.MySql.Tvans.Sinks
{
	public class MySqlSink : ILogEventSink
	{

		private static string DefaultTimeStampFormat => "yyyy-MM-dd HH:mm:ss.ffffff";

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

			if (sinkOptions?.CreateTable == true)
			{
				CreateTable();
			}
		}

		public IEnumerable<IColumnOptions> GetInsertColumns() => _columnOptions
			.GetAll()
			// only include columns with a name, others are 
			// considered to be explicitly left out
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
					TimeStampColumnOptions tsColumn => 
						GetDateTimeFormat(logEvent.Timestamp, tsColumn.DataType.Type, tsColumn.UseUtc),
					ExceptionColumnOptions _ => logEvent.Exception?.ToString(),
					MessageColumnOptions _ => logMessageString.ToString(),
					MessageTemplateColumnOptions _ => logEvent.MessageTemplate.ToString(),
					LogEventColumnOptions logEventColumn => logEvent.Properties.Any()
					  ? Serialize(logEvent.Properties)
					  : string.Empty,
					LevelColumnOptions _ => logEvent.Level.ToString(),
					// if a value was specified for the custom column, take it
					// otherwise, look in the properties for it
					CustomColumnOptions customColumn => customColumn.Value ??
														GetValueOrNull(logEvent.Properties, customColumn.Name),
					_ => throw new NotSupportedException($"{column} is not supported as column options.")
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

		public object GetDateTimeFormat(
			DateTimeOffset timeStamp, 
			Kind dataType, 
			bool utc)
		{
			return (dataType, utc) switch
			{
				(Kind.TimeStamp, false) => timeStamp.ToString(DefaultTimeStampFormat),
				(Kind.TimeStamp, true) => timeStamp.ToUniversalTime().ToString(DefaultTimeStampFormat),
				(Kind.UnixTime, _) => timeStamp.ToUnixTimeMilliseconds(),
				_ => throw new NotImplementedException($"{dataType} is not supported as a date-time format.")
			};
		}

		public string GetValueOrNull(
			IReadOnlyDictionary<string, LogEventPropertyValue> dict, 
			string propertyName)
		{

			if (dict.Any(kvp => kvp.Key.Equals(propertyName, StringComparison.OrdinalIgnoreCase)))
			{
				return dict
					.Single(kvp => kvp.Key
						.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
							.Value.ToString();
			}
			return "NULL";
		}

		public string Serialize(IReadOnlyDictionary<string, LogEventPropertyValue> dict)
		{
			var formatter = new JsonValueFormatter(typeTagName: null);
			var builder = new StringBuilder();
			using (var writer = new StringWriter(builder))
			{
				writer.Write("{");
				foreach (var kvp in dict)
				{
					JsonValueFormatter.WriteQuotedJsonString(kvp.Key, writer);
					writer.Write(":");
					formatter.Format(kvp.Value, writer);
				}
				writer.Write("},");
			}
			return builder.ToString();
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

			var columns = _columnOptions.GetAll()
			  .Where(c => c.Name != null)
			  .ToList();

			using var con = GetConnection();

			sb.Append($"CREATE TABLE IF NOT EXISTS {_sinkOptions.TableName} (");

			columns.ForEach(c => sb.Append(c.Name + $" {GetDataTypeString(c)}, "));

			sb.Append($"PRIMARY KEY ({columns.Single(c => c is IdColumnOptions).Name}))");

			var cmd = new MySqlCommand(sb.ToString(), con);
			cmd.ExecuteNonQuery();
		}

		public string GetDataTypeString(IColumnOptions column)
		{
			var type = column.DataType;
			return (type.Type, type.Length) switch
			{
				(Kind.TimeStamp, _) => "TIMESTAMP(6) DEFAULT CURRENT_TIMESTAMP(6)",
				(Kind.DateTime, _) => "DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6)",
				(Kind.Text, _) => "TEXT",
				(Kind.AutoIncrementInt, _) => "INT NOT NULL AUTO_INCREMENT",
				(Kind.Guid, _) => "CHAR(36)",
				(Kind.UnixTime, _) => "BIGINT",
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
