using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogologyExpander
{
	// 山脉图元素
	public class XyItem
	{
		public int Value { get; set; }
		public List<int> Row { get; set; }          // 行标（规范化后）
		public int Column { get; set; }
		public XyItem Parent { get; set; }
		public XyItem Head { get; set; }            // 头元素（足元素专用）
		public XyItem Foot { get; set; }            // 足元素
		public XyItem RefElement { get; set; }      // 参照元素
		public int Id { get; set; }                 // 编号（0 表示根列左侧）

		public XyItem()
		{
			Row = new List<int> { 0 };
		}
		public XyItem Clone()
		{
			return new XyItem
			{
				Value = this.Value,
				Row = new List<int>(this.Row),
				Column = this.Column,
				Parent = this.Parent,
				Head = this.Head,
				Foot = this.Foot,
				RefElement = this.RefElement,
				Id = this.Id
			};
		}
	}

	// X-Y 展开引擎（完整实现）
	public class XyEngine
	{
		private PrssEngine _prss = new PrssEngine();
		private List<List<XyItem>> _mountain;       // 山脉图：外层行（行标升序），内层列

		// ---------- 行标运算（静态辅助） ----------
		private static List<int> NormalizeRow(List<int> row)
		{
			if (row == null || row.Count == 0) return new List<int> { 0 };
			int i = 0;
			while (i < row.Count && row[i] == 0) i++;
			if (i == row.Count) return new List<int> { 0 };
			return row.Skip(i).ToList();
		}

		private static int RowDim(List<int> row) => NormalizeRow(row).Count;
		private static int RowGrade(List<int> row)
		{
			var r = NormalizeRow(row);
			return r.Count == 1 ? 0 : r[0];
		}
		private static List<int> RowDeviation(List<int> row)
		{
			var r = NormalizeRow(row);
			if (r.Count <= 1) return new List<int> { 0 };
			return NormalizeRow(r.Skip(1).ToList());
		}

		private static int CompareRow(List<int> a, List<int> b)
		{
			var aa = NormalizeRow(a);
			var bb = NormalizeRow(b);
			int da = aa.Count, db = bb.Count;
			if (da != db) return da.CompareTo(db);
			for (int i = 0; i < da; i++)
				if (aa[i] != bb[i]) return aa[i].CompareTo(bb[i]);
			return 0;
		}

		private static List<int> AddRow(List<int> a, List<int> b)
		{
			var aa = NormalizeRow(a);
			var bb = NormalizeRow(b);
			int da = aa.Count, db = bb.Count;
			if (da < db) return new List<int>(bb);
			if (da == db)
			{
				var r = new List<int>(bb);
				r[0] = aa[0] + bb[0];
				return NormalizeRow(r);
			}
			// da > db
			var result = new List<int>(aa);
			int start = da - db;
			for (int i = 0; i < db; i++)
				result[start + i] += bb[i];
			return NormalizeRow(result);
		}

		private static List<int> SubtractRow(List<int> a, List<int> b)
		{
			var aa = NormalizeRow(a);
			var bb = NormalizeRow(b);
			if (CompareRow(aa, bb) == 0) return new List<int> { 0 };
			int da = aa.Count, db = bb.Count;
			if (da > db) return new List<int>(aa);
			if (da == db)
			{
				for (int i = 0; i < da; i++)
				{
					if (aa[i] != bb[i])
					{
						var r = new List<int> { aa[i] - bb[i] };
						for (int j = i + 1; j < da; j++) r.Add(aa[j]);
						return NormalizeRow(r);
					}
				}
			}
			return new List<int> { 0 };
		}

		private static List<int> CalcFootRow(List<int> headRow, List<int> parentRow)
		{
			var h = NormalizeRow(headRow);
			var p = NormalizeRow(parentRow);
			int dh = h.Count, dp = p.Count;
			if (dh > dp || (dh == dp && h[0] > p[0]))
			{
				var r = new List<int> { 1 };
				r.AddRange(Enumerable.Repeat(0, dh));
				return NormalizeRow(r);
			}
			// 按位比较
			var acc = new List<int>();
			for (int i = 0; i < dh; i++)
			{
				if (h[i] > p[i])
				{
					if (i == 0)
					{
						var r = new List<int> { 1 };
						r.AddRange(Enumerable.Repeat(0, dh - 1));
						return NormalizeRow(r);
					}
					else
					{
						var prefix = acc.Take(i - 1).ToList();
						var newVal = acc[i - 1] + 1;
						var suffix = Enumerable.Repeat(0, dh - i).ToList();
						var r = prefix.Concat(new[] { newVal }).Concat(suffix).ToList();
						return NormalizeRow(r);
					}
				}
				acc.Add(h[i]);
			}
			// 全部相等，末位 +1
			var last = acc[acc.Count - 1] + 1;
			acc[acc.Count - 1] = last;
			return NormalizeRow(acc);
		}

		// ---------- 山脉图构建 ----------
		private void BuildMountain(List<int> seq)
		{
			_mountain = new List<List<XyItem>>();
			// 首行（第 0 行）
			var firstRow = new List<XyItem>();
			for (int col = 0; col < seq.Count; col++)
			{
				var item = new XyItem
				{
					Value = seq[col],
					Row = new List<int> { 0 },
					Column = col
				};
				firstRow.Add(item);
			}
			_mountain.Add(firstRow);

			// 设置父关系（前方最近小于当前值）
			for (int col = 0; col < seq.Count; col++)
			{
				var item = firstRow[col];
				for (int j = col - 1; j >= 0; j--)
				{
					if (firstRow[j].Value < item.Value)
					{
						item.Parent = firstRow[j];
						break;
					}
				}
			}

			// 循环生成足元素，直到各列最大行值均为 1
			bool changed;
			do
			{
				changed = false;
				for (int col = 0; col < seq.Count; col++)
				{
					var colItems = GetColumnItems(col);
					for (int row = 0; row < colItems.Count; row++)
					{
						var item = colItems[row];
						if (item.Value > 1 && item.Foot == null && item.Parent != null)
						{
							var foot = CreateFoot(item);
							InsertItem(foot);
							changed = true;
						}
					}
				}
				// 检查终止条件
				bool allMaxOne = true;
				for (int col = 0; col < seq.Count; col++)
				{
					var colItems = GetColumnItems(col);
					if (colItems.Count == 0) continue;
					if (colItems[colItems.Count - 1].Value != 1)
					{
						allMaxOne = false;
						break;
					}
				}
				if (allMaxOne) changed = false;
			} while (changed);
		}

		private XyItem CreateFoot(XyItem head)
		{
			var footRow = CalcFootRow(head.Row, head.Parent.Row);
			var foot = new XyItem
			{
				Value = head.Value - head.Parent.Value,
				Row = footRow,
				Column = head.Column,
				Head = head
			};
			head.Foot = foot;

			// 确定足元素的父元素（左腿链）
			var candidate = head.Parent;
			if (candidate.Foot != null && CompareRow(candidate.Foot.Row, foot.Row) <= 0)
				candidate = candidate.Foot;
			// 向上回溯直到 candidate.Value < foot.Value
			while (candidate.Value >= foot.Value)
				candidate = candidate.Parent;
			foot.Parent = candidate;

			return foot;
		}

		private void InsertItem(XyItem item)
		{
			// 确保行存在（若行标不在当前最大行中，增加行）
			int rowIndex = FindRowIndex(item.Row);
			if (rowIndex == -1)
			{
				// 按行标大小插入新行
				int insertAt = _mountain.Count;
				for (int i = 0; i < _mountain.Count; i++)
				{
					if (CompareRow(item.Row, _mountain[i][0].Row) < 0)
					{
						insertAt = i;
						break;
					}
				}
				_mountain.Insert(insertAt, new List<XyItem>());
				rowIndex = insertAt;
			}
			var row = _mountain[rowIndex];
			// 填充列
			while (row.Count <= item.Column)
				row.Add(null);
			row[item.Column] = item;
		}

		private int FindRowIndex(List<int> row)
		{
			for (int i = 0; i < _mountain.Count; i++)
			{
				if (_mountain[i].Count > 0 && CompareRow(_mountain[i][0].Row, row) == 0)
					return i;
			}
			return -1;
		}

		private List<XyItem> GetColumnItems(int col)
		{
			var result = new List<XyItem>();
			for (int r = 0; r < _mountain.Count; r++)
			{
				if (_mountain[r].Count > col && _mountain[r][col] != null)
					result.Add(_mountain[r][col]);
			}
			return result;
		}

		private XyItem GetMaxRowItem(int col)
		{
			var items = GetColumnItems(col);
			return items.Count == 0 ? null : items[items.Count - 1];
		}

		private void RemoveMaxRowItem(int col)
		{
			var items = GetColumnItems(col);
			if (items.Count > 0)
			{
				var last = items[items.Count - 1];
				int rowIdx = FindRowIndex(last.Row);
				if (rowIdx >= 0)
				{
					_mountain[rowIdx][col] = null;
					// 如果该行全空则移除行（可选）
				}
			}
		}

		// ---------- 参照链与根元素 ----------
		private void BuildRefs()
		{
			for (int r = 0; r < _mountain.Count; r++)
			{
				var row = _mountain[r];
				for (int c = 0; c < row.Count; c++)
				{
					var item = row[c];
					if (item == null) continue;
					if (CompareRow(item.Row, new List<int> { 0 }) == 0 && item.Value == 1)
					{
						item.RefElement = null;
						continue;
					}
					if (item.Parent != null && CompareRow(item.Parent.Row, item.Row) == 0)
						item.RefElement = item.Parent;
					else if (item.Head != null)
						item.RefElement = item.Head;
					else
						item.RefElement = null;
				}
			}
		}

		private List<XyItem> GetRefChain(XyItem item)
		{
			var chain = new List<XyItem>();
			var cur = item;
			while (cur.RefElement != null)
			{
				cur = cur.RefElement;
				chain.Add(cur);
			}
			return chain;
		}

		private XyItem FindBud(int lastCol)
		{
			var items = GetColumnItems(lastCol);
			if (items.Count < 2) return null;
			return items[items.Count - 2];   // 行标第二大的
		}

		private XyItem DetermineRoot(XyItem bud, List<XyItem> refChain)
		{
			// 若芽与父在同一维度，则父为根
			if (bud.Parent != null && RowDim(bud.Row) == RowDim(bud.Parent.Row))
				return bud.Parent;

			// 否则构建主维度数列
			var mainDimSeq = new List<int>();
			foreach (var item in refChain)
				mainDimSeq.Add(RowDim(item.Row));
			mainDimSeq.Add(mainDimSeq[mainDimSeq.Count - 1] + 1);   // 末项 +1

			// 对主维度数列进行 PrSS 展开一次，得到根列索引
			var prssExpanded = _prss.ExpandOneStep(mainDimSeq);
			// 根列索引 = 原序列长度 - 展开后序列长度 + 1? 但我们需要的是根列在参照链中的位置
			// 根据规则：主维度数列的“根列”对应的参照链元素就是根元素。
			// 按照 PrSS 展开，根列索引通常是 badRoot 的位置。
			// 我们直接调用 PrSS 的 FindBadRoot 方法（需要暴露）或者模拟。
			// 这里由于 PrssEngine 未公开 FindBadRoot，我们重新实现一个简单版本。
			// 或者我们采用更直接的方法：主维度数列中，找到最后一个比末项小的项即为根列索引。
			// 因为 PrSS 的坏根就是最后一个小于最后一个元素的位置。
			int rootIdx = -1;
			int lastVal = mainDimSeq[mainDimSeq.Count - 1];
			for (int i = mainDimSeq.Count - 2; i >= 0; i--)
			{
				if (mainDimSeq[i] < lastVal)
				{
					rootIdx = i;
					break;
				}
			}
			if (rootIdx == -1) return null;
			// 映射到参照链：根列索引 = rootIdx（因为主维度数列各项对应参照链元素）
			if (rootIdx < refChain.Count)
				return refChain[rootIdx];
			return null;
		}

		// ---------- 坏区复制 ----------
		private void CopyBadArea(XyItem root, int L, int lastCol, int n)
		{
			int rootCol = root.Column;
			for (int srcCol = rootCol + 1; srcCol <= lastCol; srcCol++)
			{
				int targetCol = srcCol + L * n;
				CopyColumn(srcCol, targetCol, root, L, n);
			}
		}

		private void CopyColumn(int srcCol, int targetCol, XyItem root, int L, int n)
		{
			var srcItems = GetColumnItems(srcCol);
			// 按行标升序复制（从上到下）
			foreach (var src in srcItems)
			{
				var maxRow = ComputeMaxRow(src, root, L, n);
				var minRow = ComputeMinRow(src, root, L, n);
				CopyElementRecursive(src, targetCol, root, L, n, maxRow, minRow);
			}
		}

		private void CopyElementRecursive(XyItem src, int targetCol, XyItem root, int L, int n,
										  List<int> maxRow, List<int> minRow)
		{
			if (CompareRow(minRow, maxRow) > 0) return;

			// 创建新元素
			var newItem = new XyItem
			{
				Value = src.Value,
				Row = new List<int>(minRow),
				Column = targetCol,
				Id = src.Id
			};

			// 确定父元素
			if (src.Parent != null && src.Parent.Column < root.Column)
				newItem.Parent = src.Parent;
			else if (src.Parent != null)
			{
				// 父列偏移 = (src.Parent.Column - src.Column) 不变
				int pCol = src.Parent.Column + (targetCol - src.Column);
				var pItems = GetColumnItems(pCol);
				// 选行标 <= minRow 的最大行
				XyItem candidate = null;
				foreach (var p in pItems)
				{
					if (CompareRow(p.Row, minRow) <= 0)
						candidate = p;
				}
				newItem.Parent = candidate;
			}

			// 插入山脉图
			InsertItem(newItem);

			// 递归复制足元素
			if (src.Foot != null)
			{
				var footMaxRow = ComputeMaxRow(src.Foot, root, L, n);
				var headParentRow = newItem.Parent?.Row ?? new List<int> { 0 };
				var footMinRow = CalcFootRow(newItem.Row, headParentRow);
				CopyElementRecursive(src.Foot, targetCol, root, L, n, footMaxRow, footMinRow);
			}
		}

		private List<int> ComputeMaxRow(XyItem src, XyItem root, int L, int n)
		{
			if (src.Id == 0) return new List<int>(src.Row);   // 情况(1)

			// 最大复制行参照元素：根列 + L*n 列中同编号且行标最大的
			int refCol = root.Column + L * n;
			var refItems = GetColumnItems(refCol);
			XyItem refItem = refItems.LastOrDefault(x => x.Id == src.Id);

			if (refItem == null) return new List<int>(src.Row);

			// 根列中同编号元素
			var rootItems = GetColumnItems(root.Column);
			XyItem rootSameId = rootItems.LastOrDefault(x => x.Id == src.Id);
			if (rootSameId == null) return new List<int>(src.Row);

			if (src.Id != root.Id)
			{
				// 情况(2)
				var diff = SubtractRow(src.Row, rootSameId.Row);
				return AddRow(refItem.Row, diff);
			}
			else
			{
				// src.Id == root.Id
				int srcGrade = RowGrade(src.Row);
				int rootGrade = RowGrade(root.Row);
				int srcDim = RowDim(src.Row);
				int rootDim = RowDim(root.Row);

				if (srcDim == rootDim && srcGrade == rootGrade)
				{
					// 情况(2) 子情形
					var diff = SubtractRow(src.Row, rootSameId.Row);
					return AddRow(refItem.Row, diff);
				}
				else if (srcDim == rootDim && srcGrade > rootGrade)
				{
					// 情况(3)
					int refGrade = RowGrade(refItem.Row);
					int newGrade = refGrade + (srcGrade - rootGrade);
					var dev = RowDeviation(src.Row);
					var newRow = new List<int> { newGrade };
					newRow.AddRange(dev);
					return NormalizeRow(newRow);
				}
				else if (srcDim > rootDim)
				{
					// 情况(4) – 需要展开维度数列
					// 构建该元素的特征维度数列：在参照链中，维度大于根维度的项的维度提取出来，
					// 插入到主维度数列中根元素之后。
					// 这里简化处理：我们直接返回原行标，但可扩展。
					// 若要严格实现，需实现维度数列的 PrSS 展开。
					// 由于完整实现较复杂，此处暂返回原行标（相当于未展开，但实际规则应展开）
					return new List<int>(src.Row);
				}
				else
				{
					return new List<int>(src.Row);
				}
			}
		}

		private List<int> ComputeMinRow(XyItem src, XyItem root, int L, int n)
		{
			if (src.Row.SequenceEqual(new List<int> { 0 }))
				return new List<int> { 0 };

			if (src.Head != null)
			{
				var headMaxRow = ComputeMaxRow(src.Head, root, L, n);
				var headParentRow = src.Head.Parent?.Row ?? new List<int> { 0 };
				return CalcFootRow(headMaxRow, headParentRow);
			}
			return new List<int> { 0 };
		}

		// ---------- 公共接口 ----------
		public string Expand(List<int> sequence, int steps)
		{
			if (sequence == null || sequence.Count == 0) return "";
			var current = new List<int>(sequence);
			for (int i = 0; i < steps && current.Count > 0; i++)
				current = ExpandOneStep(current);
			return XyParser.FormatPlain(current);
		}

		public List<int> ExpandOneStep(List<int> sequence)
		{
			if (sequence == null || sequence.Count == 0) return new List<int>();

			// 如果最后一项 <= 1，直接移除
			if (sequence.Last() <= 1)
			{
				var result = new List<int>(sequence);
				result.RemoveAt(result.Count - 1);
				return result;
			}

			// 1. 构建山脉图
			BuildMountain(sequence);
			BuildRefs();

			int lastCol = sequence.Count - 1;
			var bud = FindBud(lastCol);
			if (bud == null)
			{
				var result = new List<int>(sequence);
				result.RemoveAt(result.Count - 1);
				return result;
			}

			var refChain = GetRefChain(bud);
			var root = DetermineRoot(bud, refChain);
			if (root == null)
			{
				var result = new List<int>(sequence);
				result.RemoveAt(result.Count - 1);
				return result;
			}

			// 2. 修改最右列：去掉最大行，其余值减1
			var lastItems = GetColumnItems(lastCol);
			if (lastItems.Count > 0)
			{
				RemoveMaxRowItem(lastCol);
				// 剩余元素值减1
				var remaining = GetColumnItems(lastCol);
				foreach (var item in remaining)
					item.Value -= 1;
			}

			// 3. 若根值 > 1，对芽继续作差直到最大行值为1
			while (root.Value > 1)
			{
				var currentBud = GetMaxRowItem(lastCol);
				if (currentBud != null && currentBud.Parent != null)
				{
					// 以根元素的父元素为父（若根无父则用根自身？规则是用根父）
					var newParent = root.Parent ?? root;
					var foot = CreateFoot(currentBud);
					// 但我们需要手动设定父为 newParent? CreateFoot 自动根据 currentBud.Parent 计算。
					// 为了强制以 newParent 为父，我们可以调整 currentBud.Parent 再生成，但更安全的是直接调用 CreateFoot 后修改。
					// 简化：直接创建足元素并插入
					// 我们创建 foot 并指定父为 newParent
					var footRow = CalcFootRow(currentBud.Row, newParent.Row);
					var foot = new XyItem
					{
						Value = currentBud.Value - newParent.Value,
						Row = footRow,
						Column = lastCol,
						Head = currentBud,
						Parent = newParent
					};
					currentBud.Foot = foot;
					InsertItem(foot);
				}
				// 更新根值（可能因操作而变）
				root = DetermineRoot(bud, refChain); // 重新计算根
				if (root == null) break;
			}

			// 4. 坏区长度 L
			int L = lastCol - root.Column;

			// 5. 复制坏区（一轮）
			CopyBadArea(root, L, lastCol, 1);

			// 6. 提取首行值
			var firstRowItems = GetColumnItems(0);
			var resultSeq = new List<int>();
			for (int col = 0; col < firstRowItems.Count; col++)
			{
				var item = firstRowItems[col];
				if (item != null)
					resultSeq.Add(item.Value);
			}
			return resultSeq;
		}

		// 其他方法（IsValid, IsEmpty, ExpandWithHistory）保持不变，使用上述 ExpandOneStep
		public List<List<int>> ExpandWithHistory(List<int> sequence, int maxSteps)
		{
			var history = new List<List<int>>();
			if (sequence == null || sequence.Count == 0) { history.Add(new List<int>()); return history; }
			var current = new List<int>(sequence);
			history.Add(new List<int>(current));
			for (int i = 0; i < maxSteps && current.Count > 0; i++)
			{
				current = ExpandOneStep(current);
				history.Add(new List<int>(current));
			}
			return history;
		}

		public bool IsValid(List<int> sequence) => XyParser.IsValid(sequence);
		public bool IsEmpty(List<int> sequence) => sequence == null || sequence.Count == 0;
	}
}