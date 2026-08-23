// BMS/BmsExpandResult.cs - BMS 展开结果类
using System;
using System.Collections.Generic;
using System.Text;

namespace GoogologyExpander
{
	public class BmsExpandResult
	{
		public string InitialMatrix { get; set; }
		public string FinalMatrix { get; set; }
		public BMVersion Version { get; set; }
		public List<string> Steps { get; set; }
		public List<string> StepMatrices { get; set; }
		public int TotalSteps { get; set; }
		public bool IsEmpty { get; set; }

		public override string ToString()
		{
			if (Steps == null || Steps.Count == 0)
				return "无展开步骤";

			return string.Join("\n", Steps);
		}

		public string GetDetailedReport()
		{
			var report = new StringBuilder();
			report.AppendLine($"版本: {Version}");
			report.AppendLine($"初始矩阵: {InitialMatrix}");
			report.AppendLine($"最终矩阵: {FinalMatrix}");
			report.AppendLine($"总步数: {TotalSteps}");
			report.AppendLine($"是否为空: {IsEmpty}");
			report.AppendLine();
			report.AppendLine("展开过程:");
			foreach (var step in Steps)
			{
				report.AppendLine(step);
			}
			return report.ToString();
		}
	}
}