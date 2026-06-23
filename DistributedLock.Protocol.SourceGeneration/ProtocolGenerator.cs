using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DistributedLock.Protocol.SourceGeneration;

[Generator]
public sealed class ProtocolGenerator : IIncrementalGenerator
{
	private const string AttributeNamespace = "DistributedLock.Protocol";
	private const string ByteOperationsNamespace = "DistributedLock.Protocol.ByteOperations";
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
		var allowedTypesProvider = context.CompilationProvider.Select((c, _) =>
		{
		    return new AllowedTypes(
		        c.GetSpecialType(SpecialType.System_Boolean),
		        c.GetSpecialType(SpecialType.System_Byte),
		        c.GetSpecialType(SpecialType.System_Int16),
		        c.GetSpecialType(SpecialType.System_Int32),
		        c.GetSpecialType(SpecialType.System_Int64),
		        c.GetTypeByMetadataName("System.Guid")!
		    );
		});
		var attributesProvider  = context.CompilationProvider
			.Select((c, _) =>
			{
				return new
				{
					RequestAttribute = 
						c.GetTypeByMetadataName(
							$"{AttributeNamespace}.RequestAttribute"
						) ?? throw new NullReferenceException("Expected attribute class not found."),
					ResponseAttribute = 
						c.GetTypeByMetadataName(
							$"{AttributeNamespace}.ResponseAttribute"
						) ?? throw new NullReferenceException("Expected attribute class not found.")
				};
			});

		var scaffoldingInfoProvider    = context.SyntaxProvider.CreateSyntaxProvider(
			predicate: static (node, _) =>
			{
				return node is ClassDeclarationSyntax;
			},
			transform: (ctx, _) =>
			{
				if (ctx.Node is TypeDeclarationSyntax typeDeclaration)
				{
					var symbol = ctx.SemanticModel.GetDeclaredSymbol(typeDeclaration);
					if (symbol is null) return null;
					return new TypeInfo(typeDeclaration, symbol);
				}
				return null;				
			}
		)
		.Where(info => info is not null)
		.Select((info, _) => (TypeInfo)info!)
		// Find types
		.Combine(attributesProvider )
		.Where((tuple) =>
		{
			var (info, attributes) = tuple;
			return HasAttribute(info.Symbol, attributes.RequestAttribute)
				|| HasAttribute(info.Symbol, attributes.ResponseAttribute);
		})
		.Select((tuple, _) => tuple.Left)
		// Check requirements for type declaration
		.Select((info, _) =>
		{
			if (!info.DeclarationSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
				throw new Exception(
					$"Type {info.Symbol.Name} is invalid. Only types declared as partial are allowed."
				);
			if (!info.DeclarationSyntax.IsKind(SyntaxKind.RecordStructDeclaration))
				throw new Exception(
					$"Type {info.Symbol.Name} is invalid. Only types declared as record struct are allowed."
				);
			if (info.Symbol.IsAbstract)
				throw new Exception(
					$"Type {info.Symbol.Name}. Abstract classes are not allowed."
				);
			return info;		
		})
		// Check fields
		.Combine(allowedTypesProvider)
		.Select((tuple, _) =>
		{
			var (info, allowedTypes) = tuple;
			foreach (var prop in info.Symbol
				.GetMembers()
				.OfType<IPropertySymbol>()
				.Where(p => p.DeclaredAccessibility == Accessibility.Public)
				.Where(p => !p.IsStatic)
			)
			{
				if (allowedTypes.IsAllowed(prop.Type))
					throw new Exception(
						$"Type {prop.Type} is not allowed for public fields."
					);
			}
			return info;
		});

		var sourceRegistrationInputData =
			scaffoldingInfoProvider   
				.Collect()
				.Combine(allowedTypesProvider)
				.Combine(attributesProvider )
				.Select((tuple, _) =>
				{
					var ((a, b), c) = tuple;
					return (a, b, c);
				});

		context.RegisterSourceOutput(sourceRegistrationInputData, (spc, data) =>
		{
			var (
				scaffoldingTypesInfo,
				allowedPropertyTypes,
				attributes
			) = data;

			byte nextMessageTypeId = 0;
			foreach (var scaffolding in scaffoldingTypesInfo)
			{
				var indent = "    ";
				var declaration = scaffolding.DeclarationSyntax.NormalizeWhitespace();
				var symbol = scaffolding.Symbol;
				var @namespace = symbol.ContainingNamespace.ToDisplayString();

				var mem = new Memory<byte>(new byte[] {1, 2, 3});
				var outputBuilder = new StringBuilder(
					$$"""
					using {{ByteOperationsNamespace}};

					namespace {{@namespace}};

					{{declaration.ToFullString()}}
					{
						public const int ByteSize = ByteTools.SizeOf<{{symbol.Name}}() + 1;
						
						public const byte MessageTypeIdentifier = {{nextMessageTypeId++}};

						public static {{symbol.Name}} FromBytes(ReadOnlyMemory bytes)
						{
							var span = bytes.Span;
							if (span.Length < ByteSize)
								throw new Exception(
									"Serialized form contains unexpectedly few bytes."
									+ $" Expected {ByteSize} bytes, got {bytes.Length}."
								);
							var reader = new ByteReader(span[1..]);

							return new(
					"""
				);
				var props = symbol
					.GetMembers()
					.OfType<IPropertySymbol>()
					.Where(p => p.DeclaredAccessibility == Accessibility.Public)
					.ToArray();

				
				foreach (var prop in props)
				{
					var line = allowedPropertyTypes.GetEnumCode(prop.Type) switch
					{
						AllowedType.Bool => $"{indent}{indent}{indent}reader.ReadBool(),",
						AllowedType.Byte => $"{indent}{indent}{indent}reader.ReadByte(),",
						AllowedType.Short => $"{indent}{indent}{indent}reader.ReadShort(),",
						AllowedType.Int => $"{indent}{indent}{indent}reader.ReadInt(),",
						AllowedType.Long => $"{indent}{indent}{indent}reader.ReadLong(),",
						AllowedType.Guid => $"{indent}{indent}{indent}reader.ReadGuid(),",
						_ => throw new Exception($"Type {prop.Type.Name} is not allowed.")
					};
					outputBuilder.AppendLine(line);
				}
				if (props.Length > 0)
					outputBuilder.Remove(outputBuilder.Length - 1, 1);
				outputBuilder
					.AppendLine($"{indent}{indent});")
					.AppendLine($"{indent}}}")
					.AppendLine();

				outputBuilder.Append(
					$$"""
						public ReadOnlyMemory<byte> ToBytes(Memory<byte> buffer)
						{
							if (span.Length < ByteSize)
								throw new Exception(
									"Buffer is too small."
									+ $" Expected {ByteSize} bytes, got {buffer.Length}."
								);
							var writer = new ByteWriter(buffer.Span);
					"""
				);
				foreach (var prop in props)
				{
					
					var line = allowedPropertyTypes.GetEnumCode(prop.Type) switch
					{
						AllowedType.Bool => $"{indent}{indent}writer.WriteBool({prop.Name});",
						AllowedType.Byte => $"{indent}{indent}writer.WriteByte({prop.Name});",
						AllowedType.Short => $"{indent}{indent}writer.WriteShort({prop.Name});",
						AllowedType.Int => $"{indent}{indent}writer.WriteInt({prop.Name});",
						AllowedType.Long => $"{indent}{indent}writer.WriteLong({prop.Name});",
						AllowedType.Guid => $"{indent}{indent}writer.WriteGuid({prop.Name});",
						_ => throw new Exception($"Type {prop.Type.Name} is not allowed.")
					};
					outputBuilder.AppendLine(line);
				}
				if (props.Length > 0)
					outputBuilder.Remove(outputBuilder.Length - 1, 1);
				outputBuilder.Append(
					$$"""
							return buffer.Slice(0, ByteSize);
						}
					}	
					"""
				);
				spc.AddSource(
					$"{scaffolding.Symbol.Name}.g.cs",
					outputBuilder.ToString()
				);
			}
		});
	}

