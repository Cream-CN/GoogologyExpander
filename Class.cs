// Class.cs - 保留 PrSS，新增 BMS 展开
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

			// 规则 2: 如果最后一列全为 0，则删去最后一列
			bool lastColAllZero = matrix[cols - 1].All(x => x == 0);
			if (lastColAllZero)
			{
				var result = matrix.Take(cols - 1).Select(col => new List<int>(col)).ToList();
				return result;
			}

			// 规则 3: 找到坏根
			int badRoot = FindBadRoot(matrix);
			if (badRoot == -1)
				return new List<List<int>>();

			// 好部 [0, badRoot) ，坏部 [badRoot, cols-1)
			var goodPart = matrix.Take(badRoot).Select(col => new List<int>(col)).ToList();
			var badPart = matrix.Skip(badRoot).Take(cols - 1 - badRoot).Select(col => new List<int>(col)).ToList();

			// 计算阶差向量
			var delta = ComputeDelta(matrix, badRoot);

			// 修复：重命名结果变量避免冲突
			var expandedResult = new List<List<int>>();
			expandedResult.AddRange(goodPart);
			expandedResult.AddRange(badPart);

			// 复制坏部并加上 delta
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

			// 按照定义 13.4：坏根为最后一列中从上往下数的第一个非零项的父项所在的列
			for (int r = 0; r < rows; r++)
			{
				if (matrix[cols - 1][r] != 0)
				{
					int ak = matrix[cols - 1][r];
					// 从右往左找第一个小于 ak 的项
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

			// 阶差向量：未列和坏根的差值，最后一项始终为零
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

		// 验证 BMS 是否标准（可选功能）
		public bool IsStandard(List<List<int>> matrix)
		{
			if (matrix == null || matrix.Count == 0)
				return true;

			int rows = matrix[0].Count;
			int cols = matrix.Count;

			// 条件 (1): 首列所有元素都为零
			if (matrix[0].Any(x => x != 0))
				return false;

			// 条件 (2): 同列中下面的项不大于上面的项
			for (int c = 0; c < cols; c++)
			{
				for (int r = 1; r < rows; r++)
				{
					if (matrix[c][r] > matrix[c][r - 1])
						return false;
				}
			}

			// 条件 (3): 每一个非零项都至多为其父项 +1
			// 这里简化实现：检查每个非零项是否 <= 同一列上一项 + 1
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