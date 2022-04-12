using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using BnsBinTool.Core.Abstractions;
using BnsBinTool.Core.DataStructs;
using BnsBinTool.Core.Helpers;

namespace BnsBinTool.Core.Models
{
    public class NameTable
    {
        public class Rebuilder
        {
            private class Node
            {
                public Dictionary<string, Node> Children { get; init; }
                public uint Begin { get; set; }
                public uint End { get; set; }
                public bool IsLeaf => (Begin & 1) == 0;

                public Node()
                {
                    Children = new Dictionary<string, Node>();
                }

                public Node(Ref r)
                {
                    var v = ((ulong) r << 1) | 1;
                    Begin = (uint) v;
                    End = (uint) (v >> 32);
                }

                public override string ToString()
                {
                    return $"{Begin >> 1}-{End} IsLeaf:{IsLeaf}";
                }
            }

            private readonly NameTable _target;
            private readonly Node _rootNode = new Node();

            internal Rebuilder(NameTable target)
            {
                ArgGuard.ThrowIfNull(target, nameof(target));
                _target = target;
            }

            public Rebuilder AddTable<T>(string tableprefix, ICollection<T> records) where T : IHaveAlias, IRecord
            {
                tableprefix = tableprefix.ToLowerInvariant();

                foreach (var record in records)
                {
                    if (string.IsNullOrWhiteSpace(record.Alias))
                        continue;

                    var alias = tableprefix + ":" + record.Alias.ToLowerInvariant();

                    Add(_rootNode, record.Ref, alias);
                }

                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddAliasManually(string fullAlias, Ref @ref)
            {
                Add(_rootNode, @ref, fullAlias);
            }

            public void EndRebuilding()
            {
                _target.Clear();

                Optimize(_rootNode);
                Rebuild(_target, _rootNode);
                _target.RootEntry.Begin = _rootNode.Begin;
                _target.RootEntry.End = _rootNode.End;
            }

            private static void Optimize(Node currentNode, bool isRoot = true)
            {
                foreach (var (key, node) in currentNode.Children.ToArray())
                {
                    if (node.Children != null)
                    {
                        Optimize(node, false);

                        if (node.Children.Count == 1)
                        {
                            var childNodePair = node.Children.First();

                            if (!isRoot || !childNodePair.Value.IsLeaf)
                            {
                                var newKey = key + childNodePair.Key;
                                currentNode.Children.Remove(key);
                                currentNode.Children[newKey] = childNodePair.Value;
                            }
                        }
                    }
                }
            }

            private class KoreanStringComparer : IComparer<string>
            {
                public static readonly KoreanStringComparer Instance = new();
                private static readonly Encoding KoreanEncoding
                    = CodePagesEncodingProvider.Instance.GetEncoding(949);

                private static unsafe int strcmp(byte* p1, byte* p2)
                {
                    while (*p1 != 0 && *p1 == *p2)
                    {
                        ++p1;
                        ++p2;
                    }

                    return (*p1 > *p2 ? 1 : 0) - (*p2 > *p1 ? 1 : 0);
                }

                public unsafe int Compare(string x, string y)
                {
                    var b1 = KoreanEncoding.GetBytes(x + "\0");
                    var b2 = KoreanEncoding.GetBytes(y + "\0");

                    fixed (byte* p1 = b1)
                    fixed (byte* p2 = b2)
                        return strcmp(p1, p2);
                }
            }

            /// <summary>
            /// Only called on leafs
            /// </summary>
            private static void Rebuild(NameTable nameTable, Node currentNode)
            {
                foreach (var node in currentNode.Children.Values)
                {
                    if (node.IsLeaf)
                        Rebuild(nameTable, node);
                }

                var begin = (uint) nameTable.Entries.Count;

                foreach (var (key, node) in currentNode.Children.OrderBy(x => x.Key, KoreanStringComparer.Instance))
                {
                    var entry = new NameTableEntry();
                    entry.Begin = node.Begin;
                    entry.End = node.End;
                    entry.String = key;
                    nameTable.Entries.Add(entry);
                }

                var end = (uint) nameTable.Entries.Count - 1; // probably has to be - 1

                currentNode.Begin = begin << 1;
                currentNode.End = end;
            }

            private static void Add(Node currentNode, Ref @ref, ReadOnlySpan<char> alias)
            {
                while (true)
                {
                    var index = alias.IndexOfAny('_', '.', ':') + 1;

                    if (index == 0) // no more parts means it's value
                    {
                        currentNode.Children[alias.ToString()] = new Node(@ref);
                    }
                    else // just another leaf
                    {
                        var currentPart = alias[..index].ToString();
                        if (!currentNode.Children.TryGetValue(currentPart, out var node))
                        {
                            node = new Node();
                            currentNode.Children[currentPart] = node;
                        }

                        currentNode = node;
                        alias = alias[index..];
                        continue;
                    }

                    break;
                }
            }
        }

        public NameTableEntry RootEntry { get; } = new NameTableEntry();
        public virtual List<NameTableEntry> Entries { get; } = new List<NameTableEntry>();

        public Rebuilder BeginRebuilding()
        {
            return new Rebuilder(this);
        }

        public virtual void Clear()
        {
            Entries.Clear();
        }
    }

    public class NameTableEntry
    {
        public string String;
        public long StringOffset;
        public uint Begin;
        public uint End;

        public bool IsLeaf => (Begin & 1) == 0;
        public Ref ToRef() => Ref.From((Begin | (ulong) End << 32) >> 1);

        public override string ToString()
        {
            return $"{Begin >> 1}-{End} IsLeaf:{IsLeaf}";
        }
    }
}