using System;
using System.Collections.Generic;
using System.Text;

namespace Ionic.Zip
{
    ///// <summary>
    ///// Base exception type for all custom exceptions in the Zip library. It acts as a marker class.
    ///// </summary>
    //[AttributeUsage(AttributeTargets.Class)]
    //public class ZipExceptionAttribute : Attribute { }


    /// <summary>
    /// Issued when an <c>ZipEntry.ExtractWithPassword()</c> method is invoked
    /// with an incorrect password.
    /// </summary>
    [Serializable]
    public class BadPasswordException : ZipException
    {
        /// <summary>
        /// Default ctor.
        /// </summary>
        public BadPasswordException() { }

        /// <summary>
        /// Come on, you know how exceptions work. Why are you looking at this documentation?
        /// </summary>
        /// <param name="message">The message in the exception.</param>
        public BadPasswordException(String message)
            : base(message)
        { }

        /// <summary>
        /// Come on, you know how exceptions work. Why are you looking at this documentation?
        /// </summary>
        /// <param name="message">The message in the exception.</param>
        /// <param name="innerException">The innerException for this exception.</param>
        public BadPasswordException(String message, Exception innerException)
            : base(message, innerException)
        {
        } 
        
#if !NETCF
        /// <summary>
        /// Come on, you know how exceptions work. Why are you looking at this documentation?
        /// </summary>
        /// <param name="serializationInfo">The serialization info for the exception.</param>
        /// <param name="streamingContext">The streaming context from which to deserialize.</param>
        protected BadPasswordException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
          {  }
#endif

    }

    /// <summary>
    /// Indicates that a read was attempted on a stream, and bad or incomplete data was
    /// received.  
    /// </summary>
    [Serializable]
    public class BadReadException : ZipException
    {
        /// <summary>
        /// Default ctor.
        /// </summary>
        public BadReadException() { }

        /// <summary>
        /// Come on, you know how exceptions work. Why are you looking at this documentation?
        /// </summary>
        /// <param name="message">The message in the exception.</param>
        public BadReadException(String message)
            : base(message)
        { }

        /// <summary>
        /// Come on, you know how exceptions work. Why are you looking at this documentation?
        /// </summary>
        /// <param name="message">The message in the exception.</param>
        /// <param name="innerException">The innerException for this exception.</param>
        public BadReadException(String message,
            Exception innerException)
            : base(message, innerException)
        {
        }

#if !NETCF
        /// <summary>
        /// Come on, you know how exceptions work. Why are you looking at this documentation?
        /// </summary>
        /// <param name="serializationInfo">The serialization info for the exception.</param>
        /// <param name="streamingContext">The streaming context from which to deserialize.</param>
        protected BadReadException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
          {  }        
#endif

    }

    /// <summary>
    /// Issued when an CRC check fails upon extracting an entry from a zip archive.
    /// </summary>
    [Serializable]
    public class BadCrcException : ZipException
    {
        /// <summary>
        /// Default ctor.
        /// </summary>
        public BadCrcException() { }

        /// <summary>
        /// Come on, you know how exceptions work. Why are you looking at this documentation?
        /// </summary>
        /// <param name="message">The message in the exception.</param>
        public BadCrcException(String message)
            : base(message)
        { }

        /// <summary>
        /// Come on, you know how exceptions work. Why are you looking at this documentation?
        /// </summary>
        /// <param name="message">The message in the exception.</param>
        /// <param name="innerException">The innerException for this exception.</param>
        public BadCrcException(String message,
            Exception innerException)
            : base(message, innerException)
        {
        }

#if !NETCF
        /// <summary>
        /// Come on, you know how exceptions work. Why are you looking at this documentation?
        /// </summary>
        /// <param name="serializationInfo">The serialization info for the exception.</param>
        /// <param name="streamingContext">The streaming context from which to deserialize.</param>
        protected BadCrcException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
          {  }
#endif

    }


    /// <summary>
    /// Issued when errors occur saving a self-extracting archive.
    /// </summary>
    [Serializable]
    public class SfxGenerationException : ZipException
    {
        /// <summary>
        /// Default ctor.
        /// </summary>
        public SfxGenerationException() { }

        /// <summary>
        /// Come on, you know how exceptions work. Why are you looking at this documentation?
        /// </summary>
        /// <param name="message">The message in the exception.</param>
        public SfxGenerationException(String message)
            : base(message)
        { }

        /// <summary>
        /// Come on, you know how exceptions work. Why are you looking at this documentation?
        /// </summary>
        /// <param name="message">The message in the exception.</param>
        /// <param name="innerException">The innerException for this exception.</param>
        public SfxGenerationException(String message, Exception innerException)
            : base(message, innerException)
        { }

        
#if !NETCF
        /// <summary>
        /// Come on, you know how exceptions work. Why are you looking at this documentation?
        /// </summary>
        /// <param name="serializationInfo">The serialization info for the exception.</param>
        /// <param name="streamingContext">The streaming context from which to deserialize.</param>
        protected SfxGenerationException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
          {  }
#endif

    }


    /// <summary>
    /// Indicates that an operation was attempted on a ZipFile which was not possible
    /// given the state of the instance. For example, if you call <c>Save()</c> on a ZipFile 
    /// which has no filename set, you can get this exception. 
    /// </summary>
    [Serializable]
    public class BadStateException : ZipException
    {
        /// <summary>
        /// Default ctor.
        /// </summary>
        public BadStateException() { }

        /// <summary>
        /// Come on, you know how exceptions work. Why are you looking at this documentation?
        /// </summary>
        /// <param name="message">The message in the exception.</param>
        public BadStateException(String message)
            : base(message)
        { }

        /// <summary>
        /// Come on, you know how exceptions work. Why are you looking at this documentation?
        /// </summary>
        /// <param name="message">The message in the exception.</param>
        /// <param name="innerException">The innerException for this exception.</param>
        public BadStateException(String message,
            Exception innerException)
            : base(message, innerException)
        {}

#if !NETCF
        /// <summary>
        /// Come on, you know how exceptions work. Why are you looking at this documentation?
        /// </summary>
        /// <param name="serializationInfo">The serialization info for the exception.</param>
        /// <param name="streamingContext">The streaming context from which to deserialize.</param>
        protected BadStateException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
          {  }
#endif

    }

    /// <summary>
    /// Base class for all exceptions defined by and throw by the Zip library.
    /// </summary>
    [Serializable]
    public class ZipException : Exception
    {
        /// <summary>
        /// Default ctor.
        /// </summary>
        public ZipException() { }

        /// <summary>
        /// Come on, you know how exceptions work. Why are you looking at this documentation?
        /// </summary>
        /// <param name="message">The message in the exception.</param>
        public ZipException(String message) : base(message) { }

        /// <summary>
        /// Come on, you know how exceptions work. Why are you looking at this documentation?
        /// </summary>
        /// <param name="message">The message in the exception.</param>
        /// <param name="innerException">The innerException for this exception.</param>
        public ZipException(String message,
            Exception innerException)
            : base(message, innerException)
        { }

#if !NETCF
        /// <summary>
        /// Come on, you know how exceptions work. Why are you looking at this documentation?
        /// </summary>
        /// <param name="serializationInfo">The serialization info for the exception.</param>
        /// <param name="streamingContext">The streaming context from which to deserialize.</param>
        protected ZipException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        { }
#endif

    }

}
