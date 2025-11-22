namespace Loupedeck.ResearchAidPlugin
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;

    // Adjustment to set the number of columns for table insertion
    public class TableColumnsAdjustment : PluginDynamicAdjustment
    {
        public TableColumnsAdjustment()
            : base(displayName: "Table Columns", description: "Set number of table columns", groupName: "LaTeX", hasReset: true)
        {
        }

        protected override void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            InsertTableCommand.TableColumns += diff;
            
            // Keep columns between 1 and 10
            if (InsertTableCommand.TableColumns < 1)
            {
                InsertTableCommand.TableColumns = 1;
            }
            else if (InsertTableCommand.TableColumns > 10)
            {
                InsertTableCommand.TableColumns = 10;
            }
            
            this.AdjustmentValueChanged();
            
            // Auto-insert the table with new dimensions (debounced)
            InsertTableCommand.ScheduleAutoInsert();
        }

        protected override void RunCommand(String actionParameter)
        {
            InsertTableCommand.TableColumns = 3; // Reset to default
            this.AdjustmentValueChanged();
            
            // Auto-insert the table with reset dimensions (debounced)
            InsertTableCommand.ScheduleAutoInsert();
        }

        protected override String GetAdjustmentValue(String actionParameter) => 
            InsertTableCommand.TableColumns.ToString();
    }
}
