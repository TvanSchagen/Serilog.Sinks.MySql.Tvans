using System;

namespace Serilog.Sinks.MySql.Tvans.Options
{
  public class MySqlSinkOptions
  {

    public string TableName { get; set; }

    public bool CreateTable { get; set; }

    public MySqlSinkOptions(
      string tableName,
      bool createTable = true)
    {
      TableName = tableName ?? throw new ArgumentNullException(nameof(tableName), "Table name must be specified.");
      CreateTable = createTable;
    }

  }
}
