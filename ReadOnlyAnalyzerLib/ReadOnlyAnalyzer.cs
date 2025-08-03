using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;

namespace CodeAnalyzer
{

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ReadOnlyAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "RO001";
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "ReadOnly class should not define or call Write methods",
            "Class marked [ReadOnly] must not define or call '{0}' method",
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        public static readonly string[] writableMethodNames = new string[]
        {
            "save",
            "insert",
            "update"
        };


        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static bool IsWritableMethodName(string methodName)
        {
            // Normalize method name to lower case for comparison
            string name = methodName.ToLowerInvariant();
            for(int i=0;i<writableMethodNames.Length;++i)
            {
                if(name.Contains(writableMethodNames[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private static void AnalyzeClass(SyntaxNodeAnalysisContext ctx)
        {
            try
            {
                var classDecl = (ClassDeclarationSyntax)ctx.Node;
                var sym = ctx.SemanticModel.GetDeclaredSymbol(classDecl);
                if (sym == null) return;
                
                // Check for ReadOnlyAttribute more safely
                bool hasReadOnlyAttribute = false;
                var attributes = sym.GetAttributes();
                foreach (var attr in attributes)
                {
                    if (attr.AttributeClass != null && attr.AttributeClass.Name == "ReadOnlyAttribute")
                    {
                        hasReadOnlyAttribute = true;
                        break;
                    }
                }
                
                if (!hasReadOnlyAttribute) return;

                // Check class members for forbidden methods
                foreach (var member in classDecl.Members)
                {
                    if (member is MethodDeclarationSyntax method)
                    {
                        var name = method.Identifier.Text;
                        if (IsWritableMethodName(name))
                        {
                            var diag = Diagnostic.Create(Rule, method.Identifier.GetLocation(), name);
                            ctx.ReportDiagnostic(diag);
                        }
                    }
                }
            }
            catch
            {
                // Analyzer should not throw exceptions
            }
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext ctx)
        {
            try
            {
                var inv = (InvocationExpressionSyntax)ctx.Node;
                var symbolInfo = ctx.SemanticModel.GetSymbolInfo(inv);
                var sym = symbolInfo.Symbol as IMethodSymbol;
                if (sym == null) return;

                // Check if this is a forbidden method call
                if (!IsWritableMethodName(sym.Name))
                    return;

                // Find containing class
                var containingClass = inv.FirstAncestorOrSelf<ClassDeclarationSyntax>();
                if (containingClass == null) return;
                
                var classSym = ctx.SemanticModel.GetDeclaredSymbol(containingClass);
                if (classSym == null) return;

                // Check if containing class has ReadOnlyAttribute
                bool hasReadOnlyAttribute = false;
                var attributes = classSym.GetAttributes();
                foreach (var attr in attributes)
                {
                    if (attr.AttributeClass != null && attr.AttributeClass.Name == "ReadOnlyAttribute")
                    {
                        hasReadOnlyAttribute = true;
                        break;
                    }
                }
                
                if (!hasReadOnlyAttribute) return;

                var diag = Diagnostic.Create(Rule, inv.GetLocation(), sym.Name);
                ctx.ReportDiagnostic(diag);
            }
            catch
            {
                // Analyzer should not throw exceptions
            }
        }
    }

}