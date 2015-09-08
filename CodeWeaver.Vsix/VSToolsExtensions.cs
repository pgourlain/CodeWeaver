using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeWeaver.Vsix
{
    static class VSToolsExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="textView"></param>
        /// <param name="match"></param>
        /// <returns></returns>
        /// <remarks>come from https://github.com/SLaks/Ref12/blob/master/Ref12/Extensions.cs </remarks>
        public static SnapshotPoint? GetCaretPoint(this ITextView textView, Predicate<ITextSnapshot> match)
        {
            CaretPosition position = textView.Caret.Position;
            SnapshotSpan? snapshotSpan = textView.BufferGraph.MapUpOrDownToFirstMatch(new SnapshotSpan(position.BufferPosition, 0), match);
            return (snapshotSpan.HasValue) ? new SnapshotPoint?(snapshotSpan.Value.Start) : null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bufferGraph"></param>
        /// <param name="span"></param>
        /// <param name="match"></param>
        /// <returns></returns>
        /// <remarks>come from https://github.com/SLaks/Ref12/blob/master/Ref12/Extensions.cs </remarks>
        public static SnapshotSpan? MapUpOrDownToFirstMatch(this IBufferGraph bufferGraph, SnapshotSpan span, Predicate<ITextSnapshot> match)
        {
            NormalizedSnapshotSpanCollection spans = bufferGraph.MapUpToFirstMatch(span, SpanTrackingMode.EdgeExclusive, match);
            if (!spans.Any())
                spans = bufferGraph.MapDownToFirstMatch(span, SpanTrackingMode.EdgeExclusive, match);
            return spans.Select(s => s)
                        .FirstOrDefault();
        }
    }
}
