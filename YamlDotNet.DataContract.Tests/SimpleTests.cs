using System.Runtime.Serialization;
using NUnit.Framework;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.TypeInspectors;

namespace YamlDotNet.DataContract.Tests {
    [TestFixture]
    public sealed class SimpleTests {

        [SetUp]
        public void Enter() {
            var builder = new DeserializerBuilder();

            _optInDeserializer = builder
                .WithTypeInspector(inspector => new DataContractTypeInspector(inspector) {
                    DataMemberSerialization = DataMemberSerialization.OptIn,
                    NamingConvention = new UnderscoredNamingConvention()
                })
                .IgnoreUnmatchedProperties()
                .Build();
        }

        [TearDown]
        public void Exit() {
        }

        [Test]
        public void MixedInput() {
            const string yaml = @"
data:
    url: https://www.xxx.yyy.zzz/a.bcd
    bgm: nothing is here
    score: 123654
    beats_per_minute: 123.45
    this_should_be_ignored: if you see this in the object instance, there is an error
string_list:
    - item1
    - item2
    - item3
new_field: 99999
";

            Assert.NotNull(_optInDeserializer);

            var obj = _optInDeserializer.Deserialize<OuterData>(yaml);

            Assert.IsNotNull(obj);

            Assert.IsNotNull(obj.Data);
            Assert.AreEqual("https://www.xxx.yyy.zzz/a.bcd", obj.Data.Url);
            Assert.AreEqual("nothing is here", obj.Data.Music);
            Assert.AreEqual(123654, obj.Data.Score);
            Assert.AreEqual(123.45, obj.Data.BeatsPerMinute, 0.01);
            Assert.IsNull(obj.Data.ThisShouldBeIgnored);

            Assert.IsNotNull(obj.StringList);
            Assert.AreEqual(3, obj.StringList.Length);
            Assert.AreEqual("item1", obj.StringList[0]);
            Assert.AreEqual("item2", obj.StringList[1]);
            Assert.AreEqual("item3", obj.StringList[2]);

            Assert.AreEqual(99999, obj.NewField);
        }

        [DataContract]
        private sealed class OuterData {

            // Nested types
            [DataMember]
            public InnerData Data { get; set; }

            [DataContract]
            public sealed class InnerData {

                // Reference type, original name
                [DataMember]
                public string Url { get; set; }

                // Name override
                [DataMember(Name = "bgm")]
                public string Music { get; set; }

                // Value type, original name
                [DataMember]
                public int Score { get; set; }

                // Floating point numbers, converted name
                [DataMember]
                public float BeatsPerMinute { get; set; }

                // Ignored member
                [IgnoreDataMember]
                public string ThisShouldBeIgnored { get; set; }

            }

            // Collection types
            [DataMember]
            public string[] StringList { get; set; }

            // Fields
            [DataMember]
            public int NewField;

        }

        private Deserializer _optInDeserializer;

    }
}
