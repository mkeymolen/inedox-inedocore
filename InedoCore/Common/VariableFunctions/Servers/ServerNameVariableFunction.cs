using System.ComponentModel;
using System.Linq;
#if Otter
using Inedo.Otter.Data;
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Operations;
using Inedo.Otter.Extensibility.VariableFunctions;
#elif BuildMaster
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMaster.Extensibility.VariableFunctions;
#endif
using Inedo.Documentation;

namespace Inedo.Extensions.VariableFunctions.Server
{
    [ScriptAlias("ServerName")]
    [Description("name of the current server in context")]
#if BuildMaster
    [LegacyAlias("SVRNAME")]
#endif 
    [Tag("servers")]
    public sealed class ServerNameVariableFunction : CommonScalarVariableFunction
    {
        protected override object EvaluateScalar(object context)
        {
            var execContext = context as IOperationExecutionContext;
            if (execContext == null)
                throw new VariableFunctionException("Execution context is not available.");

            if (execContext.ServerId != null)
            {
                return DB.Servers_GetServer(execContext.ServerId)
#if BuildMaster
                .Servers
#elif Otter
                .Servers_Extended
#endif
                .FirstOrDefault()?.Server_Name;
            }

            return string.Empty;
        }
    }
}
