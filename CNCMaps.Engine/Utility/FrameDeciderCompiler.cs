using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Reflection;
using CNCMaps.Engine.Map;

namespace CNCMaps.Engine.Utility {
	public static class FrameDeciderCompiler {

		/* sample super simple framedecider
		using CNCMaps.Engine.Map;

		namespace SampleSimpleFramedecider {
			class FrameDecider {
				public static int DecideFrame(GameObject obj) {
					if (obj is OwnableObject)
						return (obj as OwnableObject).Direction / 32;
					else
						return 0;
				}
			}
		}
		*/

		public static Func<GameObject, int> CompileFrameDecider(string codeStr) {
			var unit = new CodeCompileUnit();
			var ns = new CodeNamespace("DynamicFrameDeciders");
			ns.Imports.Add(new CodeNamespaceImport("CNCMaps.Engine.Map"));
			unit.Namespaces.Add(ns);

			var @class = new CodeTypeDeclaration("FrameDecider"); //Create class
			ns.Types.Add(@class);

			var method = new CodeMemberMethod();
			method.Attributes = MemberAttributes.Public | MemberAttributes.Static;
			//Make this method an override of base class's method
			method.Name = "DecideFrame";
			method.ReturnType = new CodeTypeReference(typeof(int));
			method.Parameters.Add(new CodeParameterDeclarationExpression("GameObject", "obj"));
			var code = new CodeSnippetStatement(codeStr + ";");
			method.Statements.Add(new CodeSnippetStatement("int frame = 0;"));
			method.Statements.Add(code);
			method.Statements.Add(new CodeSnippetStatement("return frame;"));
			@class.Members.Add(method); //Add method to the class

			// compile DOM
			CodeDomProvider cp = CodeDomProvider.CreateProvider("CS");
			var options = new CompilerParameters();
			options.GenerateInMemory = true;
			options.ReferencedAssemblies.Add("CNCMaps.Engine.dll");
			options.ReferencedAssemblies.Add("CNCMaps.Shared.dll");
			var cpRes = cp.CompileAssemblyFromDom(options, unit);
			AppDomain localDom = AppDomain.CreateDomain("x");

			// now bind the method
			MethodInfo mi = cpRes.CompiledAssembly.GetType("DynamicFrameDeciders.FrameDecider").GetMethod("DecideFrame");

			// and wrap it
			return obj => (int)mi.Invoke(null, new[] {obj});
		}
		
	}
}
