using System.Runtime.Serialization;

namespace YamlDotNet.Serialization {
    /// <summary>
    /// The (de-)serialization method.
    /// </summary>
    public enum DataMemberSerialization {

        /// <summary>
        /// Members are (de-)serialized only when they are annotated with <see cref="DataMemberAttribute"/>.
        /// </summary>
        OptIn = 0,
        /// <summary>
        /// Members are (de-)serialized unless they are annotated with <see cref="IgnoreDataMemberAttribute"/>.
        /// </summary>
        OptOut = 1

    }
}
