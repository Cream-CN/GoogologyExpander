// BmsEngineFactory.cs - BMS 工厂 (仅BM4)
using System;
using System.Collections.Generic;

namespace GoogologyExpander
{
	public static class BmsEngineFactory
	{
		public static BmsEngine Create()
		{
			return new BmsEngine();
		}

		public static string GetVersionDescription()
		{
			return "Bashicu 版本4 (2018) - 默认";
		}
	}
}