using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using Newtonsoft.Json;
using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;

namespace Proyecto1_Optimizacion
{
    class Program
    {
        static void Main(string[] args)
        {
            string csvFilePath = @"D:/Universidad/Cuarto ciclo/Estructura de datos II/Operations.csv";
            List<OperationRecord> operations = ReadCsvFile(csvFilePath);

            BTree bTree = new BTree(3); // Cambia el grado mínimo según tu necesidad

            foreach (var record in operations)
            {

                try
                {
                    Articulo articulo = JsonConvert.DeserializeObject<Articulo>(record.Details);

                    switch (record.Operation.ToUpper())
                    {
                        case "INSERT":
                            bTree.Insert(articulo);
                            break;
                        case "PATCH":
                            bTree.Update(articulo.ISBN, articulo);
                            break;
                        case "DELETE":
                            bTree.Delete(articulo.ISBN);
                            break;
                        default:
                            Console.WriteLine($"Operación desconocida: {record.Operation}");
                            break;
                    }
                }
                catch (JsonReaderException ex)
                {
                    Console.WriteLine($"Error de deserialización para los detalles: {record.Details}. Mensaje: {ex.Message}");
                }
            }

            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("Seleccione una opción:");
                Console.WriteLine("1. Buscar artículo por ISBN");
                Console.WriteLine("2. Buscar artículo por nombre");
                Console.WriteLine("3. Salir");
                Console.Write("Opción: ");
                string option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        Console.Write("Ingrese el ISBN del artículo: ");
                        string isbn = Console.ReadLine();
                        Articulo foundArticulo = bTree.Search(isbn);
                        if (foundArticulo != null)
                        {
                            string jsonOutput = JsonConvert.SerializeObject(foundArticulo, Formatting.Indented);
                            Console.WriteLine($"Artículo encontrado: {jsonOutput}");
                        }
                        else
                        {
                            Console.WriteLine("Artículo no encontrado.");
                        }
                        break;
                    case "2":
                        Console.Write("Ingrese el nombre del artículo: ");
                        string name = Console.ReadLine();
                        List<Articulo> foundArticulos = bTree.SearchByName(name);
                        if (foundArticulos.Count > 0)
                        {
                            foreach (var articulo in foundArticulos)
                            {
                                string jsonOutput = JsonConvert.SerializeObject(articulo, Formatting.Indented);
                                Console.WriteLine($"Artículo encontrado: {jsonOutput}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Artículo no encontrado.");
                        }
                        break;
                    case "3":
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("Opción no válida. Intente de nuevo.");
                        break;
                }
            }
            ProcessSearchFile(bTree);
        }

