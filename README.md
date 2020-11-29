# Serilog.Sinks.MySql.Tvans

Provides a Serilog Sink for MySql with customizability in columns, data types.

[![Build Status](https://dev.azure.com/teunvanschagen/Serilog.Sinks.MySql.Tvans/_apis/build/status/Serilog.Sinks.MySql.Tvans-ASP.NET%20Core-CI?branchName=master)](https://dev.azure.com/teunvanschagen/Serilog.Sinks.MySql.Tvans/_build/latest?definitionId=2&branchName=master) [![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=Serilog.Sinks.MySql.Tvans&metric=alert_status)](https://sonarcloud.io/dashboard?id=Serilog.Sinks.MySql.Tvans)

## Example usage

Default
```cs
var logger = new LoggerConfiguration()
  .WriteTo.MySql("yourConnectionString")
  .CreateLogger();
```

Default with options for specified columns, or custom columns
```cs
var logger = new LoggerConfiguration()
  .WriteTo.MySql(
    "yourConnectionString", 
    MySqlSinkOptions.Default,
    MySqlColumnOptions.Default
      .With(new IdColumnOptions { DataType = new DataType(Kind.Guid), Name = "Id" })
      .With(new CustomColumnOptions 
      { 
        DataType = new DataType(Kind.Varchar, 128), 
        Name = "Application", 
        Value = "YourAppName" 
      }))
  .CreateLogger();
```

Options for sink and columns
```cs
var logger = new LoggerConfiguration()
  .WriteTo.MySql(
    "yourConnectionString",
    new MySqlSinkOptions 
    {
      tableName = "log_events",
      autoCreateTable = false,
    },
    new MySqlColumnOptions
    {
      IdColumnOptions = new IdColumnOptions { Name = "Guid", DataType = new DataType(Kind.Guid) },
      TimeStampColumnOptions = new TimeStampColumnOptions { Name = "TimeStamp", UseUtc = true }
      MessageTemplateColumnOptions = new MessageTemplateColumnOptions { Name = "Message" },
      LogEventColumnOptions = new LogEventColumnOptions 
      { 
        Name = "Properties", 
        EventSerializer = EventSerializer.Json 
      }
    })
  .CreateLogger();
```
