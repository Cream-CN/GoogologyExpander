// Class.cs - 保留 PrSS、BMS，新增 LPrSS
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogologyExpander
{
	// ==================== 解析 / 格式化 ====================
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

	// ==================== LPrSS 解析器 ====================
	public static class LPrssParser
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

		public static string FormatPlain(List<int> sequence)
		{
			if (sequence == null || sequence.Count == 0)
				return "";
			return string.Join(", ", sequence);
		}
	}

	// ==================== LPrSS 引擎 ====================
	/// <summary>
	/// LPrSS (Limit Primitive Sequence System) 引擎
	/// 基于定义 14.1 和 14.2
	/// </summary>
	public class LPrssEngine
	{
		/// <summary>
		/// 展开序列指定步数
		/// </summary>
		public string Expand(List<int> sequence, int steps)
		{
			var current = new List<int>(sequence);

			if (current.Count == 0)
				return "";

			for (int step = 1; step <= steps; step++)
			{
				if (current.Count == 0)
					break;

				current = ExpandOneStep(current);

				if (current.SequenceEqual(sequence) && step > 1)
					break;
			}

			return LPrssParser.FormatPlain(current);
		}

		/// <summary>
		/// 单步展开
		/// 定义 14.2:
		/// (1) ( ) = 0
		/// (2) (#, 1) = (#) + 1
		/// (3) 否则将好部保持不动，坏部在序列的末端不断复制。
		///     每复制一次就将坏部的各项都加上一个常数，这个常数等于阶差减一。
		/// </summary>
		public List<int> ExpandOneStep(List<int> sequence)
		{
			if (sequence == null || sequence.Count == 0)
				return new List<int>();

			var seq = new List<int>(sequence);
			int m = seq.Count - 1;
			int last = seq[m];

			// 规则 (2): (#, 1) = (#) + 1
			if (last == 1)
			{
				seq.RemoveAt(m);
				return seq;
			}

			// 规则 (3): 否则执行坏部复制
			int badRootIndex = FindBadRoot(seq, m);

			if (badRootIndex == -1)
			{
				seq.RemoveAt(m);
				return seq;
			}

			// 好部为坏根左边的元素，不包含坏根
			// 坏部为坏根右边和最后一个元素之间的元素，包含坏根，但是不包含最后一个元素
			var goodPart = seq.Take(badRootIndex).ToList();
			var badPart = seq.Skip(badRootIndex).Take(m - badRootIndex).ToList();

			// 阶差为末项与坏根之间的差值
			int delta = last - seq[badRootIndex];

			// 每复制一次就将坏部的各项都加上一个常数，这个常数等于阶差减一
			int increment = delta - 1;

			var result = new List<int>(goodPart);
			result.AddRange(badPart);

			var badCopy = new List<int>(badPart);
			for (int i = 0; i < badCopy.Count; i++)
			{
				badCopy[i] += increment;
			}
			result.AddRange(badCopy);

			return result;
		}

		/// <summary>
		/// 定义 14.1: 坏根为在最后一个元素左边，且小于最后一个元素的第一个元素
		/// </summary>
		private int FindBadRoot(List<int> seq, int m)
		{
			int last = seq[m];
			for (int i = m - 1; i >= 0; i--)
			{
				if (seq[i] < last)
					return i;
			}
			return -1;
		}

		public bool IsEmpty(List<int> sequence) => sequence == null || sequence.Count == 0;

		public int GetValue(List<int> sequence)
		{
			if (sequence == null || sequence.Count == 0)
				return 0;

			int steps = 0;
			var current = new List<int>(sequence);
			while (current.Count > 0)
			{
				current = ExpandOneStep(current);
				steps++;
				if (steps > 10000)
					break;
			}
			return steps;
		}

		public int ExpandToEmpty(List<int> sequence)
		{
			int steps = 0;
			var current = new List<int>(sequence);
			while (current.Count > 0)
			{
				current = ExpandOneStep(current);
				steps++;
				if (steps > 10000)
					break;
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

	// ==================== PrSS 引擎 ====================
	public class PrssEngine
	{
		public virtual string Expand(List<int> sequence, int steps)
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

		public bool IsEmpty(List<int> sequence) => sequence == null || sequence.Count == 0;
		public int GetValue(List<int> sequence) => sequence == null || sequence.Count == 0 ? 0 : CalculateValueRecursive(sequence);

		private int CalculateValueRecursive(List<int> seq)
		{
			if (seq.Count == 0) return 0;
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

	// ==================== BMS 解析器 ====================
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
	}

	// ==================== BMS 引擎 ====================
	public class BmsEngine
	{
		public string Expand(List<List<int>> matrix, int steps)
		{
			var current = matrix.Select(col => new List<int>(col)).ToList();
			for (int i = 0; i < steps && current.Count > 0; i++)
			{
				current = ExpandOneStep(current);
			}
			return BmsParser.Format(current);
		}

		private List<List<int>> ExpandOneStep(List<List<int>> matrix)
		{
			if (matrix == null || matrix.Count == 0)
				return new List<List<int>>();

			int rows = matrix[0].Count;
			int cols = matrix.Count;

			bool lastColAllZero = matrix[cols - 1].All(x => x == 0);
			if (lastColAllZero)
			{
				var result = matrix.Take(cols - 1).Select(col => new List<int>(col)).ToList();
				return result;
			}

			int badRoot = FindBadRoot(matrix);
			if (badRoot == -1)
				return new List<List<int>>();

			var goodPart = matrix.Take(badRoot).Select(col => new List<int>(col)).ToList();
			var badPart = matrix.Skip(badRoot).Take(cols - 1 - badRoot).Select(col => new List<int>(col)).ToList();

			var delta = ComputeDelta(matrix, badRoot);

			var expandedResult = new List<List<int>>();
			expandedResult.AddRange(goodPart);
			expandedResult.AddRange(badPart);

			var badCopy = badPart.Select(col => new List<int>(col)).ToList();
			for (int i = 0; i < badCopy.Count; i++)
			{
				for (int r = 0; r < rows; r++)
				{
					badCopy[i][r] += delta[r];
				}
			}
			expandedResult.AddRange(badCopy);

			return expandedResult;
		}

		private int FindBadRoot(List<List<int>> matrix)
		{
			int rows = matrix[0].Count;
			int cols = matrix.Count;

			for (int r = 0; r < rows; r++)
			{
				if (matrix[cols - 1][r] != 0)
				{
					int ak = matrix[cols - 1][r];
					for (int c = cols - 2; c >= 0; c--)
					{
						if (matrix[c][r] < ak)
							return c;
					}
				}
			}
			return -1;
		}

		private int[] ComputeDelta(List<List<int>> matrix, int badRoot)
		{
			int rows = matrix[0].Count;
			int cols = matrix.Count;
			int[] delta = new int[rows];

			for (int r = 0; r < rows; r++)
			{
				if (r == rows - 1)
					delta[r] = 0;
				else
				{
					int last = matrix[cols - 1][r];
					int root = matrix[badRoot][r];
					delta[r] = last - root;
				}
			}
			return delta;
		}

		public bool IsStandard(List<List<int>> matrix)
		{
			if (matrix == null || matrix.Count == 0)
				return true;

			int rows = matrix[0].Count;
			int cols = matrix.Count;

			if (matrix[0].Any(x => x != 0))
				return false;

			for (int c = 0; c < cols; c++)
			{
				for (int r = 1; r < rows; r++)
				{
					if (matrix[c][r] > matrix[c][r - 1])
						return false;
				}
			}

			for (int c = 0; c < cols; c++)
			{
				for (int r = 1; r < rows; r++)
				{
					if (matrix[c][r] > 0 && matrix[c][r] > matrix[c][r - 1] + 1)
						return false;
				}
			}

			return true;
		}
	}
}