        static void ProcessSearchFile(BTree bTree)
        {
            string searchFilePath = @"D:/Universidad/Cuarto ciclo/Estructura de datos II/pruebas.csv";
            string outputFilePath = @"D:/Universidad/Cuarto ciclo/Estructura de datos II/OutputLog.csv";

            List<OperationRecord> searchOperations = ReadSearchCsvFile(searchFilePath);

            if (searchOperations.Count == 0)
            {
                Console.WriteLine("No se encontraron operaciones de búsqueda en el archivo.");
                return;
            }

            try
            {
                using (var writer = new StreamWriter(outputFilePath))
                using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {

                    foreach (var record in searchOperations)
                    {
                        Console.WriteLine($"Operación: {record.Operation}, Detalles: {record.Details}");

                        if (record.Operation.Trim().ToUpper() != "SEARCH")
                        {
                            writer.WriteLine($"Operación desconocida en el registro: {record.Operation}");
                            Console.WriteLine($"Operación desconocida en el registro: {record.Operation}");
                            continue;
                        }

                        try
                        {
                            // Limpiar y ajustar los detalles del JSON
                            var cleanedDetails = record.Details
                                .Replace("\"\"", "\"") // Eliminar comillas dobles adicionales
                                .Trim(); // Eliminar espacios adicionales

                            // Deserializar a la clase Articulo
                            var searchDetails = JsonConvert.DeserializeObject<Articulo>(cleanedDetails);

                            if (searchDetails != null && (!string.IsNullOrEmpty(searchDetails.name) || !string.IsNullOrEmpty(searchDetails.ISBN)))
                            {
                                List<Articulo> foundArticulos = new List<Articulo>();

                                if (!string.IsNullOrEmpty(searchDetails.name))
                                {
                                    Console.WriteLine($"Buscando artículos con nombre: {searchDetails.name}");
                                    foundArticulos = bTree.SearchByName(searchDetails.name);
                                }
                                else if (!string.IsNullOrEmpty(searchDetails.ISBN))
                                {
                                    Console.WriteLine($"Buscando artículo con ISBN: {searchDetails.ISBN}");
                                    var articulo = bTree.Search(searchDetails.ISBN);
                                    if (articulo != null)
                                    {
                                        foundArticulos.Add(articulo);
                                    }
                                }
                                else
                                {
                                    writer.WriteLine($"Tipo de búsqueda desconocido en el registro: {record.Details}");
                                    Console.WriteLine($"Tipo de búsqueda desconocido en el registro: {record.Details}");
                                    continue;
                                }

                                if (foundArticulos.Count > 0)
                                {
                                    foreach (var articulo in foundArticulos)
                                    {
                                        string jsonOutput = JsonConvert.SerializeObject(articulo, Formatting.Indented);
                                        writer.WriteLine(jsonOutput);
                                        Console.WriteLine($"Artículo encontrado y escrito en el archivo: {jsonOutput}");
                                    }
                                }
                                else
                                {
                                    writer.WriteLine($"No se encontraron artículos con {searchDetails.name ?? searchDetails.ISBN}");
                                    Console.WriteLine($"No se encontraron artículos con {searchDetails.name ?? searchDetails.ISBN}");
                                }
                            }
                            else
                            {
                                writer.WriteLine($"Detalles de búsqueda vacíos o inválidos en el registro: {record.Details}");
                            }
                        }
                        catch (JsonException ex)
                        {
                            writer.WriteLine($"Error al deserializar detalles: {record.Details}. Mensaje: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al escribir en el archivo: {ex.Message}");
            }
        }


        static List<OperationRecord> ReadSearchCsvFile(string filePath)
        {
            List<OperationRecord> records = new List<OperationRecord>();
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                Delimiter = ";",
                MissingFieldFound = null,
                BadDataFound = null,
                Quote = '"',
                Escape = '\\',
            };

            try
            {
                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, config))
                {
                    Console.WriteLine("Iniciando lectura del archivo CSV.");
                    while (csv.Read())
                    {
                        try
                        {
                            var operation = csv.GetField(0)?.Trim();
                            var details = csv.GetField(1)?.Trim();

                            if (string.IsNullOrEmpty(operation) || string.IsNullOrEmpty(details))
                            {
                                Console.WriteLine("Error: Se encontraron campos nulos en el archivo CSV.");
                                continue;
                            }

                            Console.WriteLine($"Leído - Operación: '{operation}', Detalles: '{details}'");

                            var record = new OperationRecord
                            {
                                Operation = operation,
                                Details = details
                            };

                            records.Add(record);
                        }
                        catch (IndexOutOfRangeException ex)
                        {
                            Console.WriteLine($"Error al leer el archivo CSV: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al abrir o leer el archivo CSV: {ex.Message}");
            }

            Console.WriteLine($"Número total de registros leídos: {records.Count}");

            return records;
        }


        static List<OperationRecord> ReadCsvFile(string filePath)
        {
            List<OperationRecord> records = new List<OperationRecord>();
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                Delimiter = ";",
                MissingFieldFound = null,
                BadDataFound = null,
                Quote = '"',
                Escape = '\\'
            };

            Console.WriteLine($"Leyendo el archivo CSV: {filePath}");

            try
            {
                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, config))
                {
                    Console.WriteLine("Iniciando lectura del archivo CSV.");
                    while (csv.Read())
                    {
                        try
                        {
                            var operation = csv.GetField(0)?.Trim();
                            var details = csv.GetField(1)?.Trim();

                            Console.WriteLine($"Leído - Operación: '{operation}', Detalles: '{details}'");

                            if (operation == null || details == null)
                            {
                                Console.WriteLine("Error: Se encontraron campos nulos en el archivo CSV.");
                                continue;
                            }

                            var record = new OperationRecord
                            {
                                Operation = operation,
                                Details = details
                            };

                            records.Add(record);
                        }
                        catch (IndexOutOfRangeException ex)
                        {
                            Console.WriteLine($"Error al leer el archivo CSV: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al abrir o leer el archivo CSV: {ex.Message}");
            }

            Console.WriteLine($"Número total de registros leídos: {records.Count}");

            return records;
        }
    }
}
