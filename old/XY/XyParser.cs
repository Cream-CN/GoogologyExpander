// XY/XyParser.cs - X-Y 解析器
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogologyExpander
{
	public static class XyParser
	{
		public static List<int> Parse(string input)
		{
			if (string.IsNullOrWhiteSpace(input))
				return new List<int>();

			var parts = input.Split(new[] { ',', '，', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
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

		public static string FormatPlain(List<int> sequence)
		{
			if (sequence == null || sequence.Count == 0)
				return "";
			return string.Join(", ", sequence);
		}

		public static bool IsValid(List<int> sequence)
		{
			if (sequence == null || sequence.Count == 0)
				return true;

			// X-Y序列必须满足：第一个元素为1，且每个元素为正整数
			if (sequence[0] != 1)
				return false;

			foreach (var v in sequence)
			{
				if (v <= 0)
					return false;
			}
			return true;
		}
	}
}