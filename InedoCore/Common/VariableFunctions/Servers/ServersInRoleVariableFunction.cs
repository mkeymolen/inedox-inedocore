﻿using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
#if Otter
using Inedo.Otter.Data;
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.VariableFunctions;
using ContextType = Inedo.Otter.IOtterContext;
#elif BuildMaster
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.VariableFunctions;
using ContextType = Inedo.BuildMaster.Extensibility.IGenericBuildMasterContext;
#endif

namespace Inedo.Extensions.VariableFunctions.Server
{
    [ScriptAlias("ServersInRole")]
    [Description("Returns a list of servers in the specified role.")]
    public sealed class ServersInRoleVariableFunction : VectorVariableFunction
    {
        [DisplayName("roleName")]
        [VariableFunctionParameter(0, Optional = true)]
        [Description("The name of the server role. If not supplied, the current role in context will be used.")]
        public string RoleName { get; set; }

        protected override IEnumerable EvaluateVector(ContextType context)
        {
            int? roleId = FindRole(this.RoleName, context);
            if (roleId == null)
                return null;

            return DB.Servers_SearchServers(Has_ServerRole_Id: roleId, In_Environment_Id: null)
#if Otter
                .Servers_Extended
#elif BuildMaster
                .Servers
#endif
                .Select(s => s.Server_Name);
        }

        private int? FindRole(string roleName, ContextType context)
        {
            var allRoles = DB.ServerRoles_GetServerRoles();

            if (!string.IsNullOrEmpty(roleName))
            {
                return allRoles
                    .FirstOrDefault(r => r.ServerRole_Name.Equals(roleName, StringComparison.OrdinalIgnoreCase))
                    ?.ServerRole_Id;
            }
            else
            {
                return allRoles
                    .FirstOrDefault(r => r.ServerRole_Id == context.ServerRoleId)
                    ?.ServerRole_Id;
            }
        }
    }
}
