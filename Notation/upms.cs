using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogologyExpander
{
	public static class UPMS
	{
		private const bool STRICT_BASE_COLUMN = true;

		#region Helper Functions

		private static bool IsPseudoInfinity(object expr) => expr?.ToString() == "Infinity";

		private static List<int> CloneColumn(List<int> col) => col.ToList();

		private static List<List<int>> CloneMatrix(List<List<int>> matrix) => matrix.Select(col => col.ToList()).ToList();

		private static bool IsNatural(int value) => value >= 0 && value <= int.MaxValue;

		private static List<List<int>> StandardizeMatrix(List<List<int>> matrix)
		{
			if (matrix == null || matrix.Count == 0) return new List<List<int>>();

			int rows = 1;
			foreach (var col in matrix)
			{
				if (col == null) return new List<List<int>>();
				rows = Math.Max(rows, col.Count);
			}

			var result = new List<List<int>>();
			foreach (var col in matrix)
			{
				var outCol = col.ToList();
				while (outCol.Count < rows) outCol.Add(0);
				result.Add(outCol);
			}

			while (rows > 1 && result.All(col => col[rows - 1] == 0))
			{
				foreach (var col in result) col.RemoveAt(rows - 1);
				rows--;
			}

			return result;
		}

		private static bool IsLegalUPMSMatrix(List<List<int>> matrix)
		{
			if (IsPseudoInfinity(matrix)) return true;
			if (matrix == null) return false;
			if (matrix.Count == 0) return true;

			foreach (var col in matrix)
			{
				if (col == null) return false;
				foreach (var value in col)
				{
					if (!IsNatural(value)) return false;
				}
			}

			var m = StandardizeMatrix(matrix);
			if (m.Count == 0) return true;
			int rows = m[0].Count;

			for (int r = 0; r < rows; r++)
				if (m[0][r] != 0) return false;

			for (int c = 0; c < m.Count; c++)
			{
				var col = m[c];
				for (int r = 1; r < rows; r++)
					if (col[r] > col[r - 1]) return false;
			}

			return true;
		}

		private static int SequenceCompare(List<int> seq1, List<int> seq2)
		{
			int len = Math.Max(seq1.Count, seq2.Count);
			for (int i = 0; i < len; i++)
			{
				int a = i < seq1.Count ? seq1[i] : 0;
				int b = i < seq2.Count ? seq2[i] : 0;
				if (a < b) return -1;
				if (a > b) return 1;
			}
			return 0;
		}

		private static int MatrixCompare(List<List<int>> m1, List<List<int>> m2)
		{
			if (m1.Count != m2.Count) return m1.Count.CompareTo(m2.Count);
			for (int i = 0; i < m1.Count; i++)
			{
				int cmp = SequenceCompare(m1[i], m2[i]);
				if (cmp != 0) return cmp;
			}
			return 0;
		}

		#endregion

		#region Context

		public class AncestorInfo
		{
			public List<int> List { get; set; }
			public byte[] Mask { get; set; }
		}

		private class Context
		{
			public List<List<int>> M { get; }
			public int ColCount { get; }
			public int RowCount { get; }

			// parentCache[b][col] : 父列索引，-2未知，-1无父
			private readonly int[][] _parentCache;
			// ancestorCache[a][col] : AncestorInfo 对象
			private readonly AncestorInfo[][] _ancestorCache;

			public Context(List<List<int>> matrix)
			{
				M = StandardizeMatrix(matrix);
				ColCount = M.Count;
				RowCount = ColCount == 0 ? 0 : M[0].Count;

				// 初始化 parentCache : (RowCount+1) x ColCount，全部 -2
				_parentCache = new int[RowCount + 1][];
				for (int i = 0; i <= RowCount; i++)
				{
					_parentCache[i] = Enumerable.Repeat(-2, ColCount).ToArray();
				}

				// 初始化 ancestorCache : (RowCount+1) x ColCount，全部 null
				_ancestorCache = new AncestorInfo[RowCount + 1][];
				for (int i = 0; i <= RowCount; i++)
				{
					_ancestorCache[i] = new AncestorInfo[ColCount];
				}
			}

			private int GetZeroParent(int colIndex) => colIndex > 0 ? colIndex - 1 : -1;

			public AncestorInfo GetAAncestors(int colIndex, int a)
			{
				if (a < 0 || a > RowCount || colIndex < 0 || colIndex >= ColCount)
				{
					return new AncestorInfo { List = new List<int>(), Mask = new byte[ColCount] };
				}

				var cached = _ancestorCache[a][colIndex];
				if (cached != null) return cached;

				var list = new List<int>();
				var mask = new byte[ColCount];
				int current = colIndex;
				int guard = 0;

				while (current != -1 && mask[current] == 0 && guard++ <= ColCount + 2)
				{
					list.Add(current);
					mask[current] = 1;
					current = a == 0 ? GetZeroParent(current) : GetBParent(current, a);
				}

				var result = new AncestorInfo { List = list, Mask = mask };
				_ancestorCache[a][colIndex] = result;
				return result;
			}

			public int GetBParent(int colIndex, int b)
			{
				if (b < 1 || b > RowCount || colIndex < 0 || colIndex >= ColCount) return -1;

				int cached = _parentCache[b][colIndex];
				if (cached != -2) return cached;

				int row = b - 1;
				int value = M[colIndex][row];
				var ancestors = GetAAncestors(colIndex, b - 1).List;
				int best = -1;

				for (int i = 0; i < ancestors.Count; i++)
				{
					int candidate = ancestors[i];
					if (candidate >= colIndex) continue;
					if (M[candidate][row] < value)
					{
						best = candidate;
						break;
					}
				}

				_parentCache[b][colIndex] = best;
				return best;
			}
		}

		#endregion

		#region Core UPMS Logic

		private static bool LastColumnIsZero(List<List<int>> matrix)
		{
			if (matrix.Count == 0) return true;
			var last = matrix[matrix.Count - 1];
			for (int r = 0; r < last.Count; r++)
				if (last[r] != 0) return false;
			return true;
		}

		private static int FindLastNonZeroRowLabel(List<List<int>> matrix)
		{
			if (matrix.Count == 0) return -1;
			var last = matrix[matrix.Count - 1];
			for (int r = last.Count - 1; r >= 0; r--)
				if (last[r] != 0) return r + 1;
			return -1;
		}

		private static (int rootCol, int t)? FindBadRoot(Context ctx)
		{
			int lastCol = ctx.ColCount - 1;
			int t = FindLastNonZeroRowLabel(ctx.M);
			if (t == -1) return null;
			int rootCol = ctx.GetBParent(lastCol, t);
			if (rootCol == -1) return null;
			return (rootCol, t);
		}

		private static int[] ComputeDelta(Context ctx, int rootCol, int t)
		{
			int lastCol = ctx.ColCount - 1;
			var delta = new int[ctx.RowCount];
			for (int r = 0; r < ctx.RowCount; r++)
				delta[r] = r >= t - 1 ? 0 : ctx.M[lastCol][r] - ctx.M[rootCol][r];
			return delta;
		}

		private static int MaxEntry(List<List<int>> matrix)
		{
			int max = 0;
			for (int c = 0; c < matrix.Count; c++)
				for (int r = 0; r < matrix[c].Count; r++)
					if (matrix[c][r] > max) max = matrix[c][r];
			return max;
		}

		private class VRResult
		{
			public sbyte[] Data { get; set; }
			public Func<int, int, int> Index { get; set; }
			public int Height { get; set; }
		}

		private static VRResult ComputeUPMSVerificationRoots(Context ctx, int rootCol, int t)
		{
			var m = ctx.M;
			int alpha = ctx.ColCount - 1;
			int y = rootCol;
			int width = ctx.ColCount;
			int height = ctx.RowCount;
			int maxTwice = MaxEntry(m) * 2;

			var vr = new sbyte[width * height];
			for (int i = 0; i < vr.Length; i++) vr[i] = -1;

			int VrIndex(int col, int row) => col * height + row;
			bool InBadPart(int col, int row) => col >= y && col < alpha && row < t - 1;

			int GetVR(int col, int row) => InBadPart(col, row) ? vr[VrIndex(col, row)] : -1;
			void SetVR(int col, int row, int value) { vr[VrIndex(col, row)] = (sbyte)value; }

			int BaseValue(int col, int k, int r)
			{
				return STRICT_BASE_COLUMN ? m[col][r] + (r < k ? 1 : 0) : m[col][r] + (r < k - 1 ? 1 : 0);
			}

			bool ColumnLessThanBase(int candidate, int col, int k)
			{
				int limit = STRICT_BASE_COLUMN ? k + 1 : k;
				for (int r = 0; r < limit; r++)
				{
					int a = r < height ? m[candidate][r] : 0;
					int b = BaseValue(col, k, r);
					if (a < b) return true;
					if (a > b) return false;
				}
				return false;
			}

			int TransformedXValue(int sourceCol, int row, int iCol, int k)
			{
				int value = m[sourceCol][row];
				if (row < k - 1 && GetVR(sourceCol, row) == 1)
					value += maxTwice - m[iCol][row];
				return value;
			}

			int TransformedYValue(int sourceCol, int row, int jCol, int k)
			{
				int value = m[sourceCol][row];
				if (row < k - 1)
				{
					bool colIsJ = sourceCol == jCol;
					bool containsJ = ctx.GetAAncestors(sourceCol, row + 1).Mask[jCol] == 1;
					if (colIsJ || containsJ) value += maxTwice - m[jCol][row];
				}
				return value;
			}

			int CompareTransformedParts(int xStart, int xEnd, int yStart, int jCol, int iCol, int k)
			{
				int xLen = xEnd - xStart + 1;
				int yLen = alpha - yStart + 1;
				int commonCols = Math.Min(xLen, yLen);

				for (int local = 0; local < commonCols; local++)
				{
					int xCol = xStart + local;
					int yCol = yStart + local;
					for (int row = 0; row < height; row++)
					{
						int xv = TransformedXValue(xCol, row, iCol, k);
						int yv = TransformedYValue(yCol, row, jCol, k);
						if (xv < yv) return -1;
						if (xv > yv) return 1;
					}
				}
				if (xLen < yLen) return -1;
				if (xLen > yLen) return 1;
				return 0;
			}

			for (int row = 0; row < t - 1; row++)
			{
				int k = row + 1;
				for (int col = y; col < alpha; col++)
				{
					if (col == y || row == 0)
					{
						SetVR(col, row, 1);
						continue;
					}

					var kAncestors = ctx.GetAAncestors(col, k);
					bool ancestorHasVR0 = false;
					foreach (int ancCol in kAncestors.List)
					{
						if (GetVR(ancCol, row) == 0) { ancestorHasVR0 = true; break; }
					}

					int kParent = ctx.GetBParent(col, k);
					if (kAncestors.Mask[y] != 1 || ancestorHasVR0 || kParent == -1)
					{
						SetVR(col, row, 0);
						continue;
					}

					if (kParent != y)
					{
						SetVR(col, row, 1);
						continue;
					}

					bool earlierRowHasVR0 = false;
					for (int wRow = 0; wRow < row; wRow++)
						if (GetVR(col, wRow) == 0) { earlierRowHasVR0 = true; break; }
					if (earlierRowHasVR0) { SetVR(col, row, 0); continue; }

					bool higherParentEscapesBadRoot = false;
					for (int vRow = row + 1; vRow < t - 1; vRow++)
					{
						int v = vRow + 1;
						if (ctx.GetBParent(col, v) != y) { higherParentEscapesBadRoot = true; break; }
					}
					if (higherParentEscapesBadRoot) { SetVR(col, row, 0); continue; }

					int u = -1;
					for (int candidate = col + 1; candidate <= alpha; candidate++)
						if (ColumnLessThanBase(candidate, col, k)) { u = candidate; break; }

					if (u == -1) { SetVR(col, row, 1); continue; }

					int Ayk = m[y][row];
					var alphaAncestors = ctx.GetAAncestors(alpha, k).List;
					int j = -1;
					foreach (int ancCol in alphaAncestors)
						if (m[ancCol][row] == Ayk + 1) { j = ancCol; break; }
					if (j == -1) j = alpha;

					int cmp = CompareTransformedParts(col, u - 1, j, j, col, k);
					SetVR(col, row, cmp < 0 ? 0 : 1);
				}
			}

			return new VRResult { Data = vr, Index = VrIndex, Height = height };
		}

		private static List<List<int>> GenerateBh(Context ctx, List<List<int>> B, int[] delta, int t, int h, int rootCol, VRResult vr)
		{
			var result = new List<List<int>>();
			for (int localCol = 0; localCol < B.Count; localCol++)
			{
				int originalCol = rootCol + localCol;
				var next = new int[ctx.RowCount];
				for (int r = 0; r < ctx.RowCount; r++)
				{
					bool hasVR = r < t - 1 && vr.Data[vr.Index(originalCol, r)] == 1;
					next[r] = B[localCol][r] + h * delta[r] * (hasVR ? 1 : 0);
				}
				result.Add(next.ToList());
			}
			return result;
		}

		#endregion

		/// <summary>
		/// 对 UPMS 矩阵进行展开（二维统一接口，原地修改矩阵）。
		/// </summary>
		public static void ExpandUPMS(List<List<int>> matrix, int n)
		{
			if (matrix == null)
				throw new ArgumentNullException(nameof(matrix));

			if (matrix.Count == 0) return;

			for (int step = 0; step < n; step++)
			{
				var expanded = CoreExpandOne(matrix);
				matrix.Clear();
				matrix.AddRange(expanded);
			}
		}

		/// <summary>
		/// 核心展开：对 UPMS 矩阵执行一次展开，返回新矩阵（不修改输入）。
		/// </summary>
		private static List<List<int>> CoreExpandOne(List<List<int>> matrix)
		{
			if (!IsLegalUPMSMatrix(matrix)) return new List<List<int>>();

			var ctx = new Context(matrix);
			var m = ctx.M;

			if (m.Count == 0) return new List<List<int>>();

			if (LastColumnIsZero(m))
			{
				var result = new List<List<int>>();
				for (int i = 0; i < m.Count - 1; i++)
					result.Add(m[i].ToList());
				return StandardizeMatrix(result);
			}

			var badRoot = FindBadRoot(ctx);
			if (badRoot == null) return new List<List<int>>();

			var (rootCol, t) = badRoot.Value;

			var G = new List<List<int>>();
			for (int i = 0; i < rootCol; i++) G.Add(m[i].ToList());

			var B = new List<List<int>>();
			for (int i = rootCol; i < ctx.ColCount - 1; i++) B.Add(m[i].ToList());

			var delta = ComputeDelta(ctx, rootCol, t);
			var vr = ComputeUPMSVerificationRoots(ctx, rootCol, t);

			var finalResult = new List<List<int>>();

			foreach (var col in G) finalResult.Add(col.ToList());
			foreach (var col in B) finalResult.Add(col.ToList());

			var Bh = GenerateBh(ctx, B, delta, t, 1, rootCol, vr);
			foreach (var col in Bh) finalResult.Add(col.ToList());

			return StandardizeMatrix(finalResult);
		}

		/// <summary>
		/// 便捷方法：将锯齿数组转换为List<List<int>>
		/// </summary>
		public static List<List<int>> ArrayToMatrix(int[][] array)
		{
			if (array == null) return new List<List<int>>();
			var result = new List<List<int>>();
			foreach (var col in array)
				result.Add(col != null ? col.ToList() : new List<int>());
			return result;
		}

		/// <summary>
		/// 便捷方法：将List<List<int>>转换为锯齿数组
		/// </summary>
		public static int[][] MatrixToArray(List<List<int>> matrix)
		{
			if (matrix == null) return new int[0][];
			return matrix.Select(col => col.ToArray()).ToArray();
		}
	}
}