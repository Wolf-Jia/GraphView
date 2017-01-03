﻿using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphView
{
    public class GremlinUtil
    {
        internal static WColumnReferenceExpression GetColumnReferenceExpression(params string[] parts)
        {
            return new WColumnReferenceExpression()
            {
                MultiPartIdentifier = GetMultiPartIdentifier(parts)
            };
        }

        internal static WColumnReferenceExpression GetStarColumnReferenceExpression()
        {
            return new WColumnReferenceExpression()
            {
                ColumnType = ColumnType.Wildcard,
            };
        }

        internal static WMultiPartIdentifier GetMultiPartIdentifier(params string[] parts)
        {
            var multiIdentifierList = new List<Identifier>();
            foreach (var part in parts)
            {
                multiIdentifierList.Add(new Identifier() { Value = part });
            }
            return new WMultiPartIdentifier() { Identifiers = multiIdentifierList };
        }

        internal static Identifier GetIdentifier(string value)
        {
            return new Identifier() {Value = value};
        }

        internal static BooleanComparisonType GetComparisonTypeFromPredicateType(PredicateType predicateType)
        {
            if (predicateType == PredicateType.eq) return BooleanComparisonType.Equals;
            if (predicateType == PredicateType.neq) return BooleanComparisonType.NotEqualToExclamation;
            if (predicateType == PredicateType.lt) return BooleanComparisonType.LessThan;
            if (predicateType == PredicateType.lte) return BooleanComparisonType.LessThanOrEqualTo;
            if (predicateType == PredicateType.gt) return BooleanComparisonType.GreaterThan;
            if (predicateType == PredicateType.gte) return BooleanComparisonType.GreaterThanOrEqualTo;
            throw new Exception("Error: GetComparisonTypeFromPredicateType");
        }

        internal static WValueExpression GetValueExpression(object value)
        {
            if (value is string)
            {
                return new WValueExpression(value as string, true);
            }
            else
            {
                return new WValueExpression(value.ToString(), false);
            }
        }

        internal static WBooleanComparisonExpression GetBooleanComparisonExpr(WScalarExpression key, object value)
        {
            WScalarExpression valueExpression = GetValueExpression(value);

            return new WBooleanComparisonExpression()
            {
                ComparisonType = BooleanComparisonType.Equals,
                FirstExpr = key,
                SecondExpr = valueExpression
            };
        }

        internal static WBooleanComparisonExpression GetBooleanComparisonExpr(WScalarExpression firstExpr,
            WScalarExpression secondExpr, BooleanComparisonType type)
        {
            return new WBooleanComparisonExpression()
            {
                ComparisonType = type,
                FirstExpr = firstExpr,
                SecondExpr = secondExpr
            };
        }

        internal static WBooleanExpression GetBooleanComparisonExpr(WScalarExpression firstExpr, WScalarExpression secondExpr, Predicate predicate)
        {
            if (predicate.PredicateType == PredicateType.within ||
                predicate.PredicateType == PredicateType.without ||
                predicate.PredicateType == PredicateType.inside ||
                predicate.PredicateType == PredicateType.outside ||
                predicate.PredicateType == PredicateType.between)
            {
                List<WBooleanExpression> booleanExprList = new List<WBooleanExpression>();
                switch (predicate.PredicateType)
                {
                    case PredicateType.within:
                        foreach (var value in predicate.Values)
                        {
                            booleanExprList.Add(GetBooleanComparisonExpr(firstExpr,
                                new Predicate(PredicateType.eq, value)));
                        }
                        return ConcatBooleanExprWithOr(booleanExprList);
                    case PredicateType.without:
                        foreach (var value in predicate.Values)
                        {
                            booleanExprList.Add(GetBooleanComparisonExpr(firstExpr,
                                new Predicate(PredicateType.neq, value)));
                        }
                        return ConcatBooleanExprWithAnd(booleanExprList);
                    case PredicateType.inside:
                        //TODO
                        throw new NotImplementedException();
                    case PredicateType.outside:
                        //TODO
                        throw new NotImplementedException();
                    case PredicateType.between:
                        //TODO
                        throw new NotImplementedException();
                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                return new WBooleanComparisonExpression()
                {
                    ComparisonType = GetComparisonTypeFromPredicateType(predicate.PredicateType),
                    FirstExpr = firstExpr,
                    SecondExpr = secondExpr
                };
            }
        }

        internal static WBooleanBinaryExpression GetAndBooleanBinaryExpr(WBooleanExpression booleanExpr1,
            WBooleanExpression booleanExpr2)
        {
            return new WBooleanBinaryExpression()
            {
                BooleanExpressionType = BooleanBinaryExpressionType.And,
                FirstExpr = booleanExpr1,
                SecondExpr = booleanExpr2
            };
        }

        internal static WExistsPredicate GetExistPredicate(WSqlStatement subQueryExpr)
        {
            return new WExistsPredicate()
            {
                Subquery = new WScalarSubquery
                {
                    SubQueryExpr = subQueryExpr as WSelectQueryExpression
                }
            };
        }

        internal static WBooleanNotExpression GetNotExistPredicate(WSqlStatement subQueryExpr)
        {
            return new WBooleanNotExpression()
            {
                Expression = GetExistPredicate(subQueryExpr)
            };
        }


        internal static WBooleanExpression ConcatBooleanExprWithOr(List<WBooleanExpression> booleanExprList)
        {
            return ConcatBooleanExpressionList(booleanExprList, BooleanBinaryExpressionType.Or);
        }

        internal static WBooleanExpression ConcatBooleanExprWithAnd(List<WBooleanExpression> booleanExprList)
        {
            return ConcatBooleanExpressionList(booleanExprList, BooleanBinaryExpressionType.And);
        }

        internal static WBooleanExpression ConcatBooleanExpressionList(List<WBooleanExpression> booleanExprList,
            BooleanBinaryExpressionType type)
        {
            WBooleanExpression concatExpr = null;
            foreach (var booleanExpr in booleanExprList)
            {
                if (booleanExpr != null && concatExpr != null)
                    concatExpr = new WBooleanBinaryExpression()
                    {
                        BooleanExpressionType = type,
                        FirstExpr = booleanExpr,
                        SecondExpr = concatExpr
                    };
                if (booleanExpr != null && concatExpr == null)
                    concatExpr = booleanExpr;
            }
            return concatExpr;
        }

        internal static WSchemaObjectName GetSchemaObjectName(string value)
        {
            return new WSchemaObjectName()
            {
                Identifiers = new List<Identifier>() {new Identifier() {Value = value}}
            };
        }

        internal static WNamedTableReference GetNamedTableReference(string value)
        {
            return new WNamedTableReference()
            {
                TableObjectName = GetSchemaObjectName(value)
            };
        }

        internal static WNamedTableReference GetNamedTableReference(GremlinVariable2 gremlinVar)
        {
            return new WNamedTableReference()
            {
                Alias = new Identifier() { Value = gremlinVar.VariableName },
                TableObjectString = "node",
                TableObjectName = new WSchemaObjectName(new Identifier() { Value = "node" }),
                Low = gremlinVar.Low,
                High = gremlinVar.High
            };
        }

        internal static WBooleanExpression GetHasKeyBooleanExpression(GremlinVariable currVar, string key)
        {
            WFunctionCall functionCall = new WFunctionCall()
            {
                FunctionName = GetIdentifier("IS_DEFINED"),
                Parameters = new List<WScalarExpression>()
                {
                    new WColumnReferenceExpression()
                    {
                        MultiPartIdentifier = GetMultiPartIdentifier(currVar.VariableName, key)
                    }
                }
            };
            WBooleanExpression booleanExpr = new WBooleanComparisonExpression()
            {
                ComparisonType = BooleanComparisonType.Equals,
                FirstExpr = functionCall,
                SecondExpr =
                    new WColumnReferenceExpression() {MultiPartIdentifier = GetMultiPartIdentifier("true")}
            };
            return booleanExpr;
        }

        internal static WFunctionCall GetFunctionCall(string functionName, params WScalarExpression[] parameterList)
        {
            return new WFunctionCall()
            {
                FunctionName = GetIdentifier(functionName),
                Parameters = new List<WScalarExpression>(parameterList)
            };
        }

        internal static WSelectScalarExpression GetSelectScalarExpression(WScalarExpression valueExpr)
        {
            return new WSelectScalarExpression() {SelectExpr = valueExpr};
        }

        internal static WExpressionWithSortOrder GetExpressionWithSortOrder(string key, GremlinKeyword.Order order)
        {
            return new WExpressionWithSortOrder()
            {
                ScalarExpr = GetColumnReferenceExpression(key),
                SortOrder = ConvertGremlinOrderToSqlOrder(order)
            };
        }

        internal static SortOrder ConvertGremlinOrderToSqlOrder(GremlinKeyword.Order order)
        {
            if (GremlinKeyword.Order.Desr == order) return SortOrder.Descending;
            if (GremlinKeyword.Order.Incr == order) return SortOrder.Ascending;
            if (GremlinKeyword.Order.Shuffle == order) return SortOrder.NotSpecified;
            return SortOrder.Descending;
        }

        internal static WGroupingSpecification GetGroupingSpecification(string key)
        {
            return new WExpressionGroupingSpec()
            {
                Expression = GetColumnReferenceExpression(key)
            };
        }

        internal static WMatchPath GetMatchPath(GremlinMatchPath path)
        {
            var pathEdges = new List<Tuple<WSchemaObjectName, WEdgeColumnReferenceExpression>>();
            pathEdges.Add(GetPathExpression(path));

            WSchemaObjectName tailNode = null;
            if (path.SinkVariable != null)
            {
                tailNode = GetSchemaObjectName(path.SinkVariable.VariableName);
            }

            return new WMatchPath() { PathEdgeList = pathEdges, Tail = tailNode };
        }

        internal static Tuple<WSchemaObjectName, WEdgeColumnReferenceExpression> GetPathExpression(GremlinMatchPath path)
        {
            String value = "Edge";

            WSchemaObjectName sourceName = null;
            if (path.SourceVariable != null)
            {
                sourceName = GetSchemaObjectName(path.SourceVariable.VariableName);
            }
            var pathExpr = new Tuple<WSchemaObjectName, WEdgeColumnReferenceExpression>(
                sourceName,
                new WEdgeColumnReferenceExpression()
                {
                    MultiPartIdentifier = GetMultiPartIdentifier(value),
                    Alias = path.EdgeVariable.VariableName,
                    MinLength = 1,
                    MaxLength = 1
                }
            );
            return pathExpr;
        }

        internal static WBooleanParenthesisExpression GetBooleanParenthesisExpression(WBooleanExpression booleanExpr)
        {
            return new WBooleanParenthesisExpression()
            {
                Expression = booleanExpr
            };
        }

        internal static void InheritedVariableFromParent(GraphTraversal2 childTraversal, GremlinToSqlContext inputContext)
        {
            var rootOp = childTraversal.GetStartOp();
            if (rootOp.GetType() == typeof(GremlinParentContextOp))
            {
                GremlinParentContextOp rootAsContextOp = rootOp as GremlinParentContextOp;
                rootAsContextOp.InheritedPivotVariable = inputContext.PivotVariable;
                rootAsContextOp.InheritedTaggedVariables = inputContext.TaggedVariables;
            }
        }

        internal static void InheritedContextFromParent(GraphTraversal2 childTraversal, GremlinToSqlContext inputContext)
        {
            GremlinTranslationOperator rootOp = childTraversal.GetStartOp();
            if (rootOp.GetType() == typeof(GremlinParentContextOp))
            {
                GremlinParentContextOp rootAsContextOp = rootOp as GremlinParentContextOp;
                rootAsContextOp.InheritedContext = inputContext.Duplicate();
            }
        }

        internal static WSchemaObjectFunctionTableReference GetSchemaObjectFunctionTableReference(string functionName,
            List<WScalarExpression> parameterList)
        {
            return new WSchemaObjectFunctionTableReference()
            {
                SchemaObject = new WSchemaObjectName(GetIdentifier(functionName)),
                Parameters = parameterList
            };
        }

        internal static WScalarSubquery GetScalarSubquery(WSelectQueryBlock selectQueryBlock)
        {
            return new WScalarSubquery()
            {
                SubQueryExpr = selectQueryBlock
            };
        }

        internal static WVariableReference GetVariableReference(string name)
        {
            return new WVariableReference()
            {
                Name = "@" + name
            };
        }

        internal static WUnqualifiedJoin GetCrossApplyTableReference(WTableReference firstTableRef, WTableReference secondTableRef)
        {
            return new WUnqualifiedJoin()
            {
                FirstTableRef = firstTableRef,
                SecondTableRef = secondTableRef,
                UnqualifiedJoinType = UnqualifiedJoinType.CrossApply
            };
        }

        internal static WBooleanComparisonExpression GetEqualPredicate(WScalarExpression firstExpr, WScalarExpression secondExpr)
        {
            return new WBooleanComparisonExpression()
            {
                ComparisonType = BooleanComparisonType.Equals,
                FirstExpr = firstExpr,
                SecondExpr = secondExpr
            };
        }
    }
}
