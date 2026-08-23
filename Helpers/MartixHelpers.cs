// Helpers/MatrixHelper.cs
// HM/SM的公共库
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogologyExpander.Helpers
{
	public static class MatrixHelper
	{
		/// <summary>
		/// 计算所有元素的父项列索引（-1 表示第 0 列）
		/// parents[y][x] = 第 y 行第 x 列的父项列索引
		/// </summary>
		public static int[][] ComputeParents(List<List<int>> matrix)
		{
			int cols = matrix.Count;
			int rows = matrix[0].Count;
			var parents = new int[rows][];
			for (int y = 0; y < rows; y++)
				parents[y] = new int[cols];

			for (int y = 0; y < rows; y++)
			{
				for (int x = 0; x < cols; x++)
				{
					parents[y][x] = FindParent(matrix, x, y, parents);
				}
			}
			return parents;
		}

		private static int FindParent(List<List<int>> matrix, int x, int y, int[][] parents)
		{
			int rows = matrix[0].Count;
			if (y == 0)
			{
				// 第一行：左边第一个小于该元素
				for (int p = x - 1; p >= 0; p--)
				{
					if (matrix[p][y] < matrix[x][y])
						return p;
				}
				return -1; // 第 0 列
			}
			else
			{
				// 其他行：左边小于该元素，且其正上方的项是当前元素正上方项的祖先
				for (int p = x - 1; p >= 0; p--)
				{
					if (matrix[p][y] < matrix[x][y])
					{
						if (IsAncestor(parents, p, x, y - 1))
							return p;
					}
				}
				return -1; // 第 0 列
			}
		}

		/// <summary>
		/// 判断 p 是否为 x 在 y 行的祖先（严格祖先，不包括自身）
		/// </summary>
		public static bool IsAncestor(int[][] parents, int p, int x, int y)
		{
			int current = x;
			while (current >= 0)
			{
				int parent = parents[y][current];
				if (parent == p) return true;
				if (parent < 0 || parent >= current) break;
				current = parent;
			}
			return false;
		}

		/// <summary>
		/// 判断 p 是否为 x 在 y 行的祖先（包括自身）
		/// </summary>
		public static bool IsAncestorOrSelf(int[][] parents, int p, int x, int y)
		{
			if (p == x) return true;
			return IsAncestor(parents, p, x, y);
		}

		/// <summary>
		/// 获取 x 在 y 行的所有祖先（不含自身），按从近到远顺序（即 parent, parent's parent, ...）
		/// </summary>
		public static List<int> GetAncestors(int[][] parents, int x, int y)
		{
			var result = new List<int>();
			int current = x;
			while (current >= 0)
			{
				int parent = parents[y][current];
				if (parent < 0) break;
				result.Add(parent);
				current = parent;
			}
			return result;
		}

		/// <summary>
		/// 字典序比较两个矩阵（按列展开，每列内按行从上到下）
		/// 返回 -1: a<b, 0: a==b, 1: a>b
		/// </summary>
		public static int CompareMatrices(List<List<int>> a, List<List<int>> b)
		{
			int colsA = a.Count, colsB = b.Count;
			int rowsA = a.Count == 0 ? 0 : a[0].Count;
			int rowsB = b.Count == 0 ? 0 : b[0].Count;
			int rows = Math.Max(rowsA, rowsB);
			int cols = Math.Max(colsA, colsB);

			for (int x = 0; x < cols; x++)
			{
				for (int y = 0; y < rows; y++)
				{
					int va = (x < colsA && y < rowsA) ? a[x][y] : 0;
					int vb = (x < colsB && y < rowsB) ? b[x][y] : 0;
					if (va < vb) return -1;
					if (va > vb) return 1;
				}
			}
			return 0;
		}

		/// <summary>
		/// 获取矩阵的深拷贝
		/// </summary>
		public static List<List<int>> DeepCopy(List<List<int>> matrix)
		{
			return matrix.Select(col => new List<int>(col)).ToList();
		}
	}
}