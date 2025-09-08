using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileUtilityLib.Core.Compatibility
{
    /// <summary>
    /// Métodos de compatibilidad para diferentes frameworks
    /// </summary>
    public static class FrameworkCompatibility
    {
        /// <summary>
        /// Copia un stream de forma asíncrona compatible con todos los frameworks
        /// </summary>
        public static async Task CopyToAsync(Stream source, Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
#if NET6_0_OR_GREATER || NET7_0_OR_GREATER || NET8_0_OR_GREATER
            // .NET 6+ tiene mejor soporte nativo
            await source.CopyToAsync(destination, bufferSize, cancellationToken);
#elif NETSTANDARD2_1
            // .NET Standard 2.1 soporta CancellationToken
            await source.CopyToAsync(destination, bufferSize, cancellationToken);
#else
            // .NET Framework 4.7.2 y .NET Standard 2.0 - implementación manual
            await CopyToAsyncCompat(source, destination, bufferSize, cancellationToken);
#endif
        }

#if NETFRAMEWORK || NETSTANDARD2_0
        /// <summary>
        /// Implementación compatible para frameworks antiguos
        /// </summary>
        private static async Task CopyToAsyncCompat(Stream source, Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            var buffer = new byte[bufferSize];
            int bytesRead;

            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            }
        }
#endif

        /// <summary>
        /// Verifica si el framework soporta características modernas
        /// </summary>
        public static bool SupportsModernFeatures()
        {
#if NET6_0_OR_GREATER || NET7_0_OR_GREATER || NET8_0_OR_GREATER
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// Obtiene información del framework actual
        /// </summary>
        public static string GetFrameworkInfo()
        {
#if NET8_0_OR_GREATER
            return ".NET 8.0+";
#elif NET7_0_OR_GREATER
            return ".NET 7.0";
#elif NET6_0_OR_GREATER
            return ".NET 6.0";
#elif NET48
            return ".NET Framework 4.8";
#elif NETFRAMEWORK
            return ".NET Framework 4.7.2+";
#else
            return "Unknown Framework";
#endif
        }

#if NETFRAMEWORK
        /// <summary>
        /// Escribe texto de forma asíncrona en un archivo para .NET Framework
        /// </summary>
        /// <param name="path">Ruta del archivo</param>
        /// <param name="contents">Contenido a escribir</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Task que representa la operación asíncrona</returns>
        public static async Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (contents == null)
                throw new ArgumentNullException(nameof(contents));

            var bytes = Encoding.UTF8.GetBytes(contents);
            await WriteAllBytesAsync(path, bytes, cancellationToken).ConfigureAwait(false);
        }
#elif NET6_0_OR_GREATER
        /// <summary>
        /// Escribe texto de forma asíncrona en un archivo para .NET Framework
        /// </summary>
        /// <param name="path">Ruta del archivo</param>
        /// <param name="contents">Contenido a escribir</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Task que representa la operación asíncrona</returns>
        public static async Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (contents == null)
                throw new ArgumentNullException(nameof(contents));

            await File.WriteAllTextAsync(path, contents);
        }

        /// <summary>
        /// Escribe texto de forma asíncrona en un archivo con encoding específico para .NET Framework
        /// </summary>
        /// <param name="path">Ruta del archivo</param>
        /// <param name="contents">Contenido a escribir</param>
        /// <param name="encoding">Encoding a utilizar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Task que representa la operación asíncrona</returns>
        public static async Task WriteAllTextAsync(string path, string contents, Encoding encoding, CancellationToken cancellationToken = default)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (contents == null)
                throw new ArgumentNullException(nameof(contents));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            var bytes = encoding.GetBytes(contents);
            await WriteAllBytesAsync(path, bytes, cancellationToken).ConfigureAwait(false);
        }
#endif

#if NETFRAMEWORK
        /// <summary>
        /// Lee todo el texto de un archivo de forma asíncrona para .NET Framework
        /// </summary>
        /// <param name="path">Ruta del archivo</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Contenido del archivo</returns>
        public static async Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var bytes = await ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
            return Encoding.UTF8.GetString(bytes);
        }
#elif NET6_0_OR_GREATER

        /// <summary>
        /// Lee todo el texto de un archivo de forma asíncrona para .NET Framework
        /// </summary>
        /// <param name="path">Ruta del archivo</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Contenido del archivo</returns>
        public static async Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            
            return await File.ReadAllTextAsync(path);
        }

#endif
        /// <summary>
        /// Lee todo el texto de un archivo de forma asíncrona con encoding específico para .NET Framework
        /// </summary>
        /// <param name="path">Ruta del archivo</param>
        /// <param name="encoding">Encoding a utilizar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Contenido del archivo</returns>
        public static async Task<string> ReadAllTextAsync(string path, Encoding encoding, CancellationToken cancellationToken = default)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            var bytes = await ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
            return encoding.GetString(bytes);
        }

        /// <summary>
        /// Escribe bytes de forma asíncrona en un archivo para .NET Framework
        /// </summary>
        /// <param name="path">Ruta del archivo</param>
        /// <param name="bytes">Bytes a escribir</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Task que representa la operación asíncrona</returns>
        public static async Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await fileStream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
                await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Lee todos los bytes de un archivo de forma asíncrona para .NET Framework
        /// </summary>
        /// <param name="path">Ruta del archivo</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Bytes del archivo</returns>
        public static async Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
            {
                var buffer = new byte[fileStream.Length];
                int totalBytesRead = 0;
                int bytesRead;

                while (totalBytesRead < buffer.Length)
                {
                    bytesRead = await fileStream.ReadAsync(buffer, totalBytesRead, buffer.Length - totalBytesRead, cancellationToken).ConfigureAwait(false);
                    if (bytesRead == 0)
                        break;
                    totalBytesRead += bytesRead;
                }

                if (totalBytesRead != buffer.Length)
                {
                    // Redimensionar el array si el archivo cambió de tamaño durante la lectura
                    var actualBuffer = new byte[totalBytesRead];
                    Array.Copy(buffer, actualBuffer, totalBytesRead);
                    return actualBuffer;
                }

                return buffer;
            }
        }

        /// <summary>
        /// Verifica si un directorio existe de forma asíncrona para .NET Framework
        /// </summary>
        /// <param name="path">Ruta del directorio</param>
        /// <returns>True si existe, false en caso contrario</returns>
        public static Task<bool> DirectoryExistsAsync(string path)
        {
            return Task.FromResult(Directory.Exists(path));
        }

        /// <summary>
        /// Verifica si un archivo existe de forma asíncrona para .NET Framework
        /// </summary>
        /// <param name="path">Ruta del archivo</param>
        /// <returns>True si existe, false en caso contrario</returns>
        public static Task<bool> FileExistsAsync(string path)
        {
            return Task.FromResult(File.Exists(path));
        }

        /// <summary>
        /// Crea un directorio de forma asíncrona para .NET Framework
        /// </summary>
        /// <param name="path">Ruta del directorio</param>
        /// <returns>Task que representa la operación asíncrona</returns>
        public static Task CreateDirectoryAsync(string path)
        {
            return Task.Run(() => Directory.CreateDirectory(path));
        }

    }

}
