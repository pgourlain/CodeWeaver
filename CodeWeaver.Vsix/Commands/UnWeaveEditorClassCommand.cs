
using CodeWeaver.Vsix.Processor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeWeaver.Vsix
{
    class UnWeaveEditorClassCommand : SimpleCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0105;

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
            WeaverHelper.WeaveOrUnWeaveFromEditor((x, y) => x.UnWeave(y));
        }

        protected override bool UpdateVisibleAndEnabled(out bool visible, out bool enabled)
        {
            visible = WeaverHelper.CaretIsInCSharpDocument();
            enabled = WeaverHelper.CaretIsInClass();
            return true;
        }
    }
}
