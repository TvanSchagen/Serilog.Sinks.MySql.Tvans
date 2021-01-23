/*
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

		public IList<IColumnOptions> AdditonalColumns { get; set; } = new List<IColumnOptions>();

		public IList<IColumnOptions> All
		{
			get
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
		}

		/// <summary>
		/// Returns the default set of columns that will be used for logging to.
		///		Id
		///		TimeStamp
		///		LogEvent
		///		Message
		///		MessageTemplate
		///		Level
		///		Exception
		/// </summary>
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

		/// <summary>
		/// Specifies properties for a single column. 
		/// </summary>
		/// <param name="opts">Extension method entry.</param>
		/// <param name="columnOptions">The column options to be used.</param>
		/// <returns>The options object with the new configuration, or an exception when no name is specified.</returns>
		public MySqlColumnOptions With(IColumnOptions columnOptions)
		{
			if (string.IsNullOrWhiteSpace(columnOptions?.Name))
			{
				throw new InvalidOperationException("When specifying a column, a name must be included.");
			}

			if (columnOptions is CustomColumnOptions)
			{
				AdditonalColumns.Add(columnOptions);
				return this;
			}
			var type = columnOptions.GetType().Name;
			GetType().GetProperty(type).SetValue(this, columnOptions);
			return this;
		}

		/// <summary>
		/// Excludes a column that is enabled by <see cref="MySqlColumnOptions.Default"/>.
		/// </summary>
		/// <param name="opts">Extension method entry.</param>
		/// <param name="columnOptions">An empty column options object of the to-be excluded type.</param>
		/// <returns>The options with the new configuration, or an exception if the name is specified.</returns>
		public MySqlColumnOptions Exclude(IColumnOptions columnOptions)
		{
			if (columnOptions == null || columnOptions.Name != null)
			{
				throw new InvalidOperationException("When excluding a column, an empty column object is expected (with no name).");
			}
			var type = columnOptions.GetType().Name;
			GetType().GetProperty(type).SetValue(this, columnOptions);
			return this;
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
		public const int DEFAULT_LEVEL_COLUMN_LENGTH = 16;

		public DataType DataType { get; set; } = new DataType(Kind.Varchar) { Length = DEFAULT_LEVEL_COLUMN_LENGTH };

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
		public bool UseUtc { get; set; }

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
		public const int DEFAULT_COLUMN_LENGTH = 65535;

		public DataType(Kind type, int length = DEFAULT_COLUMN_LENGTH)
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
