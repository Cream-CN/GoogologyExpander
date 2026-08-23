// BHM/BhmEngine.cs
using System;
using System.Collections.Generic;
using System.Linq;
using GoogologyExpander.Helpers;

namespace GoogologyExpander
{
	public class BhmEngine
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
			result.Version = BMVersion.BM4; // 占位，实际是BHM
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

			// 末列全零 -> 删末列 (+1)
			if (matrix[cols - 1].All(v => v == 0))
			{
				return matrix.Take(cols - 1).Select(c => new List<int>(c)).ToList();
			}

			// 计算父项
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

			// 待定坏根集合 R
			var R = new List<int>();
			int p_t_X = parents[t][cols - 1]; // P_t(X)
			int p_t_p_t_X = (p_t_X >= 0) ? parents[t][p_t_X] : -1; // P_t(P_t(X))

			for (int r = 0; r < cols - 1; r++)
			{
				if (parents[t][r] == p_t_p_t_X)
				{
					if (t > 1)
					{
						// 需要 r 是 X 的祖先（在行 t-1）
						if (MatrixHelper.IsAncestor(parents, r, cols - 1, t - 1))
							R.Add(r);
					}
					else
					{
						R.Add(r);
					}
				}
			}

			if (R.Count == 0)
			{
				// 没有坏根，通常不会发生，按空处理
				return new List<List<int>>();
			}

			// 基准式：r = max(R)
			int rMax = R.Max();
			var baseExpansion = ComputePreExpansion(matrix, parents, rMax, R, t);

			// 小根：预展开式 < 基准式
			var smallRoots = new List<int>();
			foreach (int r in R)
			{
				var pre = ComputePreExpansion(matrix, parents, r, R, t);
				if (MatrixHelper.CompareMatrices(pre, baseExpansion) < 0)
					smallRoots.Add(r);
			}

			// 退出点 e = max({0} ∪ smallRoots)
			int e = smallRoots.Count > 0 ? smallRoots.Max() : 0;

			// 实际坏根 rb = min{r ∈ R | r > e}
			int rb = R.Where(r => r > e).Min();

			// 好部、坏部、阶差向量
			var G = matrix.Take(rb).Select(c => new List<int>(c)).ToList();
			var badPart = matrix.Skip(rb).Take(cols - 1 - rb).Select(c => new List<int>(c)).ToList();

			// 阶差向量
			int[] delta = new int[rows];
			for (int y = 0; y < rows; y++)
			{
				if (y < t)
					delta[y] = matrix[cols - 1][y] - matrix[rb][y];
				else
					delta[y] = 0;
			}

			// 提升矩阵
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
			// 坏部^0 (不提升)
			foreach (var col in badPart)
				result.Add(new List<int>(col));

			// 坏部^1 (提升一次)
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
		/// 计算待定坏根 r 的预展开式：G(r) + B(r)^0 + B(r)^1 + (末列+Δ(r))
		/// </summary>
		private List<List<int>> ComputePreExpansion(List<List<int>> matrix, int[][] parents, int r, List<int> R, int t)
		{
			int cols = matrix.Count;
			int rows = matrix[0].Count;

			var G = matrix.Take(r).Select(c => new List<int>(c)).ToList();
			var badPart = matrix.Skip(r).Take(cols - 1 - r).Select(c => new List<int>(c)).ToList();

			// 阶差向量
			int[] delta = new int[rows];
			for (int y = 0; y < rows; y++)
			{
				if (y < t)
					delta[y] = matrix[cols - 1][y] - matrix[r][y];
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

			// 构建预展开式：G + badPart^0 + badPart^1 + (末列+Δ)
			var result = new List<List<int>>(G);
			// badPart^0
			foreach (var col in badPart)
				result.Add(new List<int>(col));

			// badPart^1
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