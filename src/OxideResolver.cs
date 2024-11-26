﻿using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using Oxide.CompilerServices.Common;
using Oxide.CompilerServices.Models.Configuration;

namespace Oxide.CompilerServices
{
    internal class OxideResolver : MetadataReferenceResolver
    {
        private readonly ILogger logger;
        private readonly DirectorySettings directories;
        private readonly string runtimePath;

        private readonly HashSet<PortableExecutableReference> referenceCache;

        public OxideResolver(ILogger<OxideResolver> logger, OxideSettings settings)
        {
            this.logger = logger;
            directories = settings.Path;
            runtimePath = settings.Compiler.FrameworkPath;
            referenceCache = new HashSet<PortableExecutableReference>();
        }

        public override bool Equals(object? other) => other?.Equals(this) ?? false;

        public override int GetHashCode() => logger.GetHashCode();

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string? baseFilePath, MetadataReferenceProperties properties)
        {
            logger.LogInformation("Resolve: {Reference} {BaseFilePath}", reference, baseFilePath);
            return ImmutableArray<PortableExecutableReference>.Empty;
        }

        public override bool ResolveMissingAssemblies => true;

        public override PortableExecutableReference? ResolveMissingAssembly(MetadataReference definition, AssemblyIdentity referenceIdentity) => Reference(definition.Display!);

        public PortableExecutableReference? Reference(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            PortableExecutableReference? reference = referenceCache.FirstOrDefault(r => Path.GetFileName(r.Display) == name);

            if (reference != null)
            {
                return reference;
            }

            if (name.Equals("System.Private.CoreLib"))
            {
                name = "mscorlib.dll";
            }

            FileInfo fileSystem = new FileInfo(Path.Combine(directories.Libraries, name));

            if (fileSystem.Exists)
            {
                reference = MetadataReference.CreateFromFile(fileSystem.FullName);
                referenceCache.Add(reference);
                return reference;
            }

            fileSystem = new(Path.Combine(runtimePath, name));

            if (fileSystem.Exists)
            {
                reference = MetadataReference.CreateFromFile(fileSystem.FullName);
                referenceCache.Add(reference);
                return reference;
            }

            logger.LogError(Constants.CompileEventId, "Unable to find required dependency {name}", name);
            return null;
        }
    }
}
