// PrSS/PrssParser.cs - PrSS 解析器
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogologyExpander
{
	public static class PrssParser
	{
		public static List<int> Parse(string input)
		{
			if (string.IsNullOrWhiteSpace(input))
				return new List<int>();

			var parts = input.Split(new[] { ',', '，', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			var result = new List<int>();
			foreach (var part in parts)
			{
				if (int.TryParse(part.Trim(), out int value))
					result.Add(value);
				else
					throw new FormatException("无法解析 '" + part + "' 为整数");
			}
			return result;
		}

		public static string Format(List<int> sequence)
		{
			if (sequence == null || sequence.Count == 0)
				return "()";
			return "(" + string.Join(", ", sequence) + ")";
		}

		public static string FormatCompact(List<int> sequence)
		{
			if (sequence == null || sequence.Count == 0)
				return "[]";
			return "[" + string.Join(", ", sequence) + "]";
		}

		public static string FormatPlain(List<int> sequence)
		{
			if (sequence == null || sequence.Count == 0)
				return "";
			return string.Join(", ", sequence);
		}
	}
}