using System;
using System.IO;

namespace Resources
{

    /**
     * The default implementation of the ResourceManager
     */
    public class DefaultResourceManager : ResourceManager
    {
        private string DataDir;

        public DefaultResourceManager(string Path)
        {
            this.DataDir = Path;
        }

        // Try to avoid this function
        public override string GetFilename(string ResourcePath)
        {
            return DataDir + ResourcePath;
        }

        public override TextReader Get(string ResourcePath)
        {
        	try {
        		return new StreamReader(DataDir + ResourcePath);
        	} catch(Exception e) {
        		throw new Exception("Couldn't load resource '" + ResourcePath + "'", e);
        	}
        }

        public override string GetDirectoryName(string ResourcePath)
        {
            return Path.GetDirectoryName(ResourcePath);
        }
    }

}
