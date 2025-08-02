using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace CodeAnalyzer
{

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ReadOnlyAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "RO001";
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "ReadOnly class should not define or call Write methods",
            "Class marked [ReadOnly] must not define or call '{0}' method.",
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeClass(SyntaxNodeAnalysisContext ctx)
        {
            var classDecl = (ClassDeclarationSyntax)ctx.Node;
            var sym = ctx.SemanticModel.GetDeclaredSymbol(classDecl);
            if (sym == null) return;
            if (!sym.GetAttributes().Any(a => a.AttributeClass?.Name == "ReadOnlyAttribute" || 
                                               a.AttributeClass?.ToDisplayString().EndsWith("ReadOnlyAttribute") == true))
                return;

            // 클래스 내부에 Save(), Insert(), Update() 정의 검사
            foreach (var member in classDecl.Members.OfType<MethodDeclarationSyntax>())
            {
                var name = member.Identifier.Text;
                if (name == "Save" || name == "Insert" || name == "Update")
                {
                    var diag = Diagnostic.Create(Rule, member.Identifier.GetLocation(), name);
                    ctx.ReportDiagnostic(diag);
                }
            }
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext ctx)
        {
            var inv = (InvocationExpressionSyntax)ctx.Node;
            var sym = ctx.SemanticModel.GetSymbolInfo(inv).Symbol as IMethodSymbol;
            if (sym == null) return;

            var containing = sym.ContainingType;
            // 호출 대상이 저장 관련 메서드인지 필터링
            if (sym.Name != "Save" && sym.Name != "Insert" && sym.Name != "Update")
                return;

            // 현재 호출이 ReadOnly 클래스의 내부에서 발생하는지 검사
            var containingClass = inv.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (containingClass == null) return;
            var classSym = ctx.SemanticModel.GetDeclaredSymbol(containingClass);
            if (classSym?.GetAttributes().Any(a => a.AttributeClass?.Name == "ReadOnlyAttribute" || 
                                                    a.AttributeClass?.ToDisplayString().EndsWith("ReadOnlyAttribute") == true) != true)
                return;

            var diag = Diagnostic.Create(Rule, inv.GetLocation(), sym.Name);
            ctx.ReportDiagnostic(diag);
        }
    }

}