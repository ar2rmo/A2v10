﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildSql
{
	class Program
	{
		static void Main(String[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine("Usage: buildsql [appdir]");
				return;
			}

			String dir = args[0].ToLowerInvariant();
			Console.WriteLine($"Processing: {dir}");

			try
			{
				SqlFileBuilder fb = new SqlFileBuilder(dir);
				fb.Process();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Exception: {ex.Message}");
			}
			Console.WriteLine();
		}
	}
}
