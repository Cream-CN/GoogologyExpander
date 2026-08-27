//修改自Naruyoko/StudyAndExpandSequence
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GoogologyExpander
{
	// ---------- 全局配置（对应原脚本全局变量） ----------
	public static class Config
	{
		public static int maxDimensions = 10;
		public static bool legBasedAscension = false;
		public static bool noLimitToN = false;
		public static bool automaticallyExpandOnChange = false;
		public static int autoTimeout = 0;
	}

	// ---------- 数据结构 Mountain ----------
	public class Mountain
	{
		public int dim;
		public List<Mountain> arr;          // 仅当 dim > 0 时有效
		public List<int> coord;
		public int value;                   // 仅当 dim == 0 时有效
		public int position;                // 仅当 dim == 0
		public int parentIndex;             // 仅当 dim == 0
		public bool forcedParent;           // 仅当 dim == 0
		public List<int> leftLegCoord;      // 仅当 dim == 0
		public List<int> rightLegCoord;     // 仅当 dim == 0

		public Mountain()
		{
			coord = new List<int>();
			arr = new List<Mountain>();
			leftLegCoord = null;
			rightLegCoord = null;
			dim = 0;
			value = 0;
			position = 0;
			parentIndex = -1;
			forcedParent = false;
		}

		public Mountain Clone()
		{
			return WY.CloneMountain(this);
		}
	}

	// ---------- 辅助函数（完全对应原JS） ----------
	public static class WY
	{
		public static List<int> AddVector(List<int> s, List<int> t)
		{
			var r = new List<int>();
			int len = Math.Max(s.Count, t.Count);
			for (int i = 0; i < len; i++)
				r.Add((i < s.Count ? s[i] : 0) + (i < t.Count ? t[i] : 0));
			return r;
		}

		public static bool EqualVector(List<int> s, List<int> t, int d = 0)
		{
			int len = Math.Max(s.Count, t.Count);
			for (int i = d; i < len; i++)
				if ((i < s.Count ? s[i] : 0) != (i < t.Count ? t[i] : 0))
					return false;
			return true;
		}

		public static List<int> StBasis(int d)
		{
			var r = new List<int>();
			while (r.Count < d) r.Add(0);
			r.Add(1);
			return r;
		}

		public static List<int> Basis(int d, int k)
		{
			var r = new List<int>();
			while (r.Count < d) r.Add(0);
			r.Add(k);
			return r;
		}

		public static List<int> IncrementCoord(List<int> s, int d)
		{
			var r = new List<int>(s);
			for (int i = 0; i < d; i++) r[i] = 0;
			return AddVector(r, StBasis(d));
		}

		public static List<int> AddCoord(List<int> s, int d, int k)
		{
			var r = new List<int>(s);
			for (int i = 0; i < d; i++) r[i] = 0;
			return AddVector(r, Basis(d, k));
		}

		public static int SumArray(List<int> s)
		{
			int r = 0;
			for (int i = 0; i < s.Count; i++) r += s[i];
			return r;
		}

		// ---------- 解析元素 ----------
		public class ParsedElement
		{
			public int value;
			public int position;
			public int parentIndex;
			public bool forcedParent;
			public string strexp;
		}

		public static ParsedElement ParseSequenceElement(string s, int i)
		{
			var el = new ParsedElement { position = i, parentIndex = -1, forcedParent = false };
			int idxV = s.IndexOf('v');
			if (idxV == -1 || !int.TryParse(s.Substring(idxV + 1), out int _))
			{
				el.value = int.Parse(s);
				return el;
			}
			else
			{
				el.value = int.Parse(s.Substring(0, idxV));
				el.parentIndex = Math.Max(Math.Min(i - 1, int.Parse(s.Substring(idxV + 1))), -1);
				el.forcedParent = true;
				return el;
			}
		}

		// ---------- 核心山脉操作（完整翻译） ----------
		public static Mountain CalcMountain(object s, int maxDim = int.MaxValue)
		{
			// 处理 coordOffset
			List<int> coordOffset = new List<int>();
			if (s is Mountain mIn && mIn.coord != null)
				coordOffset = new List<int>(mIn.coord);

			List<ParsedElement> seq = null;
			if (s is string str)
			{
				var parts = str.Split(new char[] { '\t', ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
				seq = new List<ParsedElement>();
				for (int i = 0; i < parts.Length; i++)
					seq.Add(ParseSequenceElement(parts[i], i));
			}
			else if (s is Mountain m)
			{
				// 如果传入的是 Mountain 且 arr 长度 <=1
				if (m.arr.Count <= 1)
				{
					var newM = new Mountain { dim = 1, coord = new List<int>(coordOffset) };
					if (m.dim == 0)
					{
						var node = new Mountain
						{
							dim = 0,
							value = m.value,
							position = m.position,
							coord = new List<int>(m.coord),
							parentIndex = m.parentIndex,
							forcedParent = m.forcedParent,
							leftLegCoord = m.leftLegCoord != null ? new List<int>(m.leftLegCoord) : null,
							rightLegCoord = m.rightLegCoord != null ? new List<int>(m.rightLegCoord) : null
						};
						newM.arr.Add(node);
					}
					else
					{
						if (m.arr.Count == 1)
						{
							var child = m.arr[0];
							var node = new Mountain
							{
								dim = 0,
								value = child.value,
								position = child.position,
								coord = new List<int>(child.coord),
								parentIndex = child.parentIndex,
								forcedParent = child.forcedParent,
								leftLegCoord = child.leftLegCoord != null ? new List<int>(child.leftLegCoord) : null,
								rightLegCoord = child.rightLegCoord != null ? new List<int>(child.rightLegCoord) : null
							};
							newM.arr.Add(node);
						}
					}
					return newM;
				}
				else
				{
					return m;
				}
			}

			if (seq == null)
				return new Mountain { dim = 1, coord = coordOffset };

			var mountain = new Mountain { dim = 1, coord = new List<int>(coordOffset) };
			for (int i = 0; i < seq.Count; i++)
			{
				var el = seq[i];
				var node = new Mountain
				{
					dim = 0,
					value = el.value,
					position = el.position,
					coord = AddCoord(coordOffset, 0, i),
					parentIndex = el.parentIndex,
					forcedParent = el.forcedParent,
					leftLegCoord = null,
					rightLegCoord = null
				};
				if (!el.forcedParent)
				{
					for (int j = i; j >= 0; j--)
					{
						if (seq[j].value < el.value)
						{
							node.parentIndex = j;
							break;
						}
					}
				}
				mountain.arr.Add(node);
			}

			int dimensions = 1;
			while (dimensions <= maxDim)
			{
				var uppers = CalcDifference(mountain);
				if (uppers.arr.Count < 1) break;
				var upperm = CalcMountain(uppers, dimensions);
				int upperdim = upperm.dim;
				var raisedupperm = upperm;
				while (raisedupperm.dim <= dimensions)
				{
					var newM = new Mountain { dim = raisedupperm.dim + 1, coord = new List<int>(raisedupperm.coord) };
					newM.arr.Add(raisedupperm);
					raisedupperm = newM;
				}
				raisedupperm.coord = new List<int>(coordOffset);
				raisedupperm.arr.Insert(0, mountain);
				mountain = raisedupperm;
				dimensions++;
			}
			return mountain;
		}

		public static Mountain CalcDifference(Mountain m)
		{
			var coordOffset = IncrementCoord(m.coord, m.dim);
			List<Mountain> rightLegs = new List<Mountain>();
			List<int> rightLegTree = new List<int>();
			List<int> rightLegPositions = new List<int>();

			if (m.dim == 1)
			{
				for (int i = 0; i < m.arr.Count; i++)
				{
					rightLegs.Add(m.arr[i]);
					rightLegTree.Add(m.arr[i].parentIndex);
					rightLegPositions.Add(SumArray(m.arr[i].coord));
				}
			}
			else
			{
				int lastPos = GetLastPosition(m);
				for (int i = 0; i <= lastPos; i++)
				{
					var node = FindHighestWithPosition(m, i);
					if (node != null) rightLegPositions.Add(i);
				}
				for (int i = 0; i < rightLegPositions.Count; i++)
				{
					var node = FindHighestWithPosition(m, rightLegPositions[i]);
					rightLegs.Add(node);
					var pn = node;
					while (pn != null)
					{
						var ppn = Parent(m, pn);
						if (ppn == null) ppn = LeftLeg(m, pn);
						if (ppn == null)
						{
							rightLegTree.Add(-1);
							break;
						}
						pn = ppn;
						if (pn.parentIndex == -1 && rightLegPositions.Contains(SumArray(pn.coord)))
						{
							rightLegTree.Add(rightLegPositions.IndexOf(SumArray(pn.coord)));
							break;
						}
					}
					if (pn == null) rightLegTree.Add(-1);
				}
			}

			List<int> rightLegInR = new List<int>();
			List<int> rInRightLeg = new List<int>();
			List<int> rightLegParents = new List<int>();
			var r = new Mountain { dim = 1, coord = coordOffset };

			for (int i = 0; i < rightLegs.Count; i++)
			{
				int pi = i;
				while (pi > -1 && !(rightLegs[pi].value < rightLegs[i].value && (rightLegs[pi].coord[m.dim - 1] < rightLegs[i].coord[m.dim - 1])))
					pi = rightLegTree[pi];
				rightLegParents.Add(pi);
				if (pi != -1)
				{
					rightLegInR.Add(r.arr.Count);
					rInRightLeg.Add(i);
					var node = new Mountain
					{
						dim = 0,
						value = rightLegs[i].value - rightLegs[pi].value,
						position = rightLegPositions[i],
						coord = AddCoord(coordOffset, 0, rightLegPositions[i] - SumArray(coordOffset)),
						parentIndex = -1,
						forcedParent = true,
						leftLegCoord = new List<int>(rightLegs[pi].coord),
						rightLegCoord = new List<int>(rightLegs[i].coord)
					};
					r.arr.Add(node);
				}
				else
				{
					rightLegInR.Add(-1);
				}
			}

			for (int i = 0; i < r.arr.Count; i++)
			{
				int pi = rInRightLeg[i];
				while (true)
				{
					int ppi = rightLegParents[pi];
					if (ppi == -1 || rightLegInR[ppi] == -1) break;
					pi = ppi;
					if (r.arr[rightLegInR[pi]].value < r.arr[i].value)
					{
						r.arr[i].parentIndex = rightLegInR[pi];
						break;
					}
				}
			}
			return r;
		}

		// ---------- 山脉查找函数 ----------
		public static List<int> IndexFromCoord(Mountain m, List<int> coord, int d = 0)
		{
			var r = new List<int>();
			while (true)
			{
				if (m.dim <= d)
				{
					if (EqualVector(m.coord, coord, d)) return r;
					else return null;
				}
				if (m.dim == 1)
				{
					for (int i = 0; i < m.arr.Count + 1; i++)
					{
						if (i == m.arr.Count) return null;
						if (m.arr[i].coord[0] == coord[0])
						{
							r.Add(i);
							m = m.arr[i];
							break;
						}
					}
				}
				else
				{
					int i = coord[m.dim - 1];
					if (i >= m.arr.Count) return null;
					r.Add(i);
					m = m.arr[i];
				}
			}
		}

		public static Mountain FindByIndex(Mountain m, List<int> index)
		{
			if (index == null) return null;
			for (int i = 0; i < index.Count; i++)
			{
				int idx = index[i];
				if (idx < 0) idx = m.arr.Count + idx;
				if (idx < 0 || idx >= m.arr.Count) return null;
				m = m.arr[idx];
			}
			return m;
		}

		public static Mountain FindByCoord(Mountain m, List<int> coord, int d = 0)
		{
			var idx = IndexFromCoord(m, coord, d);
			return FindByIndex(m, idx);
		}

		public static int GetLastPosition(Mountain m)
		{
			while (m.dim > 1) m = m.arr[0];
			return m.arr[m.arr.Count - 1].position;
		}

		public static Mountain FindHighestWithPosition(Mountain m, int position)
		{
			if (m.dim == 0)
			{
				if (m.position == position) return m;
				else return null;
			}
			if (m.arr.Count == 0) return null;
			if (m.dim == 1)
			{
				int min = 0, max = m.arr.Count - 1;
				if (m.arr[min].position > position || m.arr[max].position < position) return null;
				if (m.arr[min].position == position) return m.arr[min];
				if (m.arr[max].position == position) return m.arr[max];
				while (min != max)
				{
					int mid = (min + max) / 2;
					if (m.arr[mid].position == position) return m.arr[mid];
					else if (min == mid) return null;
					else if (m.arr[mid].position < position) min = mid;
					else if (m.arr[mid].position > position) max = mid;
				}
				return null;
			}
			else
			{
				for (int i = m.arr.Count - 1; i >= 0; i--)
				{
					var lowestRow = m.arr[i];
					while (lowestRow != null && lowestRow.dim > 1) lowestRow = lowestRow.arr[0];
					if (lowestRow == null) continue;
					var node = FindHighestWithPosition(lowestRow, position);
					if (node != null)
					{
						if (m.dim == 2) return node;
						else return FindHighestWithPosition(m.arr[i], position);
					}
				}
				return null;
			}
		}

		public static Mountain Parent(Mountain m, Mountain node)
		{
			if (node.dim != 0 || node.parentIndex == -1) return null;
			var idx = IndexFromCoord(m, node.coord);
			if (idx == null) return null;
			idx[idx.Count - 1] = node.parentIndex;
			return FindByIndex(m, idx);
		}

		public static Mountain LeftLeg(Mountain m, Mountain node)
		{
			if (node.dim != 0 || node.leftLegCoord == null) return null;
			return FindByCoord(m, node.leftLegCoord);
		}

		public static Mountain RightLeg(Mountain m, Mountain node)
		{
			if (node.dim != 0 || node.rightLegCoord == null) return null;
			return FindByCoord(m, node.rightLegCoord);
		}

		public static Mountain FindAbove(Mountain m, Mountain node)
		{
			if (node.dim != 0) return null;
			var idx = IndexFromCoord(m, node.coord);
			if (idx == null) return null;
			for (int i = idx.Count - 1; i > 0; i--)
			{
				idx[i] = 0;
				idx[i - 1]++;
				idx[idx.Count - 1] = node.position - SumArray(idx.Take(idx.Count - 1).ToList());
				var candidate = FindByIndex(m, idx.Take(i).ToList());
				if (candidate != null)
				{
					var c = FindByIndex(m, idx);
					if (c != null) return c;
				}
			}
			return null;
		}

		public static Dictionary<string, Mountain> FlattenMountain(Mountain m)
		{
			var r = new Dictionary<string, Mountain>();
			if (m.dim == 0)
			{
				r[string.Join(",", m.coord)] = m;
			}
			else
			{
				foreach (var child in m.arr)
				{
					var sub = FlattenMountain(child);
					foreach (var kv in sub)
						r[kv.Key] = kv.Value;
				}
			}
			return r;
		}

		public static Mountain CloneMountain(Mountain m)
		{
			var nm = new Mountain { dim = m.dim, coord = new List<int>(m.coord) };
			if (m.dim == 0)
			{
				nm.value = m.value;
				nm.position = m.position;
				nm.parentIndex = m.parentIndex;
				nm.forcedParent = m.forcedParent;
				nm.leftLegCoord = m.leftLegCoord != null ? new List<int>(m.leftLegCoord) : null;
				nm.rightLegCoord = m.rightLegCoord != null ? new List<int>(m.rightLegCoord) : null;
			}
			else
			{
				nm.arr = m.arr.Select(CloneMountain).ToList();
			}
			return nm;
		}

		public static Mountain GetBadRoot(Mountain s)
		{
			var mountain = s;
			return LeftLeg(mountain, FindHighestWithPosition(mountain, GetLastPosition(mountain)));
		}

		public static Mountain FilterEmpty(Mountain m)
		{
			if (m.dim > 0)
			{
				for (int i = m.arr.Count - 1; i >= 0; i--)
				{
					FilterEmpty(m.arr[i]);
					if (m.arr[i].dim > 0 && m.arr[i].arr.Count == 0)
						m.arr.RemoveAt(i);
				}
			}
			return m;
		}

		public static Mountain FindHighestWithPositionBelow(Mountain m, Mountain sub, int position)
		{
			var crawlIndex = IndexFromCoord(m, sub.coord, sub.dim);
			while (true)
			{
				crawlIndex[crawlIndex.Count - 1]--;
				while (crawlIndex.Count > 0 && crawlIndex[crawlIndex.Count - 1] < 0)
				{
					crawlIndex.RemoveAt(crawlIndex.Count - 1);
					if (crawlIndex.Count > 0)
						crawlIndex[crawlIndex.Count - 1]--;
				}
				if (crawlIndex.Count == 0) break;
				var r = FindHighestWithPosition(FindByIndex(m, crawlIndex), position);
				if (r != null) return r;
			}
			return null;
		}

		// ---------- 主要展开函数（完整翻译） ----------
		public static object Expand(object s, int n, bool legBasedAscension = false, bool stringify = true)
		{
			Mountain mountain;
			if (s is string str)
				mountain = CalcMountain(str);
			else if (s is Mountain m)
				mountain = m;
			else
				throw new ArgumentException("Invalid input");

			var result = CloneMountain(mountain);
			var badRoot = GetBadRoot(mountain);
			int cutPosition = GetLastPosition(mountain);
			var topCut = FindHighestWithPosition(mountain, cutPosition);
			var cutLookup = topCut;
			while (cutLookup != null)
			{
				var parentRow = FindByCoord(result, cutLookup.coord, 1);
				if (parentRow != null && parentRow.arr.Count > 0)
					parentRow.arr.RemoveAt(parentRow.arr.Count - 1);
				cutLookup = RightLeg(result, cutLookup);
			}
			FilterEmpty(result);

			List<Tuple<Mountain, Mountain, Mountain, int, bool>> belowCopyStackBase = new List<Tuple<Mountain, Mountain, Mountain, int, bool>>();
			List<Tuple<Mountain, Mountain>> aboveCopyStackBase = new List<Tuple<Mountain, Mountain>>();

			int badRootPosition = 0;
			if (badRoot != null)
			{
				badRootPosition = badRoot.position;
				var badRootRow = FindByCoord(mountain, badRoot.coord, 1);
				var bottomCut = mountain;
				while (bottomCut.dim > 1) bottomCut = bottomCut.arr[0];
				bottomCut = bottomCut.arr[bottomCut.arr.Count - 1];

				var topCutIndex = IndexFromCoord(mountain, topCut.coord);
				var crawlIndex = topCutIndex.Take(topCutIndex.Count - 1).ToList();

				while (true)
				{
					crawlIndex[crawlIndex.Count - 1]--;
					while (crawlIndex.Count > 0 && crawlIndex[crawlIndex.Count - 1] < 0)
					{
						crawlIndex.RemoveAt(crawlIndex.Count - 1);
						if (crawlIndex.Count > 0)
							crawlIndex[crawlIndex.Count - 1]--;
					}
					if (crawlIndex.Count == 0) break;
					var sourceSub = FindByIndex(mountain, crawlIndex);
					var destSub = FindByIndex(result, crawlIndex);
					belowCopyStackBase.Add(Tuple.Create(sourceSub, destSub, (Mountain)null, 0, false));
				}

				crawlIndex = topCutIndex.Take(topCutIndex.Count - 1).ToList();
				if (IndexFromCoord(result, FindByIndex(mountain, crawlIndex).coord, 1) != null)
				{
					while (true)
					{
						var sourceSub = FindByIndex(mountain, crawlIndex);
						var destSub = FindByIndex(result, crawlIndex);
						aboveCopyStackBase.Add(Tuple.Create(sourceSub, destSub));
						crawlIndex[crawlIndex.Count - 1]++;
						while (crawlIndex.Count > 0 && crawlIndex[crawlIndex.Count - 1] >= FindByIndex(mountain, crawlIndex.Take(crawlIndex.Count - 1).ToList()).arr.Count)
						{
							crawlIndex.RemoveAt(crawlIndex.Count - 1);
							if (crawlIndex.Count > 0)
								crawlIndex[crawlIndex.Count - 1]++;
						}
						if (crawlIndex.Count == 0) break;
					}
				}
			}

			// 缓存
			Dictionary<string, Mountain> subCutCache = new Dictionary<string, Mountain>();
			Dictionary<string, Mountain> subBadRootCache = new Dictionary<string, Mountain>();
			Dictionary<string, Mountain> subBadRootRowCache = new Dictionary<string, Mountain>();
			Dictionary<string, Mountain> topNodeCache = new Dictionary<string, Mountain>();
			Dictionary<string, bool> isAscendingCache = new Dictionary<string, bool>();

			Mountain nodeBelow = null;

			for (int i = 0; i <= n && badRoot != null; i++)
			{
				for (int x = (i == 0 ? cutPosition : badRootPosition + 1); x < cutPosition + (i < n ? 1 : 0); x++)
				{
					nodeBelow = null;
					var belowStack = new List<Tuple<Mountain, Mountain, Mountain, int, bool>>(belowCopyStackBase);
					while (belowStack.Count > 0)
					{
						var pop = belowStack[belowStack.Count - 1];
						belowStack.RemoveAt(belowStack.Count - 1);
						var sourceSub = pop.Item1;
						var destSub = pop.Item2;
						var cleanCopySource = pop.Item3;
						int cleanCopyOffset = pop.Item4;
						bool ignoreBelow = pop.Item5;

						string sourceID = string.Join(",", sourceSub.coord) + "," + sourceSub.dim;
						Mountain subCut;
						Mountain subBadRoot;
						Mountain subBadRootRow;
						if (!subCutCache.ContainsKey(sourceID))
						{
							subCut = FindHighestWithPosition(sourceSub, cutPosition);
							subBadRoot = FindHighestWithPosition(sourceSub, badRootPosition);
							subBadRootRow = subBadRoot != null ? FindByCoord(sourceSub, subBadRoot.coord, 1) : null;
							subCutCache[sourceID] = subCut;
							subBadRootCache[sourceID] = subBadRoot;
							subBadRootRowCache[sourceID] = subBadRootRow;
						}
						else
						{
							subCut = subCutCache[sourceID];
							subBadRoot = subBadRootCache[sourceID];
							subBadRootRow = subBadRootRowCache[sourceID];
						}

						string posID = sourceID + "," + x;
						Mountain topNode;
						bool isAscending;
						if (!topNodeCache.ContainsKey(posID))
						{
							topNode = FindHighestWithPosition(sourceSub, x);
							topNodeCache[posID] = topNode;
							if (topNode == null) continue;
							if (legBasedAscension)
							{
								var nodeInSubBadRootRow = subBadRootRow != null ? FindHighestWithPosition(subBadRootRow, x) : null;
								while (nodeInSubBadRootRow != null && nodeInSubBadRootRow.position > badRootPosition)
								{
									int leftPos = nodeInSubBadRootRow.leftLegCoord != null ? SumArray(nodeInSubBadRootRow.leftLegCoord) : nodeInSubBadRootRow.position - 1;
									nodeInSubBadRootRow = FindHighestWithPosition(subBadRootRow, leftPos);
								}
								isAscending = nodeInSubBadRootRow != null && nodeInSubBadRootRow.position == badRootPosition;
							}
							else
							{
								var referenceRow = (subBadRootRow != null && subBadRootRow.coord.Count > 1 && FindByCoord(sourceSub, AddCoord(subBadRootRow.coord, 1, -1), 1) != null) ? FindByCoord(sourceSub, AddCoord(subBadRootRow.coord, 1, -1), 1) : subBadRootRow;
								var nodeInRef = referenceRow != null ? FindHighestWithPosition(referenceRow, x) : null;
								while (nodeInRef != null && nodeInRef.position > badRootPosition)
									nodeInRef = Parent(referenceRow, nodeInRef);
								isAscending = nodeInRef != null && nodeInRef.position == badRootPosition;
							}
							isAscendingCache[posID] = isAscending;
						}
						else
						{
							topNode = topNodeCache[posID];
							if (topNode == null) continue;
							isAscending = isAscendingCache[posID];
						}

						if (sourceSub.dim == 1)
						{
							int position = x + (cutPosition - badRootPosition) * i;
							Mountain sourceNode;
							if (cleanCopySource != null)
							{
								sourceNode = FindHighestWithPosition(cleanCopySource, x);
								int sourceLeftPos = sourceNode.leftLegCoord != null ? SumArray(sourceNode.leftLegCoord) : x - 1;
								int leftPos = sourceLeftPos >= badRootPosition ? sourceLeftPos + (cutPosition - badRootPosition) * i : sourceLeftPos;
								var nodeLeftDown = FindHighestWithPositionBelow(result, destSub, leftPos);
								var leftLegCoord = nodeLeftDown != null ? nodeLeftDown.coord : null;
								var rightLegCoord = nodeBelow != null ? nodeBelow.coord : null;
								if (nodeBelow != null)
								{
									if (leftLegCoord != null && EqualVector(leftLegCoord, rightLegCoord, 1))
									{
										var leftIdx = IndexFromCoord(result, leftLegCoord);
										nodeBelow.parentIndex = leftIdx[leftIdx.Count - 1];
									}
									else
									{
										nodeBelow.parentIndex = -1;
									}
								}
								var newNode = new Mountain
								{
									dim = 0,
									value = int.MinValue,
									position = position,
									coord = AddCoord(destSub.coord, 0, position - SumArray(destSub.coord)),
									parentIndex = -1,
									forcedParent = sourceNode.forcedParent,
									leftLegCoord = leftLegCoord,
									rightLegCoord = rightLegCoord
								};
								destSub.arr.Add(newNode);
								nodeBelow = newNode;
							}
							else
							{
								sourceNode = FindHighestWithPosition(sourceSub, x);
								int sourceLeftPos = sourceNode.leftLegCoord != null ? SumArray(sourceNode.leftLegCoord) : -1;
								int leftPos = sourceLeftPos >= badRootPosition ? sourceLeftPos + (cutPosition - badRootPosition) * i : sourceLeftPos;
								var nodeLeftDown = FindHighestWithPositionBelow(result, destSub, leftPos);
								var leftLegCoord = nodeLeftDown != null ? nodeLeftDown.coord : null;
								var rightLegCoord = nodeBelow != null ? nodeBelow.coord : null;
								if (nodeBelow != null)
								{
									if (leftLegCoord != null && EqualVector(leftLegCoord, rightLegCoord, 1))
									{
										var leftIdx = IndexFromCoord(result, leftLegCoord);
										nodeBelow.parentIndex = leftIdx[leftIdx.Count - 1];
									}
									else
									{
										nodeBelow.parentIndex = -1;
									}
								}
								var newNode = new Mountain
								{
									dim = 0,
									value = int.MinValue,
									position = position,
									coord = AddCoord(destSub.coord, 0, position - SumArray(destSub.coord)),
									parentIndex = -1,
									forcedParent = sourceNode.forcedParent,
									leftLegCoord = leftLegCoord,
									rightLegCoord = rightLegCoord
								};
								destSub.arr.Add(newNode);
								nodeBelow = newNode;
							}
						}
						else
						{
							int subCutHeight = subCut != null ? subCut.coord[sourceSub.dim - 1] : 0;
							int subBadRootHeight = subBadRoot != null ? subBadRoot.coord[sourceSub.dim - 1] : 0;
							int topNodeHeight = topNode.coord[sourceSub.dim - 1];

							if (isAscending)
							{
								if (cleanCopySource != null)
								{
									int generationsFromSubBadRoot = 0;
									var nodeInClean = FindHighestWithPosition(cleanCopySource, x);
									if (nodeInClean.leftLegCoord != null)
									{
										var lowAncestor = nodeInClean;
										while (lowAncestor.position > badRootPosition)
										{
											lowAncestor = FindHighestWithPosition(cleanCopySource, SumArray(lowAncestor.leftLegCoord));
											generationsFromSubBadRoot++;
										}
									}
									else
									{
										generationsFromSubBadRoot = x - badRootPosition;
									}
									var lastReplacedCut = FindHighestWithPosition(destSub, badRootPosition + (cutPosition - badRootPosition) * i);
									int lastReplacedCutHeight = lastReplacedCut != null ? lastReplacedCut.coord[sourceSub.dim - 1] : 0;
									int targetHeight = i == 0 ? topNodeHeight : lastReplacedCutHeight + generationsFromSubBadRoot - cleanCopyOffset;
									if (ignoreBelow)
									{
										while (destSub.arr.Count < targetHeight + 1)
											destSub.arr.Add(new Mountain { dim = destSub.dim - 1, coord = AddCoord(destSub.coord, destSub.dim - 1, destSub.arr.Count) });
										for (int j = targetHeight; j >= 0; j--)
										{
											belowStack.Add(Tuple.Create(sourceSub.arr[subBadRootHeight], destSub.arr[j], cleanCopySource, Math.Max(j - lastReplacedCutHeight + cleanCopyOffset, 0), true));
										}
									}
									else
									{
										if (lastReplacedCut == null || cleanCopyOffset != 0) throw new Exception("Something went wrong");
										while (destSub.arr.Count < targetHeight + 1)
											destSub.arr.Add(new Mountain { dim = destSub.dim - 1, coord = AddCoord(destSub.coord, destSub.dim - 1, destSub.arr.Count) });
										for (int j = targetHeight; j >= 0; j--)
										{
											if (j < subBadRootHeight)
												belowStack.Add(Tuple.Create(sourceSub.arr[j], destSub.arr[j], (Mountain)null, 0, false));
											else
												belowStack.Add(Tuple.Create(sourceSub.arr[subBadRootHeight], destSub.arr[j], cleanCopySource, Math.Max(j - lastReplacedCutHeight + cleanCopyOffset, 0), j > subBadRootHeight));
										}
									}
								}
								else
								{
									if (cleanCopyOffset != 0) throw new Exception("Something went wrong");
									if (ignoreBelow)
									{
										var lastReplacedCut = FindHighestWithPosition(destSub, badRootPosition + (cutPosition - badRootPosition) * i);
										int lastReplacedCutHeight = lastReplacedCut != null ? lastReplacedCut.coord[sourceSub.dim - 1] : 0;
										if (lastReplacedCut == null && cleanCopyOffset != 0) throw new Exception("Something went wrong");
										int targetHeight = i == 0 ? topNodeHeight : lastReplacedCutHeight + topNodeHeight;
										while (destSub.arr.Count < targetHeight - subBadRootHeight + 1)
											destSub.arr.Add(new Mountain { dim = destSub.dim - 1, coord = AddCoord(destSub.coord, destSub.dim - 1, destSub.arr.Count) });
										for (int j = targetHeight; j >= subBadRootHeight; j--)
										{
											if (j < lastReplacedCutHeight + subBadRootHeight + (sourceSub.dim == 2 ? 1 : 0))
												belowStack.Add(Tuple.Create(sourceSub.arr[subBadRootHeight], destSub.arr[j - subBadRootHeight], subBadRootRow, 0, true));
											else
												belowStack.Add(Tuple.Create(sourceSub.arr[j - lastReplacedCutHeight], destSub.arr[j - subBadRootHeight], (Mountain)null, 0, j == lastReplacedCutHeight + subBadRootHeight));
										}
									}
									else
									{
										while (destSub.arr.Count < topNodeHeight + (subCutHeight - subBadRootHeight) * i + 1)
											destSub.arr.Add(new Mountain { dim = destSub.dim - 1, coord = AddCoord(destSub.coord, destSub.dim - 1, destSub.arr.Count) });
										for (int j = topNodeHeight + (subCutHeight - subBadRootHeight) * i; j >= 0; j--)
										{
											if (j < subBadRootHeight)
												belowStack.Add(Tuple.Create(sourceSub.arr[j], destSub.arr[j], (Mountain)null, 0, false));
											else if (j < subBadRootHeight + (subCutHeight - subBadRootHeight) * i + (sourceSub.dim == 2 ? 1 : 0))
												belowStack.Add(Tuple.Create(sourceSub.arr[subBadRootHeight], destSub.arr[j], subBadRootRow, 0, j > subBadRootHeight));
											else
												belowStack.Add(Tuple.Create(sourceSub.arr[j - (subCutHeight - subBadRootHeight) * i], destSub.arr[j], (Mountain)null, 0, i != 0 && j == subBadRootHeight + (subCutHeight - subBadRootHeight) * i));
										}
									}
								}
							}
							else
							{
								if (cleanCopySource != null || cleanCopyOffset != 0 || ignoreBelow) throw new Exception("Something went wrong");
								while (destSub.arr.Count < topNodeHeight + 1)
									destSub.arr.Add(new Mountain { dim = destSub.dim - 1, coord = AddCoord(destSub.coord, destSub.dim - 1, destSub.arr.Count) });
								for (int j = topNodeHeight; j >= 0; j--)
									belowStack.Add(Tuple.Create(sourceSub.arr[j], destSub.arr[j], (Mountain)null, 0, false));
							}
						}
					}

					// 上方复制
					int aboveCopySourceX = x == cutPosition ? badRootPosition : x;
					var aboveStack = new List<Tuple<Mountain, Mountain>>(aboveCopyStackBase);
					while (aboveStack.Count > 0)
					{
						var pop = aboveStack[aboveStack.Count - 1];
						aboveStack.RemoveAt(aboveStack.Count - 1);
						var sourceSub = pop.Item1;
						var destSub = pop.Item2;
						var topNode = FindHighestWithPosition(sourceSub, aboveCopySourceX);
						if (topNode == null) continue;
						if (sourceSub.dim == 1)
						{
							int position = x + (cutPosition - badRootPosition) * i;
							var nodeInSource = topNode;
							int sourceLeftPos = nodeInSource.leftLegCoord != null ? SumArray(nodeInSource.leftLegCoord) : -1;
							int leftPos = sourceLeftPos >= badRootPosition ? sourceLeftPos + (cutPosition - badRootPosition) * i : sourceLeftPos;
							var nodeLeftDown = FindHighestWithPositionBelow(result, destSub, leftPos);
							var leftLegCoord = nodeLeftDown != null ? nodeLeftDown.coord : null;
							var rightLegCoord = nodeBelow != null ? nodeBelow.coord : null;
							if (nodeBelow != null)
							{
								if (leftLegCoord != null && EqualVector(leftLegCoord, rightLegCoord, 1))
								{
									var leftIdx = IndexFromCoord(result, leftLegCoord);
									nodeBelow.parentIndex = leftIdx[leftIdx.Count - 1];
								}
								else
								{
									nodeBelow.parentIndex = -1;
								}
							}
							var newNode = new Mountain
							{
								dim = 0,
								value = int.MinValue,
								position = position,
								coord = AddCoord(destSub.coord, 0, position - SumArray(destSub.coord)),
								parentIndex = -1,
								forcedParent = nodeInSource.forcedParent,
								leftLegCoord = leftLegCoord,
								rightLegCoord = rightLegCoord
							};
							destSub.arr.Add(newNode);
							nodeBelow = newNode;
						}
						else
						{
							int topNodeHeight = topNode.coord[sourceSub.dim - 1];
							for (int j = topNodeHeight; j >= 0; j--)
								aboveStack.Add(Tuple.Create(sourceSub.arr[j], destSub.arr[j]));
						}
					}
				}
			}

			// 计算值
			var lastBottomNode = result;
			while (lastBottomNode != null && lastBottomNode.dim > 0)
			{
				if (lastBottomNode.dim == 1)
					lastBottomNode = lastBottomNode.arr[lastBottomNode.arr.Count - 1];
				else
					lastBottomNode = lastBottomNode.arr[0];
			}
			int resultLength = lastBottomNode != null ? lastBottomNode.position : 0;

			for (int x = 0; x <= resultLength; x++)
			{
				var node = FindHighestWithPosition(result, x);
				Mountain aboveNode = null;
				while (node != null)
				{
					if (node.value == int.MinValue)
					{
						if (aboveNode != null)
						{
							var pseudoParent = LeftLeg(result, aboveNode);
							if (pseudoParent == null) throw new Exception("Mountain not complete");
							node.value = pseudoParent.value + aboveNode.value;
						}
						else
						{
							node.value = 1;
						}
					}
					aboveNode = node;
					node = RightLeg(result, node);
				}
			}

			if (stringify)
			{
				var sb = new StringBuilder();
				if (result.arr.Count > 0)
				{
					var bottomRow = result;
					while (bottomRow.dim > 1) bottomRow = bottomRow.arr[0];
					for (int i = 0; i < bottomRow.arr.Count; i++)
					{
						if (i > 0) sb.Append(',');
						var el = bottomRow.arr[i];
						sb.Append(el.value);
						if (el.forcedParent)
							sb.Append("v").Append(el.parentIndex);
					}
				}
				return sb.ToString();
			}
			else
			{
				return result;
			}
		}

		// ---------- 多次展开（用于内部，完整保留） ----------
		public static string ExpandMulti(string s, string nstring, bool legBasedAscension)
		{
			var result = CalcMountain(s, Config.maxDimensions);
			if (result.dim > Config.maxDimensions)
			{
				int lastPos = GetLastPosition(result);
				for (int x = 0; x <= lastPos; x++)
					if (FindHighestWithPosition(result, x).value != 1)
						return "Aborted: Maximum dimensions reached.";
			}
			foreach (var part in nstring.Split(','))
			{
				int n = int.Parse(part);
				result = (Mountain)Expand(result, n, legBasedAscension, false);
			}
			return (string)Expand(result, 0, legBasedAscension, true);
		}

		// ---------- 对外接口 ExpandWY ----------
		public static int[] ExpandWY(int[] sequence)
		{
			if (sequence == null)
				throw new ArgumentNullException(nameof(sequence));

			if (sequence.Length == 0)
				return Array.Empty<int>();

			string seq = string.Join(",", sequence);
			string resultStr;
			try
			{
			resultStr = (string)Expand(seq, 1, false, true);
			}
			catch (Exception ex) when (ex is ArgumentOutOfRangeException || ex is InvalidOperationException || ex is IndexOutOfRangeException)
			{
				// 当前结构无法展开，回退为原序列
				return (int[])sequence.Clone();
			}
			if (string.IsNullOrEmpty(resultStr)) return new int[0];
			var parts = resultStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			int[] result = new int[parts.Length];
			for (int i = 0; i < parts.Length; i++)
			{
				int idxV = parts[i].IndexOf('v');
				if (idxV != -1)
					parts[i] = parts[i].Substring(0, idxV);
				result[i] = int.Parse(parts[i]);
			}
			return result;
		}
	}
}