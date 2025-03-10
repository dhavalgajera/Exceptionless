﻿using Exceptionless.Core.Extensions;

namespace Exceptionless.Core.Models.Data;

public class SimpleError : IData {
    public SimpleError() {
        Data = new DataDictionary();
    }

    /// <summary>
    /// The error message.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// The error type.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// The stack trace for the error.
    /// </summary>
    public string StackTrace { get; set; }

    /// <summary>
    /// Extended data entries for this error.
    /// </summary>
    public DataDictionary Data { get; set; }

    /// <summary>
    /// An inner (nested) error.
    /// </summary>
    public SimpleError Inner { get; set; }

    protected bool Equals(SimpleError other) {
        return String.Equals(Message, other.Message) && String.Equals(Type, other.Type) && String.Equals(StackTrace, other.StackTrace) && Equals(Data, other.Data) && Equals(Inner, other.Inner);
    }

    public override bool Equals(object obj) {
        if (obj is null)
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;
        return Equals((SimpleError)obj);
    }

    public override int GetHashCode() {
        unchecked {
            int hashCode = Message?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ (Type?.GetHashCode() ?? 0);
            hashCode = (hashCode * 397) ^ (StackTrace?.GetHashCode() ?? 0);
            hashCode = (hashCode * 397) ^ (Data?.GetCollectionHashCode() ?? 0);
            hashCode = (hashCode * 397) ^ (Inner?.GetHashCode() ?? 0);
            return hashCode;
        }
    }

    public static class KnownDataKeys {
        public const string ExtraProperties = "@ext";
    }
}
