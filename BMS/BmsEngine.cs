// BmsEngine.cs - 完全按照PDF第13章定义实现
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogologyExpander
{
	public class BmsEngine
	{
		public BmsEngine()
		{
		}

		public BMVersion GetVersion()
		{
			return BMVersion.BM4;
		}

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
			result.Version = BMVersion.BM4;
			result.Steps = new List<string>();
			result.StepMatrices = new List<string>();

			result.Steps.Add($"初始: {BmsParser.Format(current)}");
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
			// Rule: (∅) = 0
			if (matrix == null || matrix.Count == 0)
				return new List<List<int>>();

			int cols = matrix.Count;
			int rows = matrix[0].Count;

			// 检查最后一列是否全为零
			// Rule: S = S0 S1 ... S(X-2) + 1 (if ∀y S(X-1)y = 0)
			bool lastColAllZero = matrix[cols - 1].All(x => x == 0);
			if (lastColAllZero)
			{
				var result = matrix.Take(cols - 1).Select(col => new List<int>(col)).ToList();
				return result;
			}

			// 找到最下方的非零项 t
			// t = max{y | S(X-1)y > 0}
			int t = -1;
			for (int r = rows - 1; r >= 0; r--)
			{
				if (matrix[cols - 1][r] > 0)
				{
					t = r;
					break;
				}
			}

			// 坏根 r = P_t(X - 1)
			int badRoot = FindParentDirect(matrix, cols - 1, t);

			// 好部 G = S0 S1 ... S(r-1)
			var goodPart = matrix.Take(badRoot).Select(col => new List<int>(col)).ToList();

			// 阶差向量 Δ
			// Δ_y = S(X-1)y - S_ry (if y < t), 0 (if y ≥ t)
			int[] delta = new int[rows];
			for (int r = 0; r < rows; r++)
			{
				if (r < t)
					delta[r] = matrix[cols - 1][r] - matrix[badRoot][r];
				else
					delta[r] = 0;
			}

			// 构建坏部（不包括末列）
			var badPart = new List<List<int>>();
			for (int x = badRoot; x < cols - 1; x++)
			{
				badPart.Add(new List<int>(matrix[x]));
			}

			// 计算提升矩阵 A
			// A_xy = 1 如果 ∃a (r = (P_y)^a(r + x))
			int[,] ascentionMatrix = new int[badPart.Count, rows];
			for (int x = 0; x < badPart.Count; x++)
			{
				for (int y = 0; y < rows; y++)
				{
					int colIndex = badRoot + x;
					ascentionMatrix[x, y] = IsAncestorOf(matrix, colIndex, y, badRoot) ? 1 : 0;
				}
			}

			// 构建结果：好部 + 坏部（复制一次，a=1）
			var expandedResult = new List<List<int>>();
			expandedResult.AddRange(goodPart);

			// 复制坏部一次 (a=1)
			for (int x = 0; x < badPart.Count; x++)
			{
				var newCol = new List<int>();
				for (int y = 0; y < rows; y++)
				{
					int val = badPart[x][y] + 1 * delta[y] * ascentionMatrix[x, y];
					newCol.Add(val);
				}
				expandedResult.Add(newCol);
			}

			return expandedResult;
		}

		// 直接查找父项（不调用IsAncestor，避免循环）
		private int FindParentDirect(List<List<int>> matrix, int x, int y)
		{
			if (y == 0)
			{
				// 第一行：左边第一个小于该元素的项
				for (int p = x - 1; p >= 0; p--)
				{
					if (matrix[p][y] < matrix[x][y])
						return p;
				}
				return -1;
			}
			else
			{
				// 其余行：左边小于该元素，且其正上方的项是该元素正上方的项的祖先项
				for (int p = x - 1; p >= 0; p--)
				{
					if (matrix[p][y] < matrix[x][y])
					{
						// 检查 p 是否为 (P_(y-1))^a(x) 的某个值
						if (IsAncestorOf(matrix, x, y - 1, p))
							return p;
					}
				}
				return -1;
			}
		}

		// 判断 ancestor 是否为 element 的祖先项
		// 即是否存在 a 使得 ancestor = (P_y)^a(element)
		private bool IsAncestorOf(List<List<int>> matrix, int element, int y, int ancestor)
		{
			int current = element;
			while (current >= 0)
			{
				int parent = FindParentDirect(matrix, current, y);
				if (parent == ancestor)
					return true;
				if (parent < 0 || parent >= current)
					break;
				current = parent;
			}
			return false;
		}

		// ==================== 辅助方法 ====================
		public bool IsEmpty(List<List<int>> matrix)
		{
			return matrix == null || matrix.Count == 0;
		}

		// 检查BMS是否标准
		// 条件：(1) 首列全为零 (2) 同列中下面的项不大于上面的项 (3) 每个非零项至多为其父项+1
		public bool IsStandard(List<List<int>> matrix)
		{
			if (matrix == null || matrix.Count == 0)
				return true;

			int rows = matrix[0].Count;
			int cols = matrix.Count;

			// 条件1：首列全为零
			if (matrix[0].Any(x => x != 0))
				return false;

			// 条件2：同列中下面的项不大于上面的项
			for (int c = 0; c < cols; c++)
			{
				for (int r = 1; r < rows; r++)
				{
					if (matrix[c][r] > matrix[c][r - 1])
						return false;
				}
			}

			// 条件3：每个非零项至多为其父项+1
			for (int c = 0; c < cols; c++)
			{
				for (int r = 0; r < rows; r++)
				{
					if (matrix[c][r] > 0)
					{
						int parent = FindParentDirect(matrix, c, r);
						if (parent >= 0)
						{
							if (matrix[c][r] > matrix[parent][r] + 1)
								return false;
						}
						else
						{
							// 没有父项的非零项（只可能是0行以外的元素）
							if (r > 0 && matrix[c][r] > 0)
								return false;
						}
					}
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