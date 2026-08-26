using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GoogologyExpander
{
	/// <summary>
	/// BSM (Bashicu Super Matrix) 引擎
	/// 基于BMS和BHM的扩展，增加了强制小根、强制大根等概念
	/// </summary>
	public class BsmEngine
	{
		/// <summary>
		/// 展开矩阵 n 步
		/// </summary>
		public int[][] Expand(int[][] matrix, int n)
		{
			if (IsEmpty(matrix))
				return new int[0][];

			var result = CopyMatrix(matrix);
			for (int i = 0; i < n; i++)
			{
				result = ExpandOnce(result);
				if (IsEmpty(result))
					break;
			}
			return result;
		}

		/// <summary>
		/// 展开一步
		/// </summary>
		public int[][] ExpandOnce(int[][] matrix)
		{
			if (IsEmpty(matrix))
				return new int[0][];

			int rows = GetRowCount(matrix);
			int cols = GetColCount(matrix);

			// 如果最后一列全为零，删除最后一列 (规则2)
			if (IsLastColumnZero(matrix))
			{
				return RemoveLastColumn(matrix);
			}

			// 查找坏根 (规则15)
			int badRoot = FindBadRoot(matrix);

			// 坏部：坏根到倒数第二列
			int[][] badPart = GetBadPart(matrix, badRoot);

			// 好部：坏根之前
			int[][] goodPart = GetGoodPart(matrix, badRoot);

			// 阶差向量 (规则9)
			int[] delta = ComputeDelta(matrix, badRoot, rows);

			// 构建结果
			var result = new List<int[]>();

			// 添加好部
			foreach (var col in goodPart)
				result.Add(col);

			// 复制坏部并加上阶差向量
			for (int i = 0; i < badPart.Length; i++)
			{
				int[] newCol = ApplyDeltaToColumn(badPart[i], delta, 1, matrix, badRoot, badPart, i);
				result.Add(newCol);
			}

			return result.ToArray();
		}

		/// <summary>
		/// 查找坏根 (规则15)
		/// </summary>
		private int FindBadRoot(int[][] matrix)
		{
			int rows = GetRowCount(matrix);
			int cols = GetColCount(matrix);

			if (cols <= 1 || rows == 0)
				return 0;

			// 获取所有待定坏根
			var candidates = GetCandidateBadRoots(matrix);

			if (candidates.Count == 0)
				return 0;

			// 最右侧待定坏根
			int rightmostCandidate = candidates.Last();

			// 获取基准式 (最右侧待定坏根的预展开式)
			int[][] baseExpansion = GetPreExpansion(matrix, rightmostCandidate);

			// 收集所有满足条件的待定坏根
			var validRoots = new List<int>();

			foreach (int candidate in candidates)
			{
				int[][] preExpansion = GetPreExpansion(matrix, candidate);

				// 检查是否为小根 (规则12)
				bool isSmallRoot = IsSmallRoot(preExpansion, baseExpansion);

				// 检查是否为强制小根 (规则13)
				bool isForcedSmallRoot = IsForcedSmallRoot(matrix, candidate, rightmostCandidate);

				// 检查是否为强制大根 (规则14)
				bool isForcedLargeRoot = IsForcedLargeRoot(matrix, candidate, rightmostCandidate);

				// 坏根为在所有"是小根或强制小根，但不是强制大根"的待定坏根右边的第一个
				if ((isSmallRoot || isForcedSmallRoot) && !isForcedLargeRoot)
				{
					validRoots.Add(candidate);
				}
			}

			// 返回最右侧的有效根
			if (validRoots.Count > 0)
				return validRoots.Last();

			// 如果没有有效根，返回第0列
			return 0;
		}

		/// <summary>
		/// 获取待定坏根候选 (规则6)
		/// </summary>
		private List<int> GetCandidateBadRoots(int[][] matrix)
		{
			var candidates = new List<int>();
			int rows = GetRowCount(matrix);
			int cols = GetColCount(matrix);

			if (cols <= 1 || rows == 0)
				return candidates;

			// 找到最后一列最后一个非零项
			int lastNonZeroRow = -1;
			for (int r = rows - 1; r >= 0; r--)
			{
				if (matrix[r][cols - 1] != 0)
				{
					lastNonZeroRow = r;
					break;
				}
			}

			if (lastNonZeroRow == -1)
				return candidates;

			// 找到该元素的父项
			int parent = FindParent(matrix, lastNonZeroRow, cols - 1);

			// 父项的父项的子项 (即父项)
			int badRoot = parent;

			// 检查条件：如果末列最后一个非零项不在第一行，
			// 则待定坏根正上方的元素应当是末列最后一个非零项正上方元素的祖先项
			if (lastNonZeroRow > 0)
			{
				int upperRow = lastNonZeroRow - 1;
				var ancestors = GetAncestors(matrix, upperRow, cols - 1);

				// 找到满足条件的列
				for (int c = parent; c >= 0; c--)
				{
					if (ancestors.Contains(c))
					{
						badRoot = c;
						break;
					}
				}
			}

			// 从坏根到倒数第二列都是待定坏根候选
			for (int c = badRoot; c < cols - 1; c++)
			{
				candidates.Add(c);
			}

			return candidates;
		}

		/// <summary>
		/// 获取预展开式 (规则10)
		/// </summary>
		private int[][] GetPreExpansion(int[][] matrix, int badRoot)
		{
			int rows = GetRowCount(matrix);
			int cols = GetColCount(matrix);

			if (rows == 0 || cols == 0)
				return new int[0][];

			// 获取好部、坏部、阶差向量
			int[][] goodPart = GetGoodPart(matrix, badRoot);
			int[][] badPart = GetBadPart(matrix, badRoot);
			int[] delta = ComputeDelta(matrix, badRoot, rows);

			var result = new List<int[]>();

			// 添加好部
			foreach (var col in goodPart)
				result.Add(col);

			// 添加坏部
			foreach (var col in badPart)
				result.Add(col);

			// 添加坏部 + 阶差向量
			foreach (var col in badPart)
			{
				int[] newCol = ApplyDeltaToColumn(col, delta, 1, matrix, badRoot, badPart,
					Array.IndexOf(badPart, col));
				result.Add(newCol);
			}

			// 添加末列 + 阶差向量
			int[] lastCol = GetColumn(matrix, cols - 1);
			int[] newLastCol = ApplyDeltaToColumn(lastCol, delta, 1, matrix, badRoot,
				new int[][] { lastCol }, 0);
			result.Add(newLastCol);

			return result.ToArray();
		}

		/// <summary>
		/// 检查是否为小根 (规则12)
		/// </summary>
		private bool IsSmallRoot(int[][] expansion, int[][] baseExpansion)
		{
			return CompareLexicographically(expansion, baseExpansion) < 0;
		}

		/// <summary>
		/// 检查是否为强制小根 (规则13)
		/// </summary>
		private bool IsForcedSmallRoot(int[][] matrix, int candidate, int rightmost)
		{
			// 检查候选是否是最右侧坏根的祖先项
			if (!IsAncestorOf(matrix, candidate, rightmost))
				return false;

			// 检查下方所有元素是否与最右侧坏根下方的元素不完全相同
			int rows = GetRowCount(matrix);
			int startRow = 1; // 从第二行开始检查

			// 检查从startRow到最后一行的所有元素
			for (int r = startRow; r < rows; r++)
			{
				if (matrix[r][candidate] != matrix[r][rightmost])
				{
					// 只要有一个不同，就是强制小根
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// 检查是否为强制大根 (规则14)
		/// </summary>
		private bool IsForcedLargeRoot(int[][] matrix, int candidate, int rightmost)
		{
			// 检查候选是否是最右侧坏根的祖先项
			if (!IsAncestorOf(matrix, candidate, rightmost))
				return false;

			// 检查下方所有元素是否与最右侧坏根下方的元素完全相同
			int rows = GetRowCount(matrix);
			int startRow = 1; // 从第二行开始检查

			// 检查从startRow到最后一行的所有元素
			for (int r = startRow; r < rows; r++)
			{
				if (matrix[r][candidate] != matrix[r][rightmost])
				{
					// 有任何一个不同，就不是强制大根
					return false;
				}
			}

			// 所有行都相同，是强制大根
			return true;
		}

		/// <summary>
		/// 检查一列是否是另一列的祖先
		/// </summary>
		private bool IsAncestorOf(int[][] matrix, int ancestorCol, int descendantCol)
		{
			if (ancestorCol == descendantCol)
				return true;

			int rows = GetRowCount(matrix);
			if (rows == 0)
				return false;

			// 检查第一行的祖先关系
			var ancestors = GetAncestors(matrix, 0, descendantCol);
			return ancestors.Contains(ancestorCol);
		}

		/// <summary>
		/// 字典序比较两个矩阵
		/// </summary>
		private int CompareLexicographically(int[][] matrix1, int[][] matrix2)
		{
			int rows1 = GetRowCount(matrix1);
			int rows2 = GetRowCount(matrix2);
			int cols1 = GetColCount(matrix1);
			int cols2 = GetColCount(matrix2);

			int minCols = Math.Min(cols1, cols2);
			int minRows = Math.Min(rows1, rows2);

			// 逐列比较
			for (int c = 0; c < minCols; c++)
			{
				for (int r = 0; r < minRows; r++)
				{
					int val1 = (r < rows1 && c < cols1) ? matrix1[r][c] : 0;
					int val2 = (r < rows2 && c < cols2) ? matrix2[r][c] : 0;

					if (val1 != val2)
						return val1.CompareTo(val2);
				}
			}

			// 如果前面的都相等，比较列数
			return cols1.CompareTo(cols2);
		}

		/// <summary>
		/// 查找元素的父项
		/// 第一行：找左边第一个小于当前值，若不存在则取第0列
		/// 其他行：找左边第一个小于当前值，且上方是上方元素的祖先，若不存在则取第0列
		/// </summary>
		private int FindParent(int[][] matrix, int row, int col)
		{
			int cols = GetColCount(matrix);

			if (cols == 0 || col == 0)
				return 0;

			if (row == 0)
			{
				// 第一行：找左边第一个小于当前值
				int value = matrix[row][col];
				for (int c = col - 1; c >= 0; c--)
				{
					if (matrix[row][c] < value)
						return c;
				}
				// 特别地，若该元素无父项，则将其父项取为第0列
				return 0;
			}
			else
			{
				// 其他行：找左边第一个小于当前值，且其正上方的项是当前元素正上方项的祖先
				int value = matrix[row][col];

				// 获取上方元素的祖先链
				var ancestors = GetAncestors(matrix, row - 1, col);

				for (int c = col - 1; c >= 0; c--)
				{
					if (matrix[row][c] < value && IsAncestor(ancestors, row - 1, c))
					{
						return c;
					}
				}

				// 特别地，若该元素无父项，则将其父项取为第0列
				return 0;
			}
		}

		/// <summary>
		/// 获取元素的祖先链（包含自身）
		/// </summary>
		private List<int> GetAncestors(int[][] matrix, int row, int col)
		{
			var ancestors = new List<int>();
			int currentCol = col;

			while (true)
			{
				ancestors.Add(currentCol);
				if (currentCol == 0)
					break;

				int parentCol = FindParent(matrix, row, currentCol);
				if (parentCol == currentCol)
					break;

				currentCol = parentCol;
			}

			return ancestors;
		}

		/// <summary>
		/// 检查某列是否是另一列元素的祖先
		/// </summary>
		private bool IsAncestor(List<int> ancestors, int row, int col)
		{
			return ancestors.Contains(col);
		}

		/// <summary>
		/// 计算阶差向量 (规则9)
		/// </summary>
		private int[] ComputeDelta(int[][] matrix, int badRoot, int rows)
		{
			int cols = GetColCount(matrix);
			if (cols == 0 || rows == 0)
				return new int[0];

			int[] lastCol = GetColumn(matrix, cols - 1);
			int[] badRootCol = GetColumn(matrix, badRoot);

			int[] delta = new int[rows];

			// 找到末列最后一个非零项的行
			int lastNonZeroRow = -1;
			for (int r = rows - 1; r >= 0; r--)
			{
				if (matrix[r][cols - 1] != 0)
				{
					lastNonZeroRow = r;
					break;
				}
			}

			for (int i = 0; i < rows; i++)
			{
				// 对于末列最后一个非零项的元素所在列，阶差向量取值要额外减去一
				if (i == lastNonZeroRow)
				{
					delta[i] = lastCol[i] - badRootCol[i] - 1;
				}
				// 对于在该列之下的所有列，阶差向量总取为零
				else if (i > lastNonZeroRow)
				{
					delta[i] = 0;
				}
				else
				{
					delta[i] = lastCol[i] - badRootCol[i];
				}
			}

			return delta;
		}

		/// <summary>
		/// 对列应用阶差向量
		/// </summary>
		private int[] ApplyDeltaToColumn(int[] column, int[] delta, int k,
			int[][] matrix, int badRoot, int[][] badPart, int badIndex)
		{
			int rows = column.Length;
			int[] result = new int[rows];

			for (int r = 0; r < rows; r++)
			{
				// 如果坏根中的元素不是该项的祖先，则该项保持不变
				bool isAncestor = IsBadRootAncestorOf(matrix, badRoot, r, badPart, badIndex);

				if (!isAncestor)
				{
					result[r] = column[r];
				}
				else
				{
					result[r] = column[r] + delta[r] * k;
				}
			}

			return result;
		}

		/// <summary>
		/// 检查坏根中的元素是否是坏部中某项的祖先
		/// </summary>
		private bool IsBadRootAncestorOf(int[][] matrix, int badRoot, int row, int[][] badPart, int badIndex)
		{
			int rows = GetRowCount(matrix);

			if (row == rows - 1)
				return false;

			if (row == 0)
				return true;

			if (badRoot + badIndex < GetColCount(matrix) && matrix[row][badRoot + badIndex] != 0)
				return true;

			return false;
		}

		/// <summary>
		/// 获取列向量
		/// </summary>
		private int[] GetColumn(int[][] matrix, int colIndex)
		{
			int rows = GetRowCount(matrix);
			int cols = GetColCount(matrix);

			if (rows == 0 || cols == 0 || colIndex < 0 || colIndex >= cols)
				return new int[0];

			int[] column = new int[rows];
			for (int r = 0; r < rows; r++)
			{
				column[r] = matrix[r][colIndex];
			}
			return column;
		}

		/// <summary>
		/// 检查最后一列是否全为零
		/// </summary>
		private bool IsLastColumnZero(int[][] matrix)
		{
			int rows = GetRowCount(matrix);
			int cols = GetColCount(matrix);

			if (cols == 0 || rows == 0)
				return true;

			for (int r = 0; r < rows; r++)
			{
				if (matrix[r][cols - 1] != 0)
					return false;
			}
			return true;
		}

		/// <summary>
		/// 获取坏部
		/// </summary>
		private int[][] GetBadPart(int[][] matrix, int badRoot)
		{
			int rows = GetRowCount(matrix);
			int cols = GetColCount(matrix);

			if (rows == 0 || cols == 0)
				return new int[0][];

			int badLength = cols - badRoot - 1;
			if (badLength <= 0)
				return new int[0][];

			int[][] badPart = new int[badLength][];
			for (int c = 0; c < badLength; c++)
			{
				badPart[c] = new int[rows];
				for (int r = 0; r < rows; r++)
				{
					badPart[c][r] = matrix[r][badRoot + c];
				}
			}

			return badPart;
		}

		/// <summary>
		/// 获取好部
		/// </summary>
		private int[][] GetGoodPart(int[][] matrix, int badRoot)
		{
			int rows = GetRowCount(matrix);
			int cols = GetColCount(matrix);

			if (rows == 0 || cols == 0 || badRoot == 0)
				return new int[0][];

			int[][] goodPart = new int[badRoot][];
			for (int c = 0; c < badRoot; c++)
			{
				goodPart[c] = new int[rows];
				for (int r = 0; r < rows; r++)
				{
					goodPart[c][r] = matrix[r][c];
				}
			}

			return goodPart;
		}

		/// <summary>
		/// 移除最后一列
		/// </summary>
		private int[][] RemoveLastColumn(int[][] matrix)
		{
			int rows = GetRowCount(matrix);
			int cols = GetColCount(matrix);

			if (rows == 0 || cols <= 1)
				return new int[0][];

			int[][] result = new int[rows][];
			for (int r = 0; r < rows; r++)
			{
				result[r] = new int[cols - 1];
				for (int c = 0; c < cols - 1; c++)
				{
					result[r][c] = matrix[r][c];
				}
			}

			return result;
		}

		/// <summary>
		/// 复制矩阵 - 修复版 (行优先)
		/// </summary>
		private int[][] CopyMatrix(int[][] matrix)
		{
			if (matrix == null || matrix.Length == 0)
				return new int[0][];

			int rows = matrix.Length;
			if (rows == 0)
				return new int[0][];

			int cols = matrix[0].Length;
			if (cols == 0)
				return new int[0][];

			// 创建新矩阵，保持行优先格式
			int[][] copy = new int[rows][];
			for (int r = 0; r < rows; r++)
			{
				copy[r] = new int[cols];
				for (int c = 0; c < cols; c++)
				{
					copy[r][c] = matrix[r][c];
				}
			}

			return copy;
		}

		/// <summary>
		/// 判断矩阵是否为空
		/// </summary>
		public bool IsEmpty(int[][] matrix)
		{
			return matrix == null || matrix.Length == 0 || matrix[0].Length == 0;
		}

		/// <summary>
		/// 获取行数
		/// </summary>
		public int GetRowCount(int[][] matrix)
		{
			if (IsEmpty(matrix))
				return 0;
			return matrix.Length;
		}

		/// <summary>
		/// 获取列数
		/// </summary>
		public int GetColCount(int[][] matrix)
		{
			if (IsEmpty(matrix))
				return 0;
			return matrix[0].Length;
		}

		/// <summary>
		/// 带详细信息的展开
		/// </summary>
		public BsmExpansionResult ExpandWithDetails(int[][] matrix, int n)
		{
			var history = new List<int[][]>();
			var details = new List<string>();

			if (IsEmpty(matrix))
			{
				history.Add(new int[0][]);
				details.Add("空矩阵");
				return new BsmExpansionResult
				{
					History = history,
					Details = details,
					Final = new int[0][]
				};
			}

			var current = CopyMatrix(matrix);
			history.Add(current);
			details.Add($"初始矩阵: {FormatMatrix(current)}");

			for (int i = 0; i < n; i++)
			{
				if (IsEmpty(current))
					break;

				int rows = GetRowCount(current);
				int cols = GetColCount(current);

				// 检查最后一列是否全为零
				if (IsLastColumnZero(current))
				{
					details.Add($"步骤 {i + 1}: 最后一列为零，执行 +1 操作");
					current = RemoveLastColumn(current);
					history.Add(current);
					details.Add($"  结果: {FormatMatrix(current)}");
					continue;
				}

				// 获取候选坏根
				var candidates = GetCandidateBadRoots(current);
				details.Add($"步骤 {i + 1}:");
				details.Add($"  矩阵: {FormatMatrix(current)}");
				details.Add($"  候选坏根: {string.Join(", ", candidates)}");

				// 查找坏根
				int badRoot = FindBadRoot(current);

				if (badRoot >= 0 && badRoot < cols)
				{
					int[] lastCol = GetColumn(current, cols - 1);
					int[] badRootCol = GetColumn(current, badRoot);

					details.Add($"  坏根索引: {badRoot} (列: {FormatColumn(badRootCol)})");
					details.Add($"  末列: {FormatColumn(lastCol)}");

					// 计算阶差向量
					int[] delta = ComputeDelta(current, badRoot, rows);
					details.Add($"  阶差向量: ({string.Join(", ", delta)})");

					// 获取坏部和好部
					int[][] badPart = GetBadPart(current, badRoot);
					int[][] goodPart = GetGoodPart(current, badRoot);

					details.Add($"  好部: {FormatMatrix(goodPart)}");
					details.Add($"  坏部: {FormatMatrix(badPart)}");
				}
				else
				{
					details.Add($"  坏根: 未找到，使用第0列");
				}

				// 执行展开
				current = ExpandOnce(current);
				history.Add(current);
				details.Add($"  展开后: {FormatMatrix(current)}");
				details.Add("");
			}

			return new BsmExpansionResult
			{
				History = history,
				Details = details,
				Final = current
			};
		}

		/// <summary>
		/// 格式化矩阵
		/// </summary>
		private string FormatMatrix(int[][] matrix)
		{
			if (IsEmpty(matrix))
				return "()";

			int rows = GetRowCount(matrix);
			int cols = GetColCount(matrix);

			var result = new StringBuilder();
			for (int r = 0; r < rows; r++)
			{
				if (r > 0) result.Append(" ");
				result.Append("(");
				for (int c = 0; c < cols; c++)
				{
					if (c > 0) result.Append(", ");
					result.Append(matrix[r][c]);
				}
				result.Append(")");
			}

			return result.ToString();
		}

		/// <summary>
		/// 格式化列
		/// </summary>
		private string FormatColumn(int[] column)
		{
			if (column == null || column.Length == 0)
				return "()";
			return "(" + string.Join(", ", column) + ")";
		}

		/// <summary>
		/// 格式化矩阵为字符串
		/// </summary>
		public string Format(int[][] matrix)
		{
			return FormatMatrix(matrix);
		}

		/// <summary>
		/// 检查矩阵是否为标准形式
		/// </summary>
		public bool IsStandard(int[][] matrix)
		{
			if (IsEmpty(matrix))
				return true;

			int rows = GetRowCount(matrix);
			int cols = GetColCount(matrix);

			for (int c = 0; c < cols; c++)
			{
				for (int r = 1; r < rows; r++)
				{
					if (matrix[r][c] < matrix[r - 1][c])
						return false;
				}
			}

			return true;
		}

		/// <summary>
		/// 获取版本信息
		/// </summary>
		public string GetVersion()
		{
			return "BSM (Bashicu Super Matrix)";
		}
	}

	/// <summary>
	/// BSM 展开结果
	/// </summary>
	public class BsmExpansionResult
	{
		public List<int[][]> History { get; set; }
		public List<string> Details { get; set; }
		public int[][] Final { get; set; }

		public string GetDetailedReport()
		{
			var sb = new StringBuilder();
			foreach (var detail in Details)
			{
				sb.AppendLine(detail);
			}
			return sb.ToString();
		}
	}

	/// <summary>
	/// BSM 解析器
	/// </summary>
	public static class BsmParser
	{
		/// <summary>
		/// 解析 BSM 矩阵
		/// </summary>
		public static int[][] Parse(string input)
		{
			input = input.Trim();
			if (string.IsNullOrEmpty(input) || input == "请输入矩阵，如 (0,0)(1,1)(2,0)...")
				return new int[0][];

			// 检查是否包含括号
			if (!input.Contains("("))
			{
				return ParseAsSequence(input);
			}

			// 解析括号格式
			var columns = new List<int[]>();
			var parts = input.Split(new[] { ')' }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var part in parts)
			{
				var trimmed = part.Trim();
				if (trimmed.StartsWith("("))
					trimmed = trimmed.Substring(1);

				var numbers = trimmed.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
					.Select(s => s.Trim())
					.Where(s => !string.IsNullOrEmpty(s))
					.Select(s => int.Parse(s))
					.ToArray();

				if (numbers.Length > 0)
					columns.Add(numbers);
			}

			if (columns.Count == 0)
				return new int[0][];

			// 确保所有列长度一致
			int rowCount = columns.Max(col => col.Length);

			// 构建行优先矩阵
			int[][] result = new int[rowCount][];
			for (int r = 0; r < rowCount; r++)
			{
				result[r] = new int[columns.Count];
				for (int c = 0; c < columns.Count; c++)
				{
					if (r < columns[c].Length)
						result[r][c] = columns[c][r];
					else
						result[r][c] = 0;
				}
			}

			return result;
		}

		/// <summary>
		/// 作为序列解析
		/// </summary>
		private static int[][] ParseAsSequence(string input)
		{
			var numbers = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(s => s.Trim())
				.Where(s => !string.IsNullOrEmpty(s))
				.Select(s => int.Parse(s))
				.ToArray();

			if (numbers.Length == 0)
				return new int[0][];

			int[][] matrix = new int[1][];
			matrix[0] = numbers;
			return matrix;
		}

		/// <summary>
		/// 格式化矩阵
		/// </summary>
		public static string Format(int[][] matrix)
		{
			if (matrix == null || matrix.Length == 0)
				return "()";

			int rows = matrix.Length;
			int cols = matrix[0].Length;

			var result = new StringBuilder();
			for (int r = 0; r < rows; r++)
			{
				if (r > 0) result.Append(" ");
				result.Append("(");
				for (int c = 0; c < cols; c++)
				{
					if (c > 0) result.Append(", ");
					result.Append(matrix[r][c]);
				}
				result.Append(")");
			}

			return result.ToString();
		}
	}
}