using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Serilog.Core;
using System;
using System.Linq;

namespace Serilog.Sinks.MySql.Tvans.Test.Performance
{
	[SimpleJob(RuntimeMoniker.Net472)]
	[SimpleJob(RuntimeMoniker.Net48)]
	[SimpleJob(RuntimeMoniker.NetCoreApp31)]
	[SimpleJob(RuntimeMoniker.NetCoreApp50)]
	public class Benchmarks
	{

		private Logger _logger;

		private string _string;
		private object _complexObject;

		[GlobalSetup]
		public void Setup()
		{
			_logger = new LoggerConfiguration()
				.WriteTo.MySql(Environment.GetEnvironmentVariable("MYSQL_DATABASE_CONNECTIONSTRING"))
				.CreateLogger();

			_string = RandomString(1024);

			_complexObject = new
			{
				Field = RandomString(32),
				Field2 = RandomString(32),
				Field3 = 823589295,
				Field4 = Guid.NewGuid(),
				AnotherObject = new
				{
					Field = RandomString(128),
					AnotherField = RandomString(64),
					LongString = RandomString(256),
					OtherObject = new
					{
						Field = RandomString(128),
						NestedObject = new
						{
							Field = RandomString(256),
							DateTime = DateTime.MinValue
						}
					},
					BigField = new
					{
						Field = RandomString(256),
						Field2 = RandomString(256)
					}
				}
			};
		}

		[Benchmark]
		public void InsertComplexLogs()
		{
			for (int i = 0; i < 100; i++)
			{
				_logger.Information(_string + "{@_complexObject}", _complexObject);
			}
		}

		private static readonly Random random = new Random();

		public static string RandomString(int length)
		{
			const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			return new string(Enumerable.Repeat(chars, length)
			  .Select(s => s[random.Next(s.Length)]).ToArray());
		}
	}
}
