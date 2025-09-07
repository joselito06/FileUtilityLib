using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileUtilityLib.Models
{
    public enum DuplicateComparison
    {
        SizeAndDate,    // Comparar tamaño Y fecha
        SizeOnly,       // Solo tamaño
        DateOnly,       // Solo fecha de modificación
        HashContent     // Hash del contenido (más lento pero preciso)
    }
}
