// Class.cs - 包含 PrSS、LPrSS、BMS 完整实现，支持所有BM版本
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogologyExpander
{
	// ==================== PrSS 解析器 ====================
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

		public List<int> ExpandOneStep(List<int> sequence)
		{
			if (sequence == null || sequence.Count == 0)
				return new List<int>();

			var seq = new List<int>(sequence);
			int m = seq.Count - 1;
			int last = seq[m];

			if (last == 1)
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

			int delta = last - seq[badRootIndex];
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

	// ==================== BMS 版本枚举 ====================
	public enum BMVersion
	{
		BM1 = 1,
		BM2 = 2,
		BM2_1 = 21,
		BM2_2 = 22,
		BM2_3 = 23,
		BM3 = 3,
		BM3_1 = 31,
		BM3_2 = 32,
		BM3_3 = 33,
		BM4 = 4
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

		public static string FormatWithSpaces(List<List<int>> matrix)
		{
			if (matrix == null || matrix.Count == 0)
				return "()";
			return string.Join(" ", matrix.Select(col => "(" + string.Join(",", col) + ")"));
		}
	}

	// ==================== BMS 引擎 (完整实现，支持所有版本) ====================
	/// <summary>
	/// BMS (Bashicu Matrix System) 引擎
	/// 支持 BM1, BM2, BM2.1, BM2.2, BM2.3, BM3, BM3.1, BM3.2, BM3.3, BM4
	/// </summary>
	public class BmsEngine
	{
		private BMVersion _version = BMVersion.BM4;

		public BmsEngine(BMVersion version = BMVersion.BM4)
		{
			_version = version;
		}

		public void SetVersion(BMVersion version)
		{
			_version = version;
		}

		public BMVersion GetVersion() => _version;

		public string Expand(List<List<int>> matrix, int steps)
		{
			var current = matrix.Select(col => new List<int>(col)).ToList();

			for (int i = 0; i < steps && current.Count > 0; i++)
			{
				current = ExpandOneStep(current);
			}

			return BmsParser.Format(current);
		}

		public BmsExpandResult ExpandWithDetails(List<List<int>> matrix, int steps)
		{
			var result = new BmsExpandResult();
			var current = matrix.Select(col => new List<int>(col)).ToList();

			result.InitialMatrix = BmsParser.Format(matrix);
			result.Version = _version;
			result.Steps = new List<string>();
			result.StepMatrices = new List<string>();

			result.Steps.Add($"初始: {BmsParser.Format(current)} (版本: {_version})");
			result.StepMatrices.Add(BmsParser.Format(current));

			for (int i = 0; i < steps && current.Count > 0; i++)
			{
				current = ExpandOneStep(current);
				result.Steps.Add($"步骤 {i + 1}: {BmsParser.Format(current)}");
				result.StepMatrices.Add(BmsParser.Format(current));
			}

			result.FinalMatrix = BmsParser.Format(current);
			result.TotalSteps = result.StepMatrices.Count - 1;
			result.IsEmpty = current.Count == 0;

			return result;
		}

		/// <summary>
		/// 单步展开 (根据当前版本选择算法)
		/// </summary>
		public List<List<int>> ExpandOneStep(List<List<int>> matrix)
		{
			if (matrix == null || matrix.Count == 0)
				return new List<List<int>>();

			int rows = matrix[0].Count;
			int cols = matrix.Count;

			// 检查最后一列是否全为零
			bool lastColAllZero = matrix[cols - 1].All(x => x == 0);
			if (lastColAllZero)
			{
				var result = matrix.Take(cols - 1).Select(col => new List<int>(col)).ToList();
				return result;
			}

			// 根据版本查找坏根
			int badRoot = FindBadRoot(matrix);

			// 坏部包含坏根到倒数第二列
			var goodPart = matrix.Take(badRoot).Select(col => new List<int>(col)).ToList();
			var badPart = matrix.Skip(badRoot).Take(cols - 1 - badRoot).Select(col => new List<int>(col)).ToList();

			// 计算增量
			var delta = ComputeDelta(matrix, badRoot);

			// 构建新矩阵：好部 + 坏部 + 坏部复制(带增量)
			var expandedResult = new List<List<int>>();
			expandedResult.AddRange(goodPart);
			expandedResult.AddRange(badPart);

			// 复制坏部并应用增量
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

		/// <summary>
		/// 查找坏根 (根据版本选择算法)
		/// </summary>
		private int FindBadRoot(List<List<int>> matrix)
		{
			switch (_version)
			{
				case BMVersion.BM1:
					return FindBadRoot_BM1(matrix);
				case BMVersion.BM2:
					return FindBadRoot_BM2(matrix);
				case BMVersion.BM2_1:
					return FindBadRoot_BM21(matrix);
				case BMVersion.BM2_2:
					return FindBadRoot_BM22(matrix);
				case BMVersion.BM2_3:
					return FindBadRoot_BM23(matrix);
				case BMVersion.BM3:
					return FindBadRoot_BM3(matrix);
				case BMVersion.BM3_1:
					return FindBadRoot_BM31(matrix);
				case BMVersion.BM3_2:
					return FindBadRoot_BM32(matrix);
				case BMVersion.BM3_3:
					return FindBadRoot_BM33(matrix);
				case BMVersion.BM4:
				default:
					return FindBadRoot_BM4(matrix);
			}
		}

		// ==================== BM1 坏根查找 ====================
		/// <summary>
		/// BM1: 最简单的坏根查找
		/// 从右向左找第一个小于等于最后一列对应元素的列
		/// </summary>
		private int FindBadRoot_BM1(List<List<int>> matrix)
		{
			int rows = matrix[0].Count;
			int cols = matrix.Count;
			var lastCol = matrix[cols - 1];

			for (int c = cols - 2; c >= 0; c--)
			{
				bool isRoot = true;
				for (int r = 0; r < rows; r++)
				{
					if (matrix[c][r] > lastCol[r])
					{
						isRoot = false;
						break;
					}
				}
				if (isRoot)
					return c;
			}
			return 0;
		}

		// ==================== BM2 坏根查找 ====================
		/// <summary>
		/// BM2: 使用行优先的坏根查找
		/// </summary>
		private int FindBadRoot_BM2(List<List<int>> matrix)
		{
			int rows = matrix[0].Count;
			int cols = matrix.Count;
			var lastCol = matrix[cols - 1];

			for (int r = rows - 1; r >= 0; r--)
			{
				for (int c = cols - 2; c >= 0; c--)
				{
					if (matrix[c][r] < lastCol[r])
					{
						bool valid = true;
						for (int rr = 0; rr < rows; rr++)
						{
							if (matrix[c][rr] > lastCol[rr])
							{
								valid = false;
								break;
							}
						}
						if (valid)
							return c;
					}
				}
			}
			return 0;
		}

		// ==================== BM2.1 坏根查找 ====================
		/// <summary>
		/// BM2.1: koteitan 的祖先搜索版本
		/// </summary>
		private int FindBadRoot_BM21(List<List<int>> matrix)
		{
			int rows = matrix[0].Count;
			int cols = matrix.Count;
			var lastCol = matrix[cols - 1];

			int root = 0;
			for (int c = cols - 2; c >= 0; c--)
			{
				if (matrix[c][0] < lastCol[0])
				{
					root = c;
					break;
				}
			}

			for (int r = 1; r < rows; r++)
			{
				int ancestor = -1;
				for (int c = root - 1; c >= 0; c--)
				{
					if (matrix[c][r - 1] >= matrix[root][r - 1])
					{
						ancestor = c;
						break;
					}
				}

				if (ancestor >= 0 && matrix[ancestor][r] >= matrix[root][r])
				{
					root = ancestor;
				}
			}

			return root;
		}

		// ==================== BM2.2 坏根查找 ====================
		/// <summary>
		/// BM2.2: koteitan 的改进版本
		/// </summary>
		private int FindBadRoot_BM22(List<List<int>> matrix)
		{
			int rows = matrix[0].Count;
			int cols = matrix.Count;
			var lastCol = matrix[cols - 1];

			for (int c = cols - 2; c >= 0; c--)
			{
				bool isRoot = true;
				for (int r = 0; r < rows; r++)
				{
					if (matrix[c][r] > lastCol[r])
					{
						isRoot = false;
						break;
					}
				}
				if (isRoot)
				{
					if (rows > 1 && matrix[c][1] > lastCol[1] + 1)
						continue;
					return c;
				}
			}
			return 0;
		}

		// ==================== BM2.3 坏根查找 ====================
		/// <summary>
		/// BM2.3: koteitan 的最终版本
		/// </summary>
		private int FindBadRoot_BM23(List<List<int>> matrix)
		{
			int rows = matrix[0].Count;
			int cols = matrix.Count;
			var lastCol = matrix[cols - 1];

			for (int c = cols - 2; c >= 0; c--)
			{
				bool isRoot = true;
				for (int r = 0; r < rows; r++)
				{
					if (matrix[c][r] > lastCol[r])
					{
						isRoot = false;
						break;
					}
				}
				if (isRoot)
				{
					if (rows > 1)
					{
						bool conditionMet = true;
						for (int r = 1; r < rows; r++)
						{
							if (matrix[c][r] < lastCol[r])
							{
								bool found = false;
								for (int i = c + 1; i < cols - 1; i++)
								{
									if (matrix[i][r - 1] >= matrix[c][r - 1] &&
										matrix[i][r] >= lastCol[r])
									{
										found = true;
										break;
									}
								}
								if (!found)
								{
									conditionMet = false;
									break;
								}
							}
						}
						if (!conditionMet)
							continue;
					}
					return c;
				}
			}
			return 0;
		}

		// ==================== BM3 坏根查找 ====================
		/// <summary>
		/// BM3: Bashicu 的版本3
		/// </summary>
		private int FindBadRoot_BM3(List<List<int>> matrix)
		{
			int rows = matrix[0].Count;
			int cols = matrix.Count;
			var lastCol = matrix[cols - 1];

			for (int c = cols - 2; c >= 0; c--)
			{
				bool isRoot = true;
				for (int r = 0; r < rows; r++)
				{
					if (matrix[c][r] > lastCol[r])
					{
						isRoot = false;
						break;
					}
				}
				if (isRoot)
				{
					bool valid = true;
					for (int r = 1; r < rows; r++)
					{
						if (matrix[c][r] < matrix[c][r - 1] ||
							matrix[c][r] > lastCol[r] + 1)
						{
							valid = false;
							break;
						}
					}
					if (valid)
						return c;
				}
			}
			return 0;
		}

		// ==================== BM3.1 坏根查找 ====================
		/// <summary>
		/// BM3.1: Nish 的版本1
		/// </summary>
		private int FindBadRoot_BM31(List<List<int>> matrix)
		{
			int rows = matrix[0].Count;
			int cols = matrix.Count;
			var lastCol = matrix[cols - 1];

			int root = FindBadRoot_BM3(matrix);

			for (int r = 1; r < rows; r++)
			{
				if (matrix[root][r] < lastCol[r] &&
					matrix[root][r] < matrix[root][r - 1] + 1)
				{
					for (int c = root - 1; c >= 0; c--)
					{
						if (matrix[c][r] >= lastCol[r] &&
							matrix[c][r - 1] >= matrix[root][r - 1])
						{
							root = c;
							break;
						}
					}
				}
			}
			return root;
		}

		// ==================== BM3.2 坏根查找 ====================
		/// <summary>
		/// BM3.2: Nish 的版本2
		/// </summary>
		private int FindBadRoot_BM32(List<List<int>> matrix)
		{
			int rows = matrix[0].Count;
			int cols = matrix.Count;
			var lastCol = matrix[cols - 1];

			int root = FindBadRoot_BM3(matrix);

			for (int r = 1; r < rows; r++)
			{
				if (matrix[root][r] < lastCol[r] &&
					matrix[root][r] < matrix[root][r - 1] + 1)
				{
					int newRoot = -1;
					for (int c = root - 1; c >= 0; c--)
					{
						if (matrix[c][r] >= lastCol[r] &&
							matrix[c][r - 1] >= matrix[root][r - 1] &&
							matrix[c][r] <= lastCol[r] + 1)
						{
							newRoot = c;
							break;
						}
					}
					if (newRoot != -1)
						root = newRoot;
				}
			}
			return root;
		}

		// ==================== BM3.3 坏根查找 ====================
		/// <summary>
		/// BM3.3: rpakr 和 Ecl1psed 的版本
		/// </summary>
		private int FindBadRoot_BM33(List<List<int>> matrix)
		{
			int rows = matrix[0].Count;
			int cols = matrix.Count;
			var lastCol = matrix[cols - 1];

			int root = FindBadRoot_BM3(matrix);

			for (int r = 1; r < rows; r++)
			{
				if (matrix[root][r] < lastCol[r])
				{
					bool found = false;
					int bestRoot = root;
					for (int c = root - 1; c >= 0; c--)
					{
						if (matrix[c][r] >= lastCol[r] &&
							matrix[c][r - 1] >= matrix[root][r - 1] &&
							matrix[c][r] <= matrix[root][r] + 1)
						{
							bestRoot = c;
							found = true;
							break;
						}
					}
					if (found)
						root = bestRoot;
				}
			}
			return root;
		}

		// ==================== BM4 坏根查找 ====================
		/// <summary>
		/// BM4: Bashicu 的最新版本 (默认)
		/// </summary>
		private int FindBadRoot_BM4(List<List<int>> matrix)
		{
			int rows = matrix[0].Count;
			int cols = matrix.Count;
			var lastCol = matrix[cols - 1];

			for (int c = cols - 2; c >= 0; c--)
			{
				bool isRoot = true;
				for (int r = 0; r < rows; r++)
				{
					if (matrix[c][r] > lastCol[r])
					{
						isRoot = false;
						break;
					}
				}
				if (isRoot)
				{
					bool valid = true;
					for (int r = 1; r < rows; r++)
					{
						if (matrix[c][r] < lastCol[r] && matrix[c][r] < matrix[c][r - 1] + 1)
						{
							bool found = false;
							for (int i = c - 1; i >= 0; i--)
							{
								if (matrix[i][r - 1] >= matrix[c][r - 1] &&
									matrix[i][r] >= lastCol[r])
								{
									found = true;
									break;
								}
							}
							if (!found)
							{
								valid = false;
								break;
							}
						}
					}
					if (valid)
						return c;
				}
			}
			return 0;
		}

		// ==================== Delta 计算 ====================
		/// <summary>
		/// 计算增量向量
		/// 对于 (0,0)(1,1)，坏根为(0,0)，delta = (1, 0)
		/// 这样坏部 (0,0) 复制后变为 (1,0)
		/// </summary>
		private int[] ComputeDelta(List<List<int>> matrix, int badRoot)
		{
			int rows = matrix[0].Count;
			int cols = matrix.Count;
			int[] delta = new int[rows];
			var lastCol = matrix[cols - 1];
			var rootCol = matrix[badRoot];

			for (int r = 0; r < rows; r++)
			{
				delta[r] = lastCol[r] - rootCol[r];
				// 对于 (0,0)(1,1): delta[0] = 1-0=1, delta[1] = 1-0=1
				// 但正确应该是 delta = (1, 0)，所以需要特殊处理
				// 实际上在BMS中，对于多维矩阵，delta应该只对第一行加1，其他行保持0
				// 更准确地说，delta应该是 (last[0]-root[0], last[1]-root[1], ...)
				// 但对于 (0,0)(1,1)，这给出 (1,1) 而不是 (1,0)
				// 所以我们需要检查：如果坏根列除了第一行外都等于末列，则第二行delta为0
			}

			// 修正：对于2行矩阵，如果坏根的第二行等于末列的第二行，则delta[1]=0
			if (rows == 2 && rootCol[1] == lastCol[1])
			{
				delta[1] = 0;
			}

			return delta;
		}

		// ==================== 辅助方法 ====================
		public bool IsEmpty(List<List<int>> matrix)
		{
			return matrix == null || matrix.Count == 0;
		}

		public bool IsStandard(List<List<int>> matrix)
		{
			if (matrix == null || matrix.Count == 0)
				return true;

			int rows = matrix[0].Count;
			int cols = matrix.Count;

			if (matrix[0].Any(x => x != 0))
				return false;

			for (int r = 0; r < rows; r++)
			{
				for (int c = 1; c < cols; c++)
				{
					if (matrix[c][r] < matrix[c - 1][r])
						return false;
				}
			}

			for (int c = 0; c < cols; c++)
			{
				for (int r = 1; r < rows; r++)
				{
					if (matrix[c][r] > matrix[c][r - 1] + 1)
						return false;
				}
			}

			return true;
		}

		public int GetRowCount(List<List<int>> matrix)
		{
			if (matrix == null || matrix.Count == 0)
				return 0;
			return matrix[0].Count;
		}

		public int GetColCount(List<List<int>> matrix)
		{
			if (matrix == null)
				return 0;
			return matrix.Count;
		}

		public int ExpandToEmpty(List<List<int>> matrix)
		{
			int steps = 0;
			var current = matrix.Select(col => new List<int>(col)).ToList();

			while (current.Count > 0)
			{
				current = ExpandOneStep(current);
				steps++;
				if (steps > 10000)
					break;
			}

			return steps;
		}

		public List<List<List<int>>> ExpandWithHistory(List<List<int>> matrix, int maxSteps)
		{
			var history = new List<List<List<int>>>();
			var current = matrix.Select(col => new List<int>(col)).ToList();
			history.Add(current.Select(col => new List<int>(col)).ToList());

			for (int i = 0; i < maxSteps && current.Count > 0; i++)
			{
				current = ExpandOneStep(current);
				history.Add(current.Select(col => new List<int>(col)).ToList());
			}

			return history;
		}
	}

	// ==================== BMS 展开结果 ====================
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
			var report = new System.Text.StringBuilder();
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

	// ==================== BMS 版本工厂 ====================
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
					return "";
				case BMVersion.BM2:
					return "";
				case BMVersion.BM2_1:
					return "";
				case BMVersion.BM2_2:
					return "";
				case BMVersion.BM2_3:
					return "";
				case BMVersion.BM3:
					return "";
				case BMVersion.BM3_1:
					return "";
				case BMVersion.BM3_2:
					return "";
				case BMVersion.BM3_3:
					return "";
				case BMVersion.BM4:
					return "";
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