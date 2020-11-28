using System;
using Serilog.Configuration;
using Serilog.Debugging;
using Serilog.Sinks.MySql.Tvans.Options;
using Serilog.Sinks.MySql.Tvans.Sinks;

namespace Serilog.Sinks.MySql.Tvans
{
  public static class LoggerConfigurationExtensions
  {
    public static LoggerConfiguration MySql(
      this LoggerSinkConfiguration loggerSinkConfiguration,
      string connectionString,
      MySqlSinkOptions sinkOptions,
      MySqlColumnOptions columnOptions)
    {
      if (sinkOptions == null)
      {
        throw new ArgumentNullException(nameof(sinkOptions), "Sink options must be specified.");
      }

      if (columnOptions == null)
      {
        throw new ArgumentNullException(nameof(sinkOptions), "Column options must be specified, and columns initialized with names.");
      }

      try
      {
        return loggerSinkConfiguration.Sink(
          new MySqlSink(
            connectionString,
            sinkOptions,
            columnOptions));
      }
      catch (Exception exc)
      {
        SelfLog.WriteLine("An error occured trying to setup the logger: {0}, stacktrace: {1}", exc.Message, exc.StackTrace);
        throw;
      }
    }
  }
}
