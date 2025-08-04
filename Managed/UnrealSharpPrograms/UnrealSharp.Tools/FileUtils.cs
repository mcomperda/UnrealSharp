namespace UnrealSharp.Tools;

public static class FileUtils
{
    public static void RecursiveFileCopy(DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory)
    {
        // Early out of our search if the last updated timestamps match
        if (sourceDirectory.LastWriteTimeUtc == destinationDirectory.LastWriteTimeUtc) return;

        if (!destinationDirectory.Exists)
        {
            destinationDirectory.Create();
        }

        foreach (FileInfo sourceFile in sourceDirectory.GetFiles())
        {
            string destinationFilePath = Path.Combine(destinationDirectory.FullName, sourceFile.Name);
            FileInfo destinationFile = new FileInfo(destinationFilePath);

            if (!destinationFile.Exists || sourceFile.LastWriteTimeUtc > destinationFile.LastWriteTimeUtc)
            {
                sourceFile.CopyTo(destinationFilePath, true);
            }
        }

        // Update our write time to match source for faster copying
        destinationDirectory.LastWriteTimeUtc = sourceDirectory.LastWriteTimeUtc;

        foreach (DirectoryInfo subSourceDirectory in sourceDirectory.GetDirectories())
        {
            string subDestinationDirectoryPath = Path.Combine(destinationDirectory.FullName, subSourceDirectory.Name);
            DirectoryInfo subDestinationDirectory = new DirectoryInfo(subDestinationDirectoryPath);

            RecursiveFileCopy(subSourceDirectory, subDestinationDirectory);
        }
    }

}
