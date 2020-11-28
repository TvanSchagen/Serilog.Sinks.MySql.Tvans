﻿using System.Collections.Generic;

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

	public List<IColumnOptions> GetAll() => new List<IColumnOptions>
	{
	  IdColumnOptions,
	  TimeStampColumnOptions,
	  LogEventColumnOptions,
	  MessageColumnOptions,
	  MessageTemplateColumnOptions,
	  LevelColumnOptions,
	  ExceptionColumnOptions
	};

	public static MySqlColumnOptions Default()
	{ 
	  return new MySqlColumnOptions
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

  public class LevelColumnOptions: IColumnOptions {
	public DataType DataType { get; set; } = new DataType(Kind.Varchar) { Length = 16 };

	public string Name { get; set; }
  }

  public class ExceptionColumnOptions: IColumnOptions {
	public DataType DataType { get; set; } = new DataType(Kind.Text);

	public string Name { get; set; }
  }

  public class MessageColumnOptions: IColumnOptions {
	public DataType DataType { get; set; } = new DataType(Kind.Text);

	public string Name { get; set; }
  }

  public class MessageTemplateColumnOptions: IColumnOptions {
	public DataType DataType { get; set; } = new DataType(Kind.Text);

	public string Name { get; set; }
  }

  public class TimeStampColumnOptions : IColumnOptions
  {
	public bool UseUtc { get; set; } = false;

	public DataType DataType { get; set; } = new DataType(Kind.TimeStamp);

	public string Name { get; set; }
  }

  public class DataType
  {
	public DataType(Kind type)
	{
	  Type = type;
	}

	public Kind Type { get; set; }

	public int Length { get; set; } = 65535;
  }

  public enum Kind
  {
	Text,
	Varchar,
	DateTime,
	TimeStamp,    
	Guid,
	AutoIncrementInt
  }

  public class LogEventColumnOptions : IColumnOptions
  {
	public EventSerializer EventSerializer { get; set; } = EventSerializer.Json;

	public DataType DataType { get; set; } = new DataType(Kind.Text);

	public string Name { get; set; }

  }

  public enum EventSerializer
  {
	Xml,
	Json
  }

}
