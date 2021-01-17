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

namespace Serilog.Sinks.MySql.Tvans.Options
{
	public class MySqlColumnOptions
	{
		public IdColumnOptions IdColumnOptions { get; set; }

		public TimeStampColumnOptions TimeStampColumnOptions { get; set; }

		public LogEventColumnOptions LogEventColumnOptions { get; set; }

		public MessageColumnOptions MessageColumnOptions { get; set; }

		public MessageTemplateColumnOptions MessageTemplateColumnOptions { get; set; }

		public LevelColumnOptions LevelColumnOptions { get; set; }

		public ExceptionColumnOptions ExceptionColumnOptions { get; set; }

		public List<IColumnOptions> AdditonalColumns { get; set; } = new List<IColumnOptions>();

		public List<IColumnOptions> GetAll()
		{
			var list = new List<IColumnOptions>
			{
				IdColumnOptions,
				TimeStampColumnOptions,
				LogEventColumnOptions,
				MessageColumnOptions,
				MessageTemplateColumnOptions,
				LevelColumnOptions,
				ExceptionColumnOptions
			};
			list.AddRange(AdditonalColumns);
			return list;
		}

		public static MySqlColumnOptions Default => new MySqlColumnOptions
		{
			IdColumnOptions = new IdColumnOptions { Name = "Id" },
			TimeStampColumnOptions = new TimeStampColumnOptions { Name = "TimeStamp" },
			LogEventColumnOptions = new LogEventColumnOptions { Name = "Event" },
			MessageColumnOptions = new MessageColumnOptions { Name = "Message" },
			MessageTemplateColumnOptions = new MessageTemplateColumnOptions { Name = "Template" },
			LevelColumnOptions = new LevelColumnOptions { Name = "Level" },
			ExceptionColumnOptions = new ExceptionColumnOptions { Name = "Exception" }
		};

	}

	public static class MySqlColumnOptionsExtensions
	{
		public static MySqlColumnOptions With(this MySqlColumnOptions opts, IColumnOptions columnOptions)
		{
			if (string.IsNullOrWhiteSpace(columnOptions.Name))
			{
				throw new InvalidOperationException("When specifying a column, a name must be included.");
			}

			if (columnOptions is CustomColumnOptions)
			{
				opts.AdditonalColumns.Add(columnOptions);
				return opts;
			}
			var type = columnOptions.GetType().Name;
			opts.GetType().GetProperty(type).SetValue(opts, columnOptions);
			return opts;
		}

		public static MySqlColumnOptions Exclude(this MySqlColumnOptions opts, IColumnOptions columnOptions)
		{
			var type = columnOptions.GetType().Name;
			opts.GetType().GetProperty(type).SetValue(opts, columnOptions);
			return opts;
		}

	}


	public interface IColumnOptions
	{
		DataType DataType { get; set; }

		string Name { get; set; }
	}

	public class IdColumnOptions : IColumnOptions
	{
		public DataType DataType { get; set; } = new DataType(Kind.AutoIncrementInt);

		public string Name { get; set; }
	}

	public class LevelColumnOptions : IColumnOptions
	{
		public DataType DataType { get; set; } = new DataType(Kind.Varchar) { Length = 16 };

		public string Name { get; set; }
	}

	public class ExceptionColumnOptions : IColumnOptions
	{
		public DataType DataType { get; set; } = new DataType(Kind.Text);

		public string Name { get; set; }
	}

	public class MessageColumnOptions : IColumnOptions
	{
		public DataType DataType { get; set; } = new DataType(Kind.Text);

		public string Name { get; set; }
	}

	public class MessageTemplateColumnOptions : IColumnOptions
	{
		public DataType DataType { get; set; } = new DataType(Kind.Text);

		public string Name { get; set; }
	}

	public class TimeStampColumnOptions : IColumnOptions
	{
		public bool UseUtc { get; set; } = false;

		public DataType DataType { get; set; } = new DataType(Kind.TimeStamp);

		public string Name { get; set; }
	}

	public class CustomColumnOptions : IColumnOptions
	{
		public string Name { get; set; }

		public DataType DataType { get; set; }

		public object Value { get; set; }
	}

	public class DataType
	{
		public DataType(Kind type, int length = 65535)
		{
			Type = type;
			Length = length;
		}

		public Kind Type { get; set; }

		public int Length { get; set; }
	}

	public enum Kind
	{
		Text,
		Varchar,
		DateTime,
		TimeStamp,
		UnixTime,
		Guid,
		AutoIncrementInt
	}

	public class LogEventColumnOptions : IColumnOptions
	{
		public EventSerializer EventSerializer { get; set; } = EventSerializer.Json;

		public DataType DataType { get; set; } = new DataType(Kind.Text);

		public string Name { get; set; }

	}

	/// <summary>
	/// Only Json is not supported at this time.
	/// </summary>
	public enum EventSerializer
	{
		Json
	}

}
