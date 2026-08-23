// BMS/BmsEngineFactory.cs - BMS 版本工厂
using System;
using System.Collections.Generic;

namespace GoogologyExpander
{
	public static class BmsEngineFactory
	{
		public static BmsEngine Create(BMVersion version)
		{
			return new BmsEngine(version);
		}

		public static BmsEngine CreateDefault()
		{
			return new BmsEngine(BMVersion.BM4);
		}

		public static string GetVersionDescription(BMVersion version)
		{
			switch (version)
			{
				case BMVersion.BM1:
					return "Bashicu 原始版本 (2014)";
				case BMVersion.BM2:
					return "Bashicu 版本2 (2016)";
				case BMVersion.BM2_1:
					return "koteitan 版本2.1 (2018)";
				case BMVersion.BM2_2:
					return "koteitan 版本2.2 (2018)";
				case BMVersion.BM2_3:
					return "koteitan 版本2.3 (2018)";
				case BMVersion.BM3:
					return "Bashicu 版本3 (2018)";
				case BMVersion.BM3_1:
					return "Nish 版本3.1 (2018)";
				case BMVersion.BM3_2:
					return "Nish 版本3.2 (2018)";
				case BMVersion.BM3_3:
					return "rpakr/Ecl1psed 版本3.3 (2019)";
				case BMVersion.BM4:
					return "Bashicu 版本4 (2018) - 默认";
				default:
					return "";
			}
		}

		public static List<BMVersion> GetAllVersions()
		{
			return new List<BMVersion>
			{
				BMVersion.BM1,
				BMVersion.BM2,
				BMVersion.BM2_1,
				BMVersion.BM2_2,
				BMVersion.BM2_3,
				BMVersion.BM3,
				BMVersion.BM3_1,
				BMVersion.BM3_2,
				BMVersion.BM3_3,
				BMVersion.BM4
			};
		}
	}
}