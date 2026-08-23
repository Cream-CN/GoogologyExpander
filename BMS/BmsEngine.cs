// BMS/BmsEngine.cs - BMS 引擎 (完整实现，支持所有版本)
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogologyExpander
{
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

		public List<List<int>> ExpandOneStep(List<List<int>> matrix)
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
			}

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
}