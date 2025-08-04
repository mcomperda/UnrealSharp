using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnrealSharpWeaver.TypeProcessors
{
    public abstract class BaseProcessor(WeaverImporter importer)
    {
        protected readonly WeaverImporter _importer = importer;
    }
}
