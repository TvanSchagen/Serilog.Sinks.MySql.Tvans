using BenchmarkDotNet.Running;
using Serilog.Sinks.MySql.Tvans.Test.Performance;

namespace Serilog.Sinks.MySql.Tvans.Bench
{
	internal class Program
	{
		static void Main()
		{
			BenchmarkRunner.Run<Benchmarks>();
			System.Console.ReadLine();
		}
	}
}
