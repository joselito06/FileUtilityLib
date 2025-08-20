using FileUtilityLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileUtilityLib.Core
{
    public class FileManager
    {
        public FileOperationResult CopyFiles(FileCopyRequest request)
        {
            var result = new FileOperationResult();

            try
            {
                foreach (var fileName in request.FilesToCopy)
                {
                    string sourceFile = Path.Combine(request.SourceFolder, fileName);

                    if (!File.Exists(sourceFile))
                    {
                        result.Errors.Add($"No existe: {sourceFile}");
                        continue;
                    }

                    foreach (var dest in request.DestinationFolders)
                    {
                        try
                        {
                            string destFile = Path.Combine(dest, fileName);
                            File.Copy(sourceFile, destFile, request.Overwrite);
                            result.CopiedFiles.Add(destFile);
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"Error copiando {fileName} a {dest}: {ex.Message}");
                        }
                    }
                }

                result.Success = result.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error general: {ex.Message}");
                result.Success = false;
            }

            return result;
        }
    }
}
