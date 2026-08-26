// BSM/BsmEngine.cs
using System;
using System.Collections.Generic;
using System.Linq;
using GoogologyExpander.Helpers;

namespace GoogologyExpander
{
	public class BsmEngine
	{
		public string Expand(List<List<int>> matrix, int steps)
		{
			var current = MatrixHelper.DeepCopy(matrix);
			for (int i = 0; i < steps && current.Count > 0; i++)
			{
				current = ExpandOneStep(current);
			}
			return BmsParser.Format(current);
		}

		public BmsExpandResult ExpandWithDetails(List<List<int>> matrix, int steps)
		{
			var result = new BmsExpandResult();
			var current = MatrixHelper.DeepCopy(matrix);
			result.InitialMatrix = BmsParser.Format(matrix);
			result.Version = BMVersion.BM4; // 占位
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

		private List<List<int>> ExpandOneStep(List<List<int>> matrix)
		{
			if (matrix == null || matrix.Count == 0)
				return new List<List<int>>();

			int cols = matrix.Count;
			int rows = matrix[0].Count;

			// 末列全零
			if (matrix[cols - 1].All(v => v == 0))
			{
				return matrix.Take(cols - 1).Select(c => new List<int>(c)).ToList();
			}

			int[][] parents = MatrixHelper.ComputeParents(matrix);

			// 最下方非零行 t
			int t = -1;
			for (int y = rows - 1; y >= 0; y--)
			{
				if (matrix[cols - 1][y] > 0)
				{
					t = y;
					break;
				}
			}

			// 末列最后一个非零元素的父项
			int p_t_X = parents[t][cols - 1];

			// 获取除父项外的所有祖先（不含自身）
			var ancestors = MatrixHelper.GetAncestors(parents, cols - 1, t);
			ancestors.RemoveAll(a => a == p_t_X); // 去掉父项

			// 待定坏根 R：所有祖先的子项（且满足 t>1 时的祖先条件）
			var R = new List<int>();
			// 获取所有子项：对于每个祖先 a，找所有列 x 使得 parents[t][x] == a
			foreach (int a in ancestors)
			{
				for (int x = 0; x < cols - 1; x++) // 不包括末列
				{
					if (parents[t][x] == a)
					{
						// 检查 t>1 条件
						if (t > 1)
						{
							if (MatrixHelper.IsAncestor(parents, x, cols - 1, t - 1))
								R.Add(x);
						}
						else
						{
							R.Add(x);
						}
					}
				}
			}
			// 注意：第0列也被视为祖先，其子项可能是第一列？实际上，PDF中说 (表达式中不存在的) a0 的子项，即第0列元素作为子项？
			// 为了处理 a0 的子项，我们还需要考虑父项为 -1 的列（即无父项，属于第0列的子项）
			// 根据PDF，所有0的父项均在第0列，因此父项为 -1 的元素都应该算作第0列的子项。
			// 我们增加：对于父项为 -1 且满足条件的列也加入 R。
			for (int x = 0; x < cols - 1; x++)
			{
				if (parents[t][x] == -1)
				{
					// 必须满足祖先条件（如果 t>1）
					if (t > 1)
					{
						if (MatrixHelper.IsAncestor(parents, x, cols - 1, t - 1))
							R.Add(x);
					}
					else
					{
						R.Add(x);
					}
				}
			}
			// 去重
			R = R.Distinct().ToList();
			if (R.Count == 0)
				return new List<List<int>>();

			// 最右侧坏根 rMax = max(R)
			int rMax = R.Max();

			// 基准式
			var baseExpansion = ComputePreExpansion(matrix, parents, rMax, R, t);

			// 普通小根：预展开式 < 基准式
			var smallRoots = new List<int>();
			foreach (int r in R)
			{
				var pre = ComputePreExpansion(matrix, parents, r, R, t);
				if (MatrixHelper.CompareMatrices(pre, baseExpansion) < 0)
					smallRoots.Add(r);
			}

			// 强制小根：r 是 rMax 的祖先（严格），且 r 列与 rMax 列在 t+1 行以下不完全相同
			var forcedSmall = new List<int>();
			foreach (int r in R)
			{
				if (r == rMax) continue;
				if (MatrixHelper.IsAncestor(parents, r, rMax, t))
				{
					// 比较 t+1 到 末尾 的行
					bool same = true;
					for (int y = t + 1; y < rows; y++)
					{
						int valR = (r < cols) ? matrix[r][y] : 0;
						int valMax = matrix[rMax][y];
						if (valR != valMax)
						{
							same = false;
							break;
						}
					}
					if (!same)
						forcedSmall.Add(r);
				}
			}

			// 退出点 e = max({0} ∪ 普通小根 ∪ 强制小根)
			int e = 0;
			if (smallRoots.Count > 0) e = Math.Max(e, smallRoots.Max());
			if (forcedSmall.Count > 0) e = Math.Max(e, forcedSmall.Max());

			// 实际坏根 rb = min{r ∈ R | r > e}
			int rb = R.Where(r => r > e).Min();

			// 好部、坏部
			var G = matrix.Take(rb).Select(c => new List<int>(c)).ToList();
			var badPart = matrix.Skip(rb).Take(cols - 1 - rb).Select(c => new List<int>(c)).ToList();

			// 阶差向量（BSM 特殊处理：y==t 时额外减1）
			int[] delta = new int[rows];
			for (int y = 0; y < rows; y++)
			{
				if (y < t)
					delta[y] = matrix[cols - 1][y] - matrix[rb][y];
				else if (y == t)
					delta[y] = (matrix[cols - 1][y] - matrix[rb][y]) - 1;
				else
					delta[y] = 0;
			}

			// 提升矩阵（与BHM相同）
			var ascension = new int[badPart.Count, rows];
			for (int x = 0; x < badPart.Count; x++)
			{
				int colIdx = rb + x;
				for (int y = 0; y < rows; y++)
				{
					bool needAscend = false;
					foreach (int k in R)
					{
						if (k >= rb && MatrixHelper.IsAncestor(parents, k, colIdx, y))
						{
							needAscend = true;
							break;
						}
					}
					ascension[x, y] = needAscend ? 1 : 0;
				}
			}

			// 构建展开一步的结果：G + badPart^0 + badPart^1
			var result = new List<List<int>>(G);
			foreach (var col in badPart)
				result.Add(new List<int>(col));

			for (int x = 0; x < badPart.Count; x++)
			{
				var newCol = new List<int>();
				for (int y = 0; y < rows; y++)
				{
					int val = badPart[x][y] + 1 * delta[y] * ascension[x, y];
					newCol.Add(val);
				}
				result.Add(newCol);
			}

			return result;
		}

		/// <summary>
		/// 计算预展开式（与BHM相同，但Δ可能不同）
		/// </summary>
		private List<List<int>> ComputePreExpansion(List<List<int>> matrix, int[][] parents, int r, List<int> R, int t)
		{
			int cols = matrix.Count;
			int rows = matrix[0].Count;

			var G = matrix.Take(r).Select(c => new List<int>(c)).ToList();
			var badPart = matrix.Skip(r).Take(cols - 1 - r).Select(c => new List<int>(c)).ToList();

			// BSM 阶差向量
			int[] delta = new int[rows];
			for (int y = 0; y < rows; y++)
			{
				if (y < t)
					delta[y] = matrix[cols - 1][y] - matrix[r][y];
				else if (y == t)
					delta[y] = (matrix[cols - 1][y] - matrix[r][y]) - 1;
				else
					delta[y] = 0;
			}

			// 提升矩阵
			var ascension = new int[badPart.Count, rows];
			for (int x = 0; x < badPart.Count; x++)
			{
				int colIdx = r + x;
				for (int y = 0; y < rows; y++)
				{
					bool needAscend = false;
					foreach (int k in R)
					{
						if (k >= r && MatrixHelper.IsAncestor(parents, k, colIdx, y))
						{
							needAscend = true;
							break;
						}
					}
					ascension[x, y] = needAscend ? 1 : 0;
				}
			}

			// 构建预展开式
			var result = new List<List<int>>(G);
			foreach (var col in badPart)
				result.Add(new List<int>(col));

			for (int x = 0; x < badPart.Count; x++)
			{
				var newCol = new List<int>();
				for (int y = 0; y < rows; y++)
				{
					int val = badPart[x][y] + 1 * delta[y] * ascension[x, y];
					newCol.Add(val);
				}
				result.Add(newCol);
			}

			// (末列+Δ)
			var lastCol = new List<int>();
			for (int y = 0; y < rows; y++)
			{
				lastCol.Add(matrix[cols - 1][y] + delta[y]);
			}
			result.Add(lastCol);

			return result;
		}
	}
}