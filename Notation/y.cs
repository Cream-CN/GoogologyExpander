using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogologyExpander
{
	public static class YExpander
	{
		#region 内部数据结构
		private class MountainElement
		{
			public int Value { get; set; }
			public int Position { get; set; }
			public int ParentIndex { get; set; }
			public bool ForcedParent { get; set; }
		}
		#endregion

		#region 公共入口（仅展开一次）
		/// <summary>
		/// 展开 Y 序列一次（数组输入，数组输出）
		/// </summary>
		public static int[] Expand(int[] sequence)
		{
			// 构建初始层（无强制父索引）
			var initialLayer = sequence
				.Select((val, idx) => new MountainElement
				{
					Value = val,
					Position = idx,
					ParentIndex = -1,
					ForcedParent = false
				})
				.ToList();

			var mountain = CalcMountain(initialLayer);
			var resultMountain = ExpandInternal(mountain, 1); // 只展开一次
			return resultMountain[0].Select(e => e.Value).ToArray();
		}
		#endregion

		#region 私有实现（完整移植自原 JS）
		private static MountainElement ParseSequenceElement(string s, int i)
		{
			int vIndex = s.IndexOf('v');
			if (vIndex == -1 || !double.IsFinite(double.Parse(s.Substring(vIndex + 1))))
			{
				return new MountainElement
				{
					Value = int.Parse(s),
					Position = i,
					ParentIndex = -1
				};
			}
			else
			{
				int val = int.Parse(s.Substring(0, vIndex));
				int parent = int.Parse(s.Substring(vIndex + 1));
				parent = Math.Max(Math.Min(i - 1, parent), -1);
				return new MountainElement
				{
					Value = val,
					Position = i,
					ParentIndex = parent,
					ForcedParent = true
				};
			}
		}

		private static List<List<MountainElement>> CalcMountain(string s)
		{
			var parts = s.Split(new char[] { '\t', ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
			var lastLayer = parts.Select((p, idx) => ParseSequenceElement(p, idx)).ToList();
			return CalcMountain(lastLayer);
		}

		private static List<List<MountainElement>> CalcMountain(List<MountainElement> initialLayer)
		{
			var calculatedMountain = new List<List<MountainElement>> { initialLayer };
			var lastLayer = initialLayer;

			while (true)
			{
				bool hasNextLayer = false;

				for (int i = 0; i < lastLayer.Count; i++)
				{
					var elem = lastLayer[i];
					if (elem.ForcedParent)
					{
						if (elem.ParentIndex != -1) hasNextLayer = true;
						continue;
					}

					int p;
					if (calculatedMountain.Count == 1)
					{
						p = elem.Position + 1;
					}
					else
					{
						p = 0;
						var prevLayer = calculatedMountain[calculatedMountain.Count - 2];
						while (prevLayer[p].Position < elem.Position + 1) p++;
					}

					while (true)
					{
						if (p < 0) break;
						int j;
						if (calculatedMountain.Count == 1)
						{
							p--;
							j = p - 1;
						}
						else
						{
							var prevLayer = calculatedMountain[calculatedMountain.Count - 2];
							p = prevLayer[p].ParentIndex;
							if (p < 0) break;
							j = 0;
							while (lastLayer[j].Position < prevLayer[p].Position - 1) j++;
						}

						if (j < 0 || (j < lastLayer.Count - 1 && lastLayer[j].Position + 1 != lastLayer[j + 1].Position))
							break;

						if (lastLayer[j].Value < elem.Value)
						{
							elem.ParentIndex = j;
							hasNextLayer = true;
							break;
						}
					}
				}

				if (!hasNextLayer) break;

				var currentLayer = new List<MountainElement>();
				calculatedMountain.Add(currentLayer);

				for (int i = 0; i < lastLayer.Count; i++)
				{
					if (lastLayer[i].ParentIndex != -1)
					{
						currentLayer.Add(new MountainElement
						{
							Value = lastLayer[i].Value - lastLayer[lastLayer[i].ParentIndex].Value,
							Position = lastLayer[i].Position - 1,
							ParentIndex = -1
						});
					}
				}

				lastLayer = currentLayer;
			}

			return calculatedMountain;
		}

		private static string CalcDiagonal(List<List<MountainElement>> mountain)
		{
			var diagonal = new List<int>();
			var diagonalTree = new List<int>();

			for (int i = 0; i < mountain[0].Count; i++)
			{
				for (int j = mountain.Count - 1; j >= 0; j--)
				{
					int k = 0;
					while (mountain[j][k] != null && mountain[j][k].Position + j < i) k++;
					if (mountain[j][k] == null || mountain[j][k].Position + j != i) continue;

					int height = j;
					int lastIndex = k;

					while (true)
					{
						if (height == 0)
						{
							lastIndex = mountain[height][lastIndex].ParentIndex;
						}
						else
						{
							int l = 0;
							while (mountain[height - 1][l].Position != mountain[height][lastIndex].Position + 1) l++;
							l = mountain[height - 1][l].ParentIndex;

							int m = 0;
							while (mountain[height][m].Position < mountain[height - 1][l].Position - 1) m++;

							if (mountain[height][m].Position == mountain[height - 1][l].Position - 1)
							{
								lastIndex = m;
							}
							else
							{
								height--;
								lastIndex = l;
							}
						}

						if (mountain[height][lastIndex] == null || mountain[height][lastIndex].ParentIndex == -1)
						{
							diagonal.Add(mountain[j][k].Value);
							diagonalTree.Add((mountain[height][lastIndex] != null ? mountain[height][lastIndex].Position : -1) + height);
							break;
						}
					}
					break;
				}
			}

			var pw = new List<int>();
			for (int i = 0; i < diagonal.Count; i++)
			{
				int p = -1;
				for (int j = i - 1; j >= 0; j--)
				{
					if (diagonal[j] < diagonal[i])
					{
						p = j;
						break;
					}
				}
				pw.Add(p);
			}

			var r = new List<string>();
			for (int i = 0; i < diagonal.Count; i++)
			{
				int p = i;
				while (true)
				{
					p = diagonalTree[p];
					if (p < 0 || diagonal[p] < diagonal[i]) break;
				}
				if (p == pw[i])
					r.Add(diagonal[i].ToString());
				else
					r.Add(diagonal[i] + "v" + p);
			}

			return string.Join(",", r);
		}

		private static List<List<MountainElement>> CloneMountain(List<List<MountainElement>> mountain)
		{
			var clone = new List<List<MountainElement>>();
			foreach (var layer in mountain)
			{
				var newLayer = new List<MountainElement>();
				foreach (var elem in layer)
				{
					newLayer.Add(new MountainElement
					{
						Value = elem.Value,
						Position = elem.Position,
						ParentIndex = elem.ParentIndex,
						ForcedParent = elem.ForcedParent
					});
				}
				clone.Add(newLayer);
			}
			return clone;
		}

		private static int GetBadRoot(List<List<MountainElement>> mountain)
		{
			var diagonalMountain = CalcMountain(CalcDiagonal(mountain));
			if (diagonalMountain[0][diagonalMountain[0].Count - 1].Value != 1)
			{
				return GetBadRoot(diagonalMountain);
			}
			else
			{
				for (int i = mountain.Count - 1; i >= 0; i--)
				{
					if (mountain[i][mountain[i].Count - 1].Position + i == mountain[0].Count - 1)
						return mountain[i - 1][mountain[i - 1][mountain[i - 1].Count - 1].ParentIndex].Position + i - 1;
				}
			}
			return -1;
		}

		// 核心展开（支持多次，但公共入口只传 n=1）
		private static List<List<MountainElement>> ExpandInternal(List<List<MountainElement>> mountain, int n)
		{
			var result = CloneMountain(mountain);

			if (mountain[0][mountain[0].Count - 1].ParentIndex == -1)
			{
				result[0].RemoveAt(result[0].Count - 1);
				return result;
			}

			int cutHeight = mountain.Count - 1;
			while (mountain[cutHeight][mountain[cutHeight].Count - 1].Position + cutHeight != mountain[0].Count - 1)
				cutHeight--;

			int actualCutHeight = cutHeight;
			int badRootSeam = GetBadRoot(mountain);

			int badRootHeight = mountain.Count - 1;
			while (true)
			{
				int i = 0;
				while (mountain[badRootHeight][i] != null && mountain[badRootHeight][i].Position + badRootHeight < badRootSeam)
					i++;
				if (mountain[badRootHeight][i] != null && mountain[badRootHeight][i].Position + badRootHeight == badRootSeam)
					break;
				badRootHeight--;
			}

			var diagonalMountain = CalcMountain(CalcDiagonal(mountain));
			bool yamakazi = diagonalMountain[0][diagonalMountain[0].Count - 1].Value == 1;

			List<List<MountainElement>> newDiagonalMountain;
			if (yamakazi)
			{
				newDiagonalMountain = CloneMountain(diagonalMountain);
				newDiagonalMountain[0].RemoveAt(newDiagonalMountain[0].Count - 1);
				for (int i = 0; i < n; i++)
				{
					for (int j = badRootSeam; j < mountain[0].Count - 1; j++)
					{
						newDiagonalMountain[0].Add(new MountainElement
						{
							Value = newDiagonalMountain[0][j].Value,
							Position = newDiagonalMountain[0][j].Position,
							ParentIndex = newDiagonalMountain[0][j].ParentIndex,
							ForcedParent = newDiagonalMountain[0][j].ForcedParent
						});
					}
				}
				cutHeight--;
				badRootHeight = cutHeight;
			}
			else
			{
				newDiagonalMountain = ExpandInternal(diagonalMountain, n);
				badRootHeight = mountain.Count - 1;
				while (true)
				{
					int i = 0;
					while (mountain[badRootHeight][i] != null && mountain[badRootHeight][i].Position + badRootHeight < badRootSeam)
						i++;
					if (mountain[badRootHeight][i] != null && mountain[badRootHeight][i].Position + badRootHeight == badRootSeam)
						break;
					badRootHeight--;
				}
			}

			for (int i = 0; i <= actualCutHeight; i++)
				result[i].RemoveAt(result[i].Count - 1);
			if (result[result.Count - 1].Count == 0)
				result.RemoveAt(result.Count - 1);

			int afterCutHeight = result.Count;
			var afterCutMountain = CloneMountain(result);
			int afterCutLength = result[0].Count;

			int badRootSeamHeight = afterCutHeight - 1;
			while (true)
			{
				int l = 0;
				while (mountain[badRootSeamHeight][l] != null && mountain[badRootSeamHeight][l].Position + badRootSeamHeight < badRootSeam)
					l++;
				if (mountain[badRootSeamHeight][l] != null && mountain[badRootSeamHeight][l].Position + badRootSeamHeight == badRootSeam)
					break;
				badRootSeamHeight--;
			}
			badRootSeamHeight++;

			for (int iter = 1; iter <= n; iter++)
			{
				for (int j = badRootSeam; j < afterCutLength; j++)
				{
					bool isAscending;
					int p = 0;
					while (mountain[badRootHeight][p].Position + badRootHeight < j) p++;
					if (mountain[badRootHeight][p].Position + badRootHeight == j)
					{
						while (true)
						{
							if (mountain[badRootHeight][p] == null || mountain[badRootHeight][p].Position + badRootHeight < badRootSeam)
							{
								isAscending = false;
								break;
							}
							if (mountain[badRootHeight][p].Position + badRootHeight == badRootSeam)
							{
								isAscending = true;
								break;
							}
							p = mountain[badRootHeight][p].ParentIndex;
						}
					}
					else
					{
						isAscending = false;
					}

					int seamHeight = afterCutHeight - 1;
					while (true)
					{
						int l = 0;
						while (mountain[seamHeight][l] != null && mountain[seamHeight][l].Position + seamHeight < j)
							l++;
						if (mountain[seamHeight][l] != null && mountain[seamHeight][l].Position + seamHeight == j)
							break;
						seamHeight--;
					}
					seamHeight++;

					bool isReplacingCut = (j == badRootSeam);

					if (isAscending)
					{
						for (int k = 0; k < seamHeight + (cutHeight - badRootHeight) * iter; k++)
						{
							if (result.Count <= k) result.Add(new List<MountainElement>());
							int sy, sx;
							int sourceParentIndex;
							int parentShifts = iter - (isReplacingCut ? 1 : 0);
							int parentPosition;
							int parentIndex;
							int valueFromDiagonal;

							if (k < badRootHeight)
							{
								sy = k;
								if (isReplacingCut)
									sx = mountain[sy].Count - 1;
								else
								{
									sx = 0;
									while (mountain[sy][sx].Position + sy < j) sx++;
								}
								sourceParentIndex = mountain[sy][sx].ParentIndex;
								parentPosition = (mountain[sy][sourceParentIndex] != null)
									? mountain[sy][sourceParentIndex].Position + parentShifts * (afterCutLength - badRootSeam) * (mountain[sy][sourceParentIndex].Position + sy >= badRootSeam ? 1 : 0) - (k - sy)
									: -1;
								parentIndex = 0;
								while (parentIndex < result[k].Count && result[k][parentIndex].Position < parentPosition) parentIndex++;
								if (parentIndex >= result[k].Count || result[k][parentIndex].Position != parentPosition)
									parentIndex = -1;
								valueFromDiagonal = (parentIndex == -1)
									? newDiagonalMountain[0][j + (afterCutLength - badRootSeam) * iter].Value
									: 0;
								result[k].Add(new MountainElement
								{
									Value = valueFromDiagonal,
									Position = j + (afterCutLength - badRootSeam) * iter - k,
									ParentIndex = parentIndex,
									ForcedParent = mountain[sy][sx].ForcedParent
								});
							}
							else if (k <= badRootHeight + (cutHeight - badRootHeight) * (iter - (isReplacingCut ? 1 : 0)))
							{
								sy = badRootHeight;
								if (!yamakazi && isReplacingCut)
									sx = mountain[sy].Count - 1;
								else
								{
									sx = 0;
									while (mountain[sy][sx].Position + sy < j) sx++;
								}
								sourceParentIndex = mountain[sy][sx].ParentIndex;
								parentPosition = (mountain[sy][sourceParentIndex] != null)
									? mountain[sy][sourceParentIndex].Position + parentShifts * (afterCutLength - badRootSeam) * (mountain[sy][sourceParentIndex].Position + sy >= badRootSeam ? 1 : 0) - (k - sy)
									: -1;
								parentIndex = 0;
								while (parentIndex < result[k].Count && result[k][parentIndex].Position < parentPosition) parentIndex++;
								if (parentIndex >= result[k].Count || result[k][parentIndex].Position != parentPosition)
									parentIndex = -1;
								valueFromDiagonal = (parentIndex == -1)
									? newDiagonalMountain[0][j + (afterCutLength - badRootSeam) * iter].Value
									: 0;
								result[k].Add(new MountainElement
								{
									Value = valueFromDiagonal,
									Position = j + (afterCutLength - badRootSeam) * iter - k,
									ParentIndex = parentIndex,
									ForcedParent = mountain[sy][sx].ForcedParent
								});
							}
							else if (isReplacingCut && k <= badRootHeight + (cutHeight - badRootHeight) * iter)
							{
								sy = k - (cutHeight - badRootHeight) * (iter - 1);
								if (!yamakazi && isReplacingCut)
									sx = mountain[sy].Count - 1;
								else
								{
									sx = 0;
									while (mountain[sy][sx].Position + sy < j) sx++;
								}
								sourceParentIndex = mountain[sy][sx].ParentIndex;
								parentPosition = (mountain[sy][sourceParentIndex] != null)
									? mountain[sy][sourceParentIndex].Position + parentShifts * (afterCutLength - badRootSeam) * (mountain[sy][sourceParentIndex].Position + sy >= badRootSeam ? 1 : 0) - (k - sy)
									: -1;
								parentIndex = 0;
								while (parentIndex < result[k].Count && result[k][parentIndex].Position < parentPosition) parentIndex++;
								if (parentIndex >= result[k].Count || result[k][parentIndex].Position != parentPosition)
									parentIndex = -1;
								valueFromDiagonal = (parentIndex == -1)
									? newDiagonalMountain[0][j + (afterCutLength - badRootSeam) * iter].Value
									: 0;
								result[k].Add(new MountainElement
								{
									Value = valueFromDiagonal,
									Position = j + (afterCutLength - badRootSeam) * iter - k,
									ParentIndex = parentIndex,
									ForcedParent = mountain[sy][sx].ForcedParent
								});
							}
							else
							{
								sy = k - (cutHeight - badRootHeight) * iter;
								if (!yamakazi && isReplacingCut)
									sx = mountain[sy].Count - 1;
								else
								{
									sx = 0;
									while (mountain[sy][sx].Position + sy < j) sx++;
								}
								sourceParentIndex = mountain[sy][sx].ParentIndex;
								parentPosition = (mountain[sy][sourceParentIndex] != null)
									? mountain[sy][sourceParentIndex].Position + parentShifts * (afterCutLength - badRootSeam) * (mountain[sy][sourceParentIndex].Position + sy >= badRootSeam ? 1 : 0) - (k - sy)
									: -1;
								parentIndex = 0;
								while (parentIndex < result[k].Count && result[k][parentIndex].Position < parentPosition) parentIndex++;
								if (parentIndex >= result[k].Count || result[k][parentIndex].Position != parentPosition)
									parentIndex = -1;
								valueFromDiagonal = (parentIndex == -1)
									? newDiagonalMountain[0][j + (afterCutLength - badRootSeam) * iter].Value
									: 0;
								result[k].Add(new MountainElement
								{
									Value = valueFromDiagonal,
									Position = j + (afterCutLength - badRootSeam) * iter - k,
									ParentIndex = parentIndex,
									ForcedParent = mountain[sy][sx].ForcedParent
								});
							}
						}
					}
					else
					{
						if (isReplacingCut)
							Console.WriteLine("Cut child and not connected to bad root. Makes sense.");
						for (int k = 0; k < seamHeight; k++)
						{
							if (result.Count <= k) result.Add(new List<MountainElement>());
							int sy = k;
							int sx;
							if (isReplacingCut)
								sx = mountain[sy].Count - 1;
							else
							{
								sx = 0;
								while (mountain[sy][sx].Position + sy < j) sx++;
							}
							int sourceParentIndex = mountain[sy][sx].ParentIndex;
							int parentShifts = iter - (isReplacingCut ? 1 : 0);
							int parentPosition = (mountain[sy][sourceParentIndex] != null)
								? mountain[sy][sourceParentIndex].Position + parentShifts * (afterCutLength - badRootSeam) * (mountain[sy][sourceParentIndex].Position + sy >= badRootSeam ? 1 : 0) - (k - sy)
								: -1;
							int parentIndex = 0;
							while (parentIndex < result[k].Count && result[k][parentIndex].Position < parentPosition) parentIndex++;
							if (parentIndex >= result[k].Count || result[k][parentIndex].Position != parentPosition)
								parentIndex = -1;
							int valueFromDiagonal = (parentIndex == -1)
								? newDiagonalMountain[0][j + (afterCutLength - badRootSeam) * iter].Value
								: 0;
							result[k].Add(new MountainElement
							{
								Value = valueFromDiagonal,
								Position = j + (afterCutLength - badRootSeam) * iter - k,
								ParentIndex = parentIndex,
								ForcedParent = mountain[sy][sx].ForcedParent
							});
						}
					}
				}
			}

			// 补齐数值
			for (int i = result.Count - 1; i >= 0; i--)
			{
				if (result[i].Count == 0)
				{
					result.RemoveAt(i);
					continue;
				}
				for (int j = 0; j < result[i].Count; j++)
				{
					if (!double.IsNaN(result[i][j].Value)) continue;
					int k = 0;
					while (result[i + 1][k].Position < result[i][j].Position - 1) k++;
					if (result[i + 1][k].Position != result[i][j].Position - 1)
						throw new InvalidOperationException("Mountain not complete");
					result[i][j].Value = result[i][result[i][j].ParentIndex].Value + result[i + 1][k].Value;
				}
			}

			return result;
		}
		#endregion
	}
}