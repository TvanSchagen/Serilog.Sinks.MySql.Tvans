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
using Serilog.Configuration;
using Serilog.Debugging;
using Serilog.Sinks.MySql.Tvans.Options;
using Serilog.Sinks.MySql.Tvans.Sinks;

namespace Serilog.Sinks.MySql.Tvans
{
	public static class LoggerConfigurationExtensions
	{
		/// <summary>
		/// Allows the sink to integrate into the SeriLog configuration through code.
		/// </summary>
		/// <param name="loggerSinkConfiguration"></param>
		/// <param name="connectionString">Determines how to connect to the desired database to log to.</param>
		/// <param name="sinkOptions">Defines the behaviour of the sink.</param>
		/// <param name="columnOptions">Defines which columns and types to use.</param>
		/// <returns>A logger configuration</returns>
		public static LoggerConfiguration MySql(
		  this LoggerSinkConfiguration loggerSinkConfiguration,
		  string connectionString,
		  MySqlSinkOptions sinkOptions = null,
		  MySqlColumnOptions columnOptions = null)
		{
			sinkOptions ??= MySqlSinkOptions.Default;
			columnOptions ??= MySqlColumnOptions.Default;
			
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