	private bool HasAttribute(ITypeSymbol type, INamedTypeSymbol attribute)
	{
		return type.GetAttributes().Any(
			a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attribute)
		);
	}
}

internal sealed record TypeInfo(
	TypeDeclarationSyntax DeclarationSyntax, INamedTypeSymbol Symbol
);

internal sealed record AllowedTypes(
    INamedTypeSymbol Bool,
    INamedTypeSymbol Byte,
    INamedTypeSymbol Short,
    INamedTypeSymbol Int,
    INamedTypeSymbol Long,
    INamedTypeSymbol Guid
)
{
	private readonly HashSet<INamedTypeSymbol> _simpleTypes =
		new(SymbolEqualityComparer.Default)
		{
			Bool,
			Byte,
			Short,
			Int,
			Long,
			Guid
		};

	private readonly Dictionary<ITypeSymbol, AllowedType> _typeToEnum =
		new(SymbolEqualityComparer.Default)
		{
			[Bool] = AllowedType.Bool,
			[Byte] = AllowedType.Byte,
			[Short] = AllowedType.Short,
			[Int] = AllowedType.Int,
			[Long] = AllowedType.Long,
			[Guid] = AllowedType.Guid,
		};

	public bool IsAllowed(ITypeSymbol type)
	{
		if (_simpleTypes.Contains(type))
			return true;
		return false;
	}

	public AllowedType GetEnumCode(ITypeSymbol type)
	{
		return _typeToEnum.GetValueOrDefault(type, AllowedType.Disallowed);
	}
}

internal enum AllowedType
{
	Bool,
	Byte,
	Short,
	Int,
	Long,
	Guid,
	Disallowed
}

