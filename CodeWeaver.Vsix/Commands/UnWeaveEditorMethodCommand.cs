using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using CodeWeaver.Vsix.Processor;
using Microsoft.CodeAnalysis.Formatting;

namespace CodeWeaver.Vsix
{
    internal sealed class UnWeaveEditorMethodCommand : SimpleCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0103;

        protected override void GetCmdId(out int commandId)
        {
            commandId = CommandId;
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        protected override void OnMenuClick(object sender, EventArgs e)
        {
            WeaverHelper.WeaveOrUnWeaveFromEditor((x,y) => x.UnWeave(y));
        }

        protected override bool UpdateVisibleAndEnabled(out bool visible, out bool enabled)
        {
            visible = WeaverHelper.CaretIsInCSharpDocument();
            enabled = WeaverHelper.CaretIsInMethod();
            return true;
        }
    }
}


/*

appel depuis la commande
			ISymbolResolver resolver = null;
			SnapshotPoint? caretPoint = TextView.GetCaretPoint(s => resolvers.TryGetValue(s.ContentType.TypeName, out resolver));
			if (caretPoint == null)
				return false;

			var symbol = resolver.GetSymbolAt(doc.FilePath, caretPoint.Value);
			if (symbol == null || symbol.HasLocalSource)
				return false;

			var target = references.FirstOrDefault(r => r.AvailableAssemblies.Contains(symbol.AssemblyName));
			if (target == null)
				return false;

			Debug.WriteLine("Ref12: Navigating to IndexID " + symbol.IndexId);

			target.Navigate(symbol);
			return true;

//le resolver sous roslyn

public class RoslynSymbolResolver : ISymbolResolver {
		public MySymbolInfo GetSymbolAt(string sourceFileName, SnapshotPoint point) {
			// Yes; this is evil and synchronously waits for async tasks.
			// That is exactly what Roslyn's GoToDefinitionCommandHandler
			// does; apparently a VS command handler can't be truly async
			// (Roslyn does use IWaitIndicator, which I can't).

			var doc = point.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
			var model = doc.GetSemanticModelAsync().Result;
			var symbol = SymbolFinder.FindSymbolAtPosition(model, point, doc.Project.Solution.Workspace);
			if (symbol == null || symbol.ContainingAssembly == null)
				return null;

			if (symbol.Kind == SymbolKind.Local || symbol.Kind == SymbolKind.Namespace)
				return null;

			// F12 on the declaration of a lambda parameter should jump to its type; all other parameters shouldn't be handled at all.
			var param = symbol as IParameterSymbol;
			if (param != null) {
				var method = param.ContainingSymbol as IMethodSymbol;
				if (method == null || method.MethodKind != MethodKind.LambdaMethod)
					return null;
				if (param.Locations.Length != 1)
					return null;

				if (param.Locations[0].IsInSource
				 && !param.Locations[0].SourceSpan.Contains(point)
				 && param.Locations[0].SourceSpan.End != point)		// Contains() is exclusive
					return null;
				else
					symbol = param.Type;
			}
			symbol = IndexIdTranslator.GetTargetSymbol(symbol);

			PortableExecutableReference reference = null;
			Compilation comp;
			if (doc.Project.TryGetCompilation(out comp))
				reference = comp.GetMetadataReference(symbol.ContainingAssembly) as PortableExecutableReference;

			return new MySymbolInfo(
				IndexIdTranslator.GetId(symbol),
				isLocal: doc.Project.Solution.Workspace.Kind != WorkspaceKind.MetadataAsSource && doc.Project.Solution.GetProject(symbol.ContainingAssembly) != null,
				assemblyPath: reference == null ? null : reference.Display,
				assemblyName: symbol.ContainingAssembly.Identity.Name
			);
		}
	}

*/
