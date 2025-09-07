using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileUtilityLib.Models
{
    public enum DuplicateHandling
    {
        Skip,           // Saltar si existe igual
        Overwrite,      // Sobrescribir siempre
        OverwriteIfNewer, // Solo si el origen es más nuevo
        RenameNew       // Renombrar el nuevo archivo
    }
}
