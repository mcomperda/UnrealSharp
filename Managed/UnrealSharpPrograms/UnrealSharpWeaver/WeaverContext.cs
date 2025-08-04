using UnrealSharp.Tools;

namespace UnrealSharpWeaver
{
    public class WeaverContext(WeaverOptions options, ToolLogger logger) : ToolContext(logger)
    {
        protected readonly WeaverOptions _options = options;
        public WeaverOptions Options => _options;

        protected readonly WeaverImporter _importer = new(options, logger);
        public WeaverImporter Importer => _importer;

    }
}
