using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace IseHelpers
{
    public static class PowerShellInvoker
    {
        private static Collection<PSObject> lastResult;

        public static Collection<PSObject> LastResult
        {
            get { return lastResult; }
        }

        public static Collection<PSObject> Invoke(string cmd)
        {
            using (var runspace = System.Management.Automation.Runspaces.RunspaceFactory.CreateRunspace())
            {
                runspace.Open();

                var powershell = PowerShell.Create();
                powershell.Runspace = runspace;

                powershell.AddScript(cmd);

                lastResult = powershell.Invoke();

                return lastResult;
            }
        }
    }

    public class CommandParameter
    {
        public bool CanBeExecuted { get; set; }
    }
}
