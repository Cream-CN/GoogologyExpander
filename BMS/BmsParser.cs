// BMS/BmsParser.cs - BMS 解析器
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogologyExpander
{
	public static class BmsParser
	{
		public static List<List<int>> Parse(string input)
		{
			var result = new List<List<int>>();
			if (string.IsNullOrWhiteSpace(input))
				return result;

			var parts = input.Split(new[] { ')' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var part in parts)
			{
				var cleaned = part.Trim().TrimStart('(');
				if (string.IsNullOrEmpty(cleaned))
					continue;

				var nums = cleaned.Split(new[] { ',', '，', ' ' }, StringSplitOptions.RemoveEmptyEntries)
								  .Select(s => int.Parse(s.Trim()))
								  .ToList();
				result.Add(nums);
			}
			return result;
		}

		public static string Format(List<List<int>> matrix)
		{
			if (matrix == null || matrix.Count == 0)
				return "()";
			return string.Join("", matrix.Select(col => "(" + string.Join(",", col) + ")"));
		}

		public static string FormatWithSpaces(List<List<int>> matrix)
		{
			if (matrix == null || matrix.Count == 0)
				return "()";
			return string.Join(" ", matrix.Select(col => "(" + string.Join(",", col) + ")"));
		}
	}
}