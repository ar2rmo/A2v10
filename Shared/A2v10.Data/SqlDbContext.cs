﻿using A2v10.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace A2v10.Data
{
	public class SqlDbContext : IDbContext
	{
		IConfiguration _config;

		public SqlDbContext(IConfiguration config)
		{
			_config = config;
		}

		public async Task<SqlConnection> GetConnectionAsync()
		{
			var cnnStr = _config.GetConnectionString();
			var cnn = new SqlConnection(cnnStr);
			await cnn.OpenAsync();
			return cnn;
		}

		public async Task<IDataModel> LoadModelAsync(String command, Object prms = null)
		{
			var modelLoader = new DataModelLoader();
			using (var p = _config.Profiler.Start(ProfileAction.Sql, command))
			{
				await ReadDataAsync(command,
					(prm) =>
					{
					},
					(no, rdr) =>
					{
						modelLoader.ProcessOneRecord(rdr);
					},
					(no, rdr) =>
					{
						modelLoader.ProcessOneMetadata(rdr);
					});
			}
			return modelLoader.DataModel;
		}

		public async Task ReadDataAsync(String command,
			Action<SqlParameterCollection> setParams,
			Action<Int32, IDataReader> onRead,
			Action<Int32, IDataReader> onMetadata)
		{
			using (var cnn = await GetConnectionAsync())
			{
				Int32 rdrNo = 0;
				using (var cmd = cnn.CreateCommandSP(command))
				{
					if (setParams != null)
						setParams(cmd.Parameters);
					using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
					{
						do
						{
							if (onMetadata != null)
								onMetadata(rdrNo, rdr);
							while (await rdr.ReadAsync())
							{
								if (onRead != null)
									onRead(rdrNo, rdr);
							}
							rdrNo += 1;
						} while (await rdr.NextResultAsync());
					}
				}
			}
		}

	}
}
