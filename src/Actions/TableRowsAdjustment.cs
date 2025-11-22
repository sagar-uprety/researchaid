namespace Loupedeck.ResearchAidPlugin
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;

    // Adjustment to set the number of rows for table insertion
    public class TableRowsAdjustment : PluginDynamicAdjustment
    {
        public TableRowsAdjustment()
            : base(displayName: "Table Rows", description: "Set number of table rows", groupName: "LaTeX", hasReset: true)
        {
        }

        protected override void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            InsertTableCommand.TableRows += diff;
            
            // Keep rows between 1 and 20
            if (InsertTableCommand.TableRows < 1)
            {
                InsertTableCommand.TableRows = 1;
            }
            else if (InsertTableCommand.TableRows > 20)
            {
                InsertTableCommand.TableRows = 20;
            }
            
            this.AdjustmentValueChanged();
            
            // Auto-insert the table with new dimensions (debounced)
            InsertTableCommand.ScheduleAutoInsert();
        }

        protected override void RunCommand(String actionParameter)
        {
            InsertTableCommand.TableRows = 3; // Reset to default
            this.AdjustmentValueChanged();
            
            // Auto-insert the table with reset dimensions (debounced)
            InsertTableCommand.ScheduleAutoInsert();
        }

        protected override String GetAdjustmentValue(String actionParameter) => 
            InsertTableCommand.TableRows.ToString();
    }
}
