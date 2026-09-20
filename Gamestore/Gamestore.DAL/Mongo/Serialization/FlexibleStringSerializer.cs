using System.Globalization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Gamestore.DAL.Mongo.Serialization;

public sealed class FlexibleStringSerializer : SerializerBase<string>
{
    public override string Deserialize(
        BsonDeserializationContext context,
        BsonDeserializationArgs args)
    {
        var bsonType = context.Reader.GetCurrentBsonType();

        if (bsonType == BsonType.Null)
        {
            context.Reader.ReadNull();
            return string.Empty;
        }

        if (bsonType == BsonType.String)
        {
            return context.Reader.ReadString();
        }

        if (bsonType == BsonType.Int32)
        {
            return context.Reader.ReadInt32().ToString(CultureInfo.InvariantCulture);
        }

        if (bsonType == BsonType.Int64)
        {
            return context.Reader.ReadInt64().ToString(CultureInfo.InvariantCulture);
        }

        if (bsonType == BsonType.Double)
        {
            return context.Reader.ReadDouble().ToString(CultureInfo.InvariantCulture);
        }

        if (bsonType == BsonType.Decimal128)
        {
            return context.Reader.ReadDecimal128().ToString();
        }

        if (bsonType == BsonType.Boolean)
        {
            return context.Reader.ReadBoolean().ToString();
        }

        if (bsonType == BsonType.ObjectId)
        {
            return context.Reader.ReadObjectId().ToString();
        }

        context.Reader.SkipValue();
        return string.Empty;
    }

    public override void Serialize(
        BsonSerializationContext context,
        BsonSerializationArgs args,
        string? value)
    {
        if (value is null)
        {
            context.Writer.WriteNull();
            return;
        }

        context.Writer.WriteString(value);
    }
}
