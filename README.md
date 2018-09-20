# YamlDotNet.DataContract

An inspector for [YamlDotNet](https://github.com/aaubry/YamlDotNet) that processes `DataMemberAttribute`/`IgnoreDataMemberAttribute` instead of `YamlMemberAttribute`/`YamlIgnoreAttribute`.

## Usage

```csharp
var builder = new DeserializerBuilder();
var deserializer = builder
    .WithTypeInspector(inspector => new DataContractTypeInspector(inspector) {
        DataMemberSerialization = DataMemberSerialization.OptIn,
        // Since custom type inspectors are added after the built-in ones, they cannot pass their results
        // to the built-in ones, e.g. NamingConventionTypeInspector (responsible for the WithNamingConveition method).
        // The naming convention must be assigned here, if there is one.
        NamingConvention = new UnderscoredNamingConvention(),
        CacheResults = true, // default
        IncludeNonPublicMembers = false // default
    })
    .IgnoreUnmatchedProperties()
    .Build();
```

## Limitations

- `IsRequired`, `EmitDefaultValue` and `Order` of `DataMemberAttribute` are ignored.

## License

MIT
