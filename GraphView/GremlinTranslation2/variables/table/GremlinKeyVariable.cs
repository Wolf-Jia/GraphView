﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphView
{
    internal class GremlinKeyVariable : GremlinTableVariable
    {
        public GremlinVariableProperty ProjectVariable { get; set; }

        public GremlinKeyVariable(GremlinVariableProperty projectVariable)
        {
            ProjectVariable = projectVariable;
            VariableName = GenerateTableAlias();
        }

        internal override GremlinScalarVariable DefaultProjection()
        {
            return new GremlinVariableProperty(this, "_value");
        }

        public override WTableReference ToTableReference()
        {
            List<WScalarExpression> parameters = new List<WScalarExpression>();
            parameters.Add(ProjectVariable.ToScalarExpression());
            var secondTableRef = GremlinUtil.GetFunctionTableReference("key", parameters, VariableName);
            return GremlinUtil.GetCrossApplyTableReference(null, secondTableRef);
        }
    }
}
