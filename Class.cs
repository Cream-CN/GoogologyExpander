// Class.cs - 修改 Expand 方法直接输出序列
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

		// 新增：纯序列输出，无括号
		public static string FormatPlain(List<int> sequence)
		{
			if (sequence == null || sequence.Count == 0)
				return "";
			return string.Join(", ", sequence);
		}
	}

	public class PrssEngine
	{
		// 直接返回序列，不包含括号
		public string Expand(List<int> sequence, int steps)
		{
			var current = new List<int>(sequence);
			int actualSteps = 0;

			for (int step = 1; step <= steps; step++)
			{
				if (current.Count == 0)
					break;

				current = ExpandOneStep(current);
				actualSteps++;
			}

			// 直接返回纯序列，不带括号
			return PrssParser.FormatPlain(current);
		}

		public List<int> ExpandOneStep(List<int> sequence)
		{
			if (sequence == null || sequence.Count == 0)
				return new List<int>();

			var seq = new List<int>(sequence);
			int m = seq.Count - 1;
			int last = seq[m];

			if (last == 0)
			{
				seq.RemoveAt(m);
				return seq;
			}

			int badRootIndex = FindBadRoot(seq, m);
			if (badRootIndex == -1)
			{
				seq.RemoveAt(m);
				return seq;
			}

			var goodPart = seq.Take(badRootIndex).ToList();
			var badPart = seq.Skip(badRootIndex).Take(m - badRootIndex).ToList();

			var result = new List<int>(goodPart);
			result.AddRange(badPart);

			var badPartCopy = new List<int>(badPart);
			if (badPartCopy.Count > 0)
			{
				badPartCopy[badPartCopy.Count - 1] = seq[badRootIndex];
				result.AddRange(badPartCopy);
			}

			return result;
		}

		private int FindBadRoot(List<int> seq, int m)
		{
			int ak = seq[m];
			for (int i = m - 1; i >= 0; i--)
			{
				if (seq[i] < ak)
					return i;
			}
			return -1;
		}

		public bool IsEmpty(List<int> sequence)
		{
			return sequence == null || sequence.Count == 0;
		}

		public int GetValue(List<int> sequence)
		{
			if (sequence == null || sequence.Count == 0)
				return 0;

			return CalculateValueRecursive(sequence);
		}

		private int CalculateValueRecursive(List<int> seq)
		{
			if (seq.Count == 0)
				return 0;

			if (seq[seq.Count - 1] == 0)
			{
				var prefix = seq.Take(seq.Count - 1).ToList();
				return CalculateValueRecursive(prefix) + 1;
			}

			var expanded = ExpandOneStep(seq);
			return CalculateValueRecursive(expanded);
		}

		public int ExpandToEmpty(List<int> sequence)
		{
			int steps = 0;
			var current = new List<int>(sequence);

			while (current.Count > 0)
			{
				current = ExpandOneStep(current);
				steps++;
			}

			return steps;
		}

		public List<List<int>> ExpandWithHistory(List<int> sequence, int maxSteps)
		{
			var history = new List<List<int>>();
			var current = new List<int>(sequence);
			history.Add(new List<int>(current));

			for (int i = 0; i < maxSteps && current.Count > 0; i++)
			{
				current = ExpandOneStep(current);
				history.Add(new List<int>(current));
			}

			return history;
		}
	}
}