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
      MySqlSinkOptions sinkOptions = null,
      MySqlColumnOptions columnOptions = null)
    {
      if (sinkOptions == null)
      {
        sinkOptions = MySqlSinkOptions.Default;
      }

      if (columnOptions == null)
      {
        columnOptions = MySqlColumnOptions.Default;
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
