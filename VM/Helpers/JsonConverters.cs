using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryManager.VM.Helpers
{
    //Taken from: https://stackoverflow.com/a/48794588/11689514

    public class IgnoreUnexpectedArraysConverter<T> : IgnoreUnexpectedArraysConverterBase
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(T).IsAssignableFrom(objectType);
        }
    }

    public class IgnoreUnexpectedArraysConverter : IgnoreUnexpectedArraysConverterBase
    {
        readonly IContractResolver resolver;

        public IgnoreUnexpectedArraysConverter(IContractResolver resolver)
        {
            this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public override bool CanConvert(Type objectType)
        {
            if (objectType.IsPrimitive || objectType == typeof(string))
                return false;
            return resolver.ResolveContract(objectType) is JsonObjectContract;
        }
    }

    public abstract class IgnoreUnexpectedArraysConverterBase : JsonConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var contract = serializer.ContractResolver.ResolveContract(objectType);
            if (!(contract is JsonObjectContract))
            {
                throw new JsonSerializationException(string.Format("{0} is not a JSON object", objectType));
            }

            do
            {
                if (reader.TokenType == JsonToken.Null)
                    return null;
                else if (reader.TokenType == JsonToken.Comment)
                    continue;
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    var array = JArray.Load(reader);
                    if (array.Count > 0)
                        throw new JsonSerializationException(string.Format("Array was not empty."));
                    return null;
                }
                else if (reader.TokenType == JsonToken.StartObject)
                {
                    // Prevent infinite recursion by using Populate()
                    existingValue = existingValue ?? contract.DefaultCreator();
                    serializer.Populate(reader, existingValue);
                    return existingValue;
                }
                else
                {
                    throw new JsonSerializationException(string.Format("Unexpected token {0}", reader.TokenType));
                }
            }
            while (reader.Read());
            throw new JsonSerializationException("Unexpected end of JSON.");
        }

        public override bool CanWrite { get { return false; } }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
