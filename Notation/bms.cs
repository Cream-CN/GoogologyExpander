using System;
using System.Collections.Generic;
using System.Linq;
namespace GoogologyExpander
{
	public static class BmsCore
	{
		/// <summary>
		/// 对BMS矩阵进行展开操作
		/// </summary>
		/// <param name="matrix">BMS矩阵（二维列表）</param>
		/// <param name="n">展开步数</param>
		public static void ExpandBMS(List<List<double>> matrix, int n)
		{
			// 空矩阵检查
			if (matrix.Count == 0) return;

			// 1. 构建父指针矩阵 (Parent Graph)
			var parentGraph = BuildParentGraph(matrix);

			// 2. 查找最后一个有效的父指针
			var lastColumn = parentGraph[parentGraph.Count - 1];
			int lastNonZero = FindLastNonNaN(lastColumn);
			if (lastNonZero == -1) return;

			// 3. 计算根节点和增量向量
			int rootIndex = (int)lastColumn[lastNonZero];
			var rootRow = matrix[rootIndex];
			var deltaVector = ComputeDeltaVector(matrix, rootRow, lastNonZero);

			// 4. 构建掩码矩阵
			var maskMatrix = BuildMaskMatrix(matrix, parentGraph, rootIndex);

			// 5. 执行展开操作
			PerformExpansion(matrix, rootIndex, maskMatrix, deltaVector, n);
		}

		/// <summary>
		/// 构建父指针矩阵
		/// </summary>
		private static List<List<double>> BuildParentGraph(List<List<double>> matrix)
		{
			var parentGraph = new List<List<double>>();

			for (int row = 0; row < matrix.Count; row++)
			{
				parentGraph.Add(new List<double>());

				for (int col = 0; col < matrix[row].Count; col++)
				{
					if (row == 0)
					{
						// 第一行的父指针为NaN
						parentGraph[row].Add(double.NaN);
						continue;
					}

					if (col == 0)
					{
						// 第一列：向上查找第一个不大于当前元素的行
						int parentRow = row - 1;
						while (parentRow >= 0 && matrix[row][col] <= matrix[parentRow][col])
							parentRow--;
						parentGraph[row].Add(parentRow == -1 ? double.NaN : parentRow);
						continue;
					}

					// 其他列：通过前一列父指针递归查找
					double currentParent = parentGraph[row][col - 1];
					while (!double.IsNaN(currentParent) &&
						   matrix[row][col] <= matrix[(int)currentParent][col])
					{
						currentParent = parentGraph[(int)currentParent][col - 1];
					}
					parentGraph[row].Add(currentParent);
				}
			}

			return parentGraph;
		}

		/// <summary>
		/// 查找最后一个非NaN值的索引
		/// </summary>
		private static int FindLastNonNaN(List<double> column)
		{
			int index = column.Count - 1;
			while (index >= 0 && double.IsNaN(column[index]))
				index--;
			return index;
		}

		/// <summary>
		/// 计算增量向量
		/// </summary>
		private static List<double> ComputeDeltaVector(
			List<List<double>> matrix,
			List<double> rootRow,
			int lastNonZeroIndex)
		{
			var deltaVector = new List<double>();
			var lastRow = matrix[matrix.Count - 1];

			for (int col = 0; col < rootRow.Count; col++)
			{
				if (col == lastNonZeroIndex)
				{
					// 最后一列增量为0
					deltaVector.Add(0);
				}
				else
				{
					// 其他列取正差值
					deltaVector.Add(Math.Max(0, lastRow[col] - rootRow[col]));
				}
			}

			return deltaVector;
		}

		/// <summary>
		/// 构建掩码矩阵
		/// </summary>
		private static List<List<bool>> BuildMaskMatrix(
			List<List<double>> matrix,
			List<List<double>> parentGraph,
			int rootIndex)
		{
			var maskMatrix = new List<List<bool>>();
			int maskRowCount = matrix.Count - rootIndex - 1;

			for (int row = 0; row < maskRowCount; row++)
			{
				maskMatrix.Add(new List<bool>());
				int actualRow = row + rootIndex;

				for (int col = 0; col < matrix[actualRow].Count; col++)
				{
					if (row == 0)
					{
						// 第一行掩码始终为true
						maskMatrix[row].Add(true);
						continue;
					}

					var parentValue = parentGraph[actualRow][col];

					if (double.IsNaN(parentValue))
					{
						maskMatrix[row].Add(false);
						continue;
					}

					int parentRow = (int)parentValue;
					if (parentRow < rootIndex)
					{
						maskMatrix[row].Add(false);
						continue;
					}

					// 递归传递掩码值
					int maskRow = parentRow - rootIndex;
					maskMatrix[row].Add(maskMatrix[maskRow][col]);
				}
			}

			return maskMatrix;
		}

		/// <summary>
		/// 执行展开操作
		/// </summary>
		private static void PerformExpansion(
			List<List<double>> matrix,
			int rootIndex,
			List<List<bool>> maskMatrix,
			List<double> deltaVector,
			int stepCount)
		{
			// 删除最后一行
			matrix.RemoveAt(matrix.Count - 1);

			// 计算坏部长度
			int badPartLength = matrix.Count - rootIndex;

			for (int step = 0; step < stepCount; step++)
			{
				// 提取坏部
				var badPart = matrix.GetRange(matrix.Count - badPartLength, badPartLength);

				// 添加坏部的展开副本
				for (int row = 0; row < badPart.Count; row++)
				{
					var newRow = new List<double>();
					var sourceRow = badPart[row];

					for (int col = 0; col < sourceRow.Count; col++)
					{
						double value = sourceRow[col];
						if (maskMatrix[row][col])
						{
							value += deltaVector[col];
						}
						newRow.Add(value);
					}

					matrix.Add(newRow);
				}
			}
		}
	}
}