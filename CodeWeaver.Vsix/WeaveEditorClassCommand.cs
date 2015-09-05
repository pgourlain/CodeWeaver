using CodeWeaver.Vsix.Processor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeWeaver.Vsix
{
    class WeaveEditorClassCommand : SimpleCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0104;


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
            WeaverHelper.WeaveOrUnWeaveFromEditor((x, y) => x.Weave(y));
        }

        protected override int GetIsEnabled()
        {
            return WeaverHelper.CaretIsInClass() ? 1 : 0;
        }
    }
}
