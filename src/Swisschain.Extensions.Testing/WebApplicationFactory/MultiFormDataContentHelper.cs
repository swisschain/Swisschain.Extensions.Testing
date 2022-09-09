using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace Swisschain.Extensions.Testing.WebApplicationFactory
{
    public static class MultipartFormDataContentExtensions
    {
        /// <summary>
        /// Creates MultipartFormDataContent for calling REST endpoint with [FromForm] attribute.
        /// Particularly useful whenever complex DTOs with IFormFile within need to be sent.
        /// </summary>
        /// <remarks>
        /// Not all scalar types are implemented property, also collections are not implemented at all.
        /// For more info what can also be supported, see
        /// https://brokul.dev/sending-files-and-additional-data-using-httpclient-in-net-core
        /// </remarks>
        public static MultipartFormDataContent ToMultipartFormDataContent(this object dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var content = new MultipartFormDataContent();
            
            Traverse(dto, content, new Stack<string>());

            return content;
        }

        public static void Traverse(object o, MultipartFormDataContent content, Stack<string> prefixes)
        {
            if (prefixes.Count > 10)
            {
                throw new InvalidOperationException(
                    "The object being converted to MultipartFormDataContent either has too deep nested structure or there are circular links.");
            }
            
            var parentObjectType = o.GetType();
            var parentObjectAssemblyName = parentObjectType.Assembly.GetName().Name ?? string.Empty;
            
            var properties = parentObjectType.GetRuntimeProperties()
                .Where(p => p.GetMethod != null && p.GetMethod.IsPublic && p.GetMethod.IsStatic == false)
                .ToList();

            foreach (var property in properties)
            {
                if (!property.TryGetValue(o, out var value))
                {
                    throw new InvalidOperationException(
                        $"Cannot obtain value of {property.Name}: {value}");
                }

                if (value != null)
                {
                    var propertyTypeAssemblyName = property.PropertyType.Assembly.GetName().Name;
                    
                    // nested object
                    if (parentObjectAssemblyName.Equals(propertyTypeAssemblyName))
                    {
                        prefixes.Push(property.Name);
                        Traverse(value, content, prefixes);
                        prefixes.Pop();
                    }
                    else // scalar object or IFormFile or collection
                    {
                        if (prefixes.Any())
                        {
                            var name = $"{string.Join(".", prefixes)}.{property.Name}";
                            AppendNestedContent(value, name, content);
                        }
                        else
                        {
                            AppendNestedContent(value, property.Name, content);
                        }
                    }
                }
            }
        }
        
        private static void AppendNestedContent(object value, string path, MultipartFormDataContent content)
        {
            // TODO implement special logic for collections and dates
            if (value is FormFile formFile)
            {
                var formFileStream = new MemoryStream();
                formFile.CopyTo(formFileStream);
                formFileStream.Position = 0;
                
                content.Add(new StreamContent(formFileStream), path, formFile.FileName);
            }
            else
            {
                content.Add(new StringContent(value.ToString()), path);
            }
        }

        private static bool TryGetValue(this PropertyInfo property, object element, out object value)
        {
            try
            {
                value = property.GetValue(element);
                return true;
            }
            catch (Exception ex)
            {
                value = $"{{{ex.GetType().Name}: {ex.Message}}}";
                return false;
            }
        }
    }
}
