/*
 * Copyright 2020 Teun van Schagen

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
