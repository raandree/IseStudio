using IseHelpers;
using Microsoft.PowerShell.Host.ISE;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Reflection;
using System.Text;

namespace IseStudio
{
    public partial class IseStudio : IModuleAssemblyInitializer
    {        
        private ListXmlStore<Breakpoint> breakpoints = new ListXmlStore<Breakpoint>();

        public ListXmlStore<Breakpoint> Breakpoints
        {
            get { return breakpoints; }
            set { breakpoints = value; }
        }

        private void ImportBreakpoints()
        {
            var breakpointFileFullName = appFileManager.Get(breakPointsFileName).FullName;
            breakpoints.AddFromFile(breakpointFileFullName);
        }

        private void UpdateBreakpointsFromIse()
        {
            var list = new ListXmlStore<Breakpoint>();
            var currentBreakpoints = tabCollection.SelectedPowerShellTab.GetBreakpoints();

            foreach (var breakpoint in currentBreakpoints)
            {
                var lineBreakPoint = breakpoint as LineBreakpoint;

                if (lineBreakPoint == null) { continue; }

                list.Add(new Breakpoint()
                {
                    Enabled = lineBreakPoint.Enabled,
                    LineNumber = lineBreakPoint.Line,
                    ScriptFullName = lineBreakPoint.Script
                });
            }

            breakpoints = list;
        }

        private void UpdateBreakpointsToIse()
        {
            var selectedFile = tabCollection.SelectedPowerShellTab.Files.SelectedFile;
            var currentLine = selectedFile.GetCurrentLine();
            var script = new StringBuilder();

            foreach (var breakpoint in breakpoints)
            {
                List<LineBreakpoint> breakpointsAtLine = selectedFile.GetBreakpointsAtLine(currentLine);
                if (breakpointsAtLine.Count == 0)                
                {                    
                    script.AppendLine(string.Format("Set-PSBreakpoint -Script '{0}' -Line {1}", breakpoint.ScriptFullName, breakpoint.LineNumber));
                }
            }

            tabCollection.SelectedPowerShellTab.DoAsynchronousInvoke(new PSCommand().AddScript(script.ToString()));
        }

        private void ExportBreakpoints()
        {
            try
            {
                UpdateBreakpointsFromIse();
            }
            catch { }

            breakpoints.Export(appFileManager.Get(breakPointsFileName).FullName);
        }

        void SelectedPowerShellTab_PropertyChanged_ImportBreakpoints(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CanInvoke" && tabCollection.SelectedPowerShellTab.GetCanExecuteToggleBreakpoint())
            {
                ImportBreakpoints();
                UpdateBreakpointsToIse();

                //this needs to run just once, hence we unsubscribe from the event
                tabCollection.SelectedPowerShellTab.PropertyChanged -= SelectedPowerShellTab_PropertyChanged_ImportBreakpoints;
            }
        }
        void CurrentDomain_ProcessExit_ExportBreakpoints(object sender, EventArgs e)
        {
            ExportBreakpoints();
        }
    }

    public class Breakpoint
    {
        public string ScriptFullName { get; set; }
        public int LineNumber { get; set; }
        public bool Enabled { get; set; }

        public override string ToString()
        {
            return string.Format("{0}:{1}", LineNumber, ScriptFullName);
        }
    }
